using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PrivoxyManager.Services;
using PrivoxyManager.Services.Interfaces;
using PrivoxyManager.ViewModels;

namespace PrivoxyManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IHost? _host;
        private IServiceProvider? _serviceProvider;

        /// <summary>
        /// Gets the current service provider.
        /// </summary>
        public static IServiceProvider ServiceProvider => ((App)Current).ServiceProvider;

        /// <summary>
        /// Gets the current service provider.
        /// </summary>
        public IServiceProvider ServiceProvider => _serviceProvider ?? throw new InvalidOperationException("Service provider not initialized");

        /// <summary>
        /// Overrides the application startup to configure dependency injection.
        /// </summary>
        /// <param name="e">The startup event arguments.</param>
        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Configure hosting
                _host = CreateHostBuilder().Build();
                _serviceProvider = _host.Services;

                // Configure logging
                var logger = _serviceProvider.GetRequiredService<ILogger<App>>();
                logger.LogInformation("Application starting up");

                // Initialize services
                await InitializeServicesAsync();

                // Create and show main window
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();

                logger.LogInformation("Application started successfully");
            }
            catch (Exception ex)
            {
                var logger = _serviceProvider?.GetRequiredService<ILogger<App>>();
                logger?.LogError(ex, "Error during application startup");
                
                MessageBox.Show(
                    $"Failed to start application: {ex.Message}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                
                Shutdown();
            }
        }

        /// <summary>
        /// Overrides the application exit to perform cleanup.
        /// </summary>
        /// <param name="e">The exit event arguments.</param>
        protected override async void OnExit(ExitEventArgs e)
        {
            try
            {
                var logger = _serviceProvider?.GetRequiredService<ILogger<App>>();
                logger?.LogInformation("Application shutting down");

                // Save application state
                if (_serviceProvider != null)
                {
                    var mainWindowViewModel = _serviceProvider.GetService<MainWindowViewModel>();
                    if (mainWindowViewModel != null)
                    {
                        await mainWindowViewModel.SaveStateAsync();
                    }
                }

                // Dispose services
                await _host?.StopAsync();
            }
            catch (Exception ex)
            {
                var logger = _serviceProvider?.GetRequiredService<ILogger<App>>();
                logger?.LogError(ex, "Error during application shutdown");
            }
        }

        /// <summary>
        /// Creates the host builder with configured services.
        /// </summary>
        /// <returns>The configured host builder.</returns>
        private static IHostBuilder CreateHostBuilder()
        {
            var builder = Host.CreateDefaultBuilder();

            // Configure configuration
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables();
            });

            // Configure logging
            builder.ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
                logging.AddEventLog();
            });

            // Configure services
            builder.ConfigureServices((context, services) =>
            {
                // Add logging
                services.AddLogging();

                // Add configuration
                services.AddSingleton<IConfiguration>(context.Configuration);

                // Add services
                services.AddSingleton<IConfigService, ConfigService>();
                services.AddSingleton<IServiceController, ServiceController>();
                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<ILogService, LogService>();
                services.AddSingleton<IDialogService, DialogService>();

                // Add ViewModels
                services.AddTransient<MainWindowViewModel>();
                services.AddTransient<ConfigViewModel>();
                services.AddTransient<LogViewModel>();

                // Add UI services
                services.AddSingleton<MainWindow>();
            });

            return builder;
        }

        /// <summary>
        /// Initializes services asynchronously.
        /// </summary>
        private static async Task InitializeServicesAsync()
        {
            try
            {
                // Initialize all ViewModels
                var serviceProvider = ServiceProvider;
                
                var mainWindowViewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();
                await mainWindowViewModel.InitializeAsync();

                var configViewModel = serviceProvider.GetRequiredService<ConfigViewModel>();
                await configViewModel.InitializeAsync();

                var logViewModel = serviceProvider.GetRequiredService<LogViewModel>();
                await logViewModel.InitializeAsync();
            }
            catch (Exception ex)
            {
                var logger = ServiceProvider.GetRequiredService<ILogger<App>>();
                logger.LogError(ex, "Error initializing services");
                throw;
            }
        }

        /// <summary>
        /// Gets a service of the specified type.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <returns>The service instance.</returns>
        public static T GetService<T>() where T : class
        {
            return ServiceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// Gets a service of the specified type.
        /// </summary>
        /// <param name="serviceType">The service type.</param>
        /// <returns>The service instance.</returns>
        public static object GetService(Type serviceType)
        {
            return ServiceProvider.GetRequiredService(serviceType);
        }
    }
}