using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using PrivoxyManager.Commands;
using PrivoxyManager.Services.Interfaces;
using System.ServiceProcess;

namespace PrivoxyManager.ViewModels
{
    /// <summary>
    /// ViewModel for the configuration tab.
    /// </summary>
    public class ConfigViewModel : BaseViewModel
    {
        private readonly ILogger<ConfigViewModel> _logger;
        private readonly IConfigService _configService;
        private readonly IServiceController _serviceController;
        private readonly IDialogService _dialogService;
        private readonly ISettingsService _settingsService;

        private string _configPath = string.Empty;
        private string _serviceName = string.Empty;
        private string _configContent = string.Empty;
        private string _serviceStatus = "Unknown";
        private bool _isServiceRunning;
        private string _statusMessage = string.Empty;
        private bool _isStatusError;
        private List<string> _recentConfigs = new();
        private bool _isConfigModified;

        /// <summary>
        /// Initializes a new instance of the ConfigViewModel class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="configService">The configuration service.</param>
        /// <param name="serviceController">The service controller.</param>
        /// <param name="dialogService">The dialog service.</param>
        /// <param name="settingsService">The settings service.</param>
        public ConfigViewModel(
            ILogger<ConfigViewModel> logger,
            IConfigService configService,
            IServiceController serviceController,
            IDialogService dialogService,
            ISettingsService settingsService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _serviceController = serviceController ?? throw new ArgumentNullException(nameof(serviceController));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            InitializeCommands();
        }

        #region Properties

        /// <summary>
        /// Gets or sets the path to the configuration file.
        /// </summary>
        public string ConfigPath
        {
            get => _configPath;
            set
            {
                if (SetProperty(ref _configPath, value))
                {
                    _ = Task.Run(async () => await AddToRecentConfigsAsync(value));
                }
            }
        }

        /// <summary>
        /// Gets or sets the name of the Privoxy service.
        /// </summary>
        public string ServiceName
        {
            get => _serviceName;
            set => SetProperty(ref _serviceName, value);
        }

        /// <summary>
        /// Gets or sets the configuration file content.
        /// </summary>
        public string ConfigContent
        {
            get => _configContent;
            set
            {
                if (SetProperty(ref _configContent, value))
                {
                    IsConfigModified = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the service status text.
        /// </summary>
        public string ServiceStatus
        {
            get => _serviceStatus;
            private set => SetProperty(ref _serviceStatus, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the service is running.
        /// </summary>
        public bool IsServiceRunning
        {
            get => _isServiceRunning;
            private set => SetProperty(ref _isServiceRunning, value);
        }

        /// <summary>
        /// Gets or sets the status message.
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the status message represents an error.
        /// </summary>
        public bool IsStatusError
        {
            get => _isStatusError;
            set => SetProperty(ref _isStatusError, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the configuration has been modified.
        /// </summary>
        public bool IsConfigModified
        {
            get => _isConfigModified;
            set => SetProperty(ref _isConfigModified, value);
        }

        /// <summary>
        /// Gets the list of recent configuration files.
        /// </summary>
        public List<string> RecentConfigs
        {
            get => _recentConfigs;
            private set => SetProperty(ref _recentConfigs, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Gets the command to browse for a configuration file.
        /// </summary>
        public ICommand BrowseConfigCommand { get; private set; }

        /// <summary>
        /// Gets the command to load the configuration file.
        /// </summary>
        public ICommand LoadConfigCommand { get; private set; }

        /// <summary>
        /// Gets the command to save the configuration file.
        /// </summary>
        public ICommand SaveConfigCommand { get; private set; }

        /// <summary>
        /// Gets the command to save the configuration file and restart the service.
        /// </summary>
        public ICommand SaveAndRestartCommand { get; private set; }

        /// <summary>
        /// Gets the command to start the service.
        /// </summary>
        public ICommand StartServiceCommand { get; private set; }

        /// <summary>
        /// Gets the command to stop the service.
        /// </summary>
        public ICommand StopServiceCommand { get; private set; }

        /// <summary>
        /// Gets the command to restart the service.
        /// </summary>
        public ICommand RestartServiceCommand { get; private set; }

        /// <summary>
        /// Gets the command to detect the Privoxy service.
        /// </summary>
        public ICommand DetectServiceCommand { get; private set; }

        /// <summary>
        /// Gets the command to validate the configuration.
        /// </summary>
        public ICommand ValidateConfigCommand { get; private set; }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            BrowseConfigCommand = new RelayCommand(async () => await BrowseConfigAsync());
            LoadConfigCommand = new RelayCommand(async () => await LoadConfigAsync(), () => !string.IsNullOrEmpty(ConfigPath));
            SaveConfigCommand = new RelayCommand(async () => await SaveConfigAsync(), () => !string.IsNullOrEmpty(ConfigPath));
            SaveAndRestartCommand = new RelayCommand(async () => await SaveAndRestartAsync(), () => !string.IsNullOrEmpty(ConfigPath));
            StartServiceCommand = new RelayCommand(async () => await StartServiceAsync(), () => !string.IsNullOrEmpty(ServiceName) && !IsServiceRunning);
            StopServiceCommand = new RelayCommand(async () => await StopServiceAsync(), () => !string.IsNullOrEmpty(ServiceName) && IsServiceRunning);
            RestartServiceCommand = new RelayCommand(async () => await RestartServiceAsync(), () => !string.IsNullOrEmpty(ServiceName));
            DetectServiceCommand = new RelayCommand(async () => await DetectServiceAsync());
            ValidateConfigCommand = new RelayCommand(async () => await ValidateConfigAsync(), () => !string.IsNullOrEmpty(ConfigContent));
        }

        #endregion

        #region Command Implementations

        private async Task BrowseConfigAsync()
        {
            try
            {
                var configPath = await _dialogService.ShowOpenFileDialogAsync(
                    "Select Privoxy Configuration File",
                    "Privoxy config (config.txt)|config.txt|All files (*.*)|*.*",
                    initialDirectory: System.IO.Path.GetDirectoryName(ConfigPath));

                if (!string.IsNullOrEmpty(configPath))
                {
                    ConfigPath = configPath;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error browsing for configuration file");
                await _dialogService.ShowErrorAsync(
                    $"Failed to browse for configuration file: {ex.Message}",
                    "Error");
            }
        }

        private async Task LoadConfigAsync()
        {
            await ExecuteBusyAsync(async () =>
            {
                try
                {
                    if (!_configService.ConfigExists(ConfigPath))
                    {
                        await _dialogService.ShowErrorAsync(
                            $"Configuration file not found:\n{ConfigPath}",
                            "Configuration Not Found");
                        return;
                    }

                    var content = await _configService.LoadConfigAsync(ConfigPath);
                    ConfigContent = content;
                    IsConfigModified = false;

                    SetStatus("Configuration loaded successfully", false);
                    _logger.LogInformation("Configuration loaded from: {ConfigPath}", ConfigPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading configuration");
                    SetStatus($"Error loading configuration: {ex.Message}", true);
                    await _dialogService.ShowErrorAsync(
                        $"Failed to load configuration: {ex.Message}",
                        "Load Error");
                }
            }, "Loading configuration...");
        }

        private async Task SaveConfigAsync()
        {
            await ExecuteBusyAsync(async () =>
            {
                try
                {
                    await _configService.SaveConfigAsync(ConfigPath, ConfigContent);
                    IsConfigModified = false;

                    SetStatus("Configuration saved successfully", false);
                    _logger.LogInformation("Configuration saved to: {ConfigPath}", ConfigPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving configuration");
                    SetStatus($"Error saving configuration: {ex.Message}", true);
                    await _dialogService.ShowErrorAsync(
                        $"Failed to save configuration: {ex.Message}",
                        "Save Error");
                }
            }, "Saving configuration...");
        }

        private async Task SaveAndRestartAsync()
        {
            await ExecuteBusyAsync(async () =>
            {
                try
                {
                    // Save configuration first
                    await _configService.SaveConfigAsync(ConfigPath, ConfigContent);
                    IsConfigModified = false;

                    // Then restart service
                    await _serviceController.RestartServiceAsync(ServiceName);
                    await RefreshServiceStatusAsync();

                    SetStatus("Configuration saved and service restarted successfully", false);
                    _logger.LogInformation("Configuration saved and service restarted");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving configuration and restarting service");
                    SetStatus($"Error saving and restarting: {ex.Message}", true);
                    await _dialogService.ShowErrorAsync(
                        $"Failed to save configuration and restart service: {ex.Message}",
                        "Save and Restart Error");
                }
            }, "Saving configuration and restarting service...");
        }

        private async Task StartServiceAsync()
        {
            await ExecuteBusyAsync(async () =>
            {
                try
                {
                    await _serviceController.StartServiceAsync(ServiceName);
                    await RefreshServiceStatusAsync();

                    SetStatus("Service started successfully", false);
                    _logger.LogInformation("Service started: {ServiceName}", ServiceName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error starting service");
                    SetStatus($"Error starting service: {ex.Message}", true);
                    await _dialogService.ShowErrorAsync(
                        $"Failed to start service: {ex.Message}",
                        "Start Service Error");
                }
            }, "Starting service...");
        }

        private async Task StopServiceAsync()
        {
            await ExecuteBusyAsync(async () =>
            {
                try
                {
                    await _serviceController.StopServiceAsync(ServiceName);
                    await RefreshServiceStatusAsync();

                    SetStatus("Service stopped successfully", false);
                    _logger.LogInformation("Service stopped: {ServiceName}", ServiceName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error stopping service");
                    SetStatus($"Error stopping service: {ex.Message}", true);
                    await _dialogService.ShowErrorAsync(
                        $"Failed to stop service: {ex.Message}",
                        "Stop Service Error");
                }
            }, "Stopping service...");
        }

        private async Task RestartServiceAsync()
        {
            await ExecuteBusyAsync(async () =>
            {
                try
                {
                    await _serviceController.RestartServiceAsync(ServiceName);
                    await RefreshServiceStatusAsync();

                    SetStatus("Service restarted successfully", false);
                    _logger.LogInformation("Service restarted: {ServiceName}", ServiceName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error restarting service");
                    SetStatus($"Error restarting service: {ex.Message}", true);
                    await _dialogService.ShowErrorAsync(
                        $"Failed to restart service: {ex.Message}",
                        "Restart Service Error");
                }
            }, "Restarting service...");
        }

        private async Task DetectServiceAsync()
        {
            try
            {
                var services = await _serviceController.FindServicesAsync("privoxy");
                var service = services.FirstOrDefault();

                if (service != null)
                {
                    ServiceName = service.ServiceName;
                    await RefreshServiceStatusAsync();

                    SetStatus($"Detected service: {service.ServiceName}", false);
                    _logger.LogInformation("Service detected: {ServiceName}", service.ServiceName);
                }
                else
                {
                    await _dialogService.ShowInformationAsync(
                        "Privoxy service not found. Please ensure Privoxy is installed as a Windows service.",
                        "Service Detection");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting service");
                SetStatus($"Error detecting service: {ex.Message}", true);
                await _dialogService.ShowErrorAsync(
                    $"Failed to detect service: {ex.Message}",
                    "Detection Error");
            }
        }

        private async Task ValidateConfigAsync()
        {
            await ExecuteBusyAsync(async () =>
            {
                try
                {
                    var result = await _configService.ValidateConfigAsync(ConfigContent);

                    if (result.IsValid)
                    {
                        SetStatus("Configuration is valid", false);
                        await _dialogService.ShowInformationAsync(
                            "The configuration file is valid and contains no errors.",
                            "Configuration Validation");
                    }
                    else
                    {
                        var message = result.Errors.Count > 0
                            ? $"Configuration validation found {result.Errors.Count} error(s):\n\n" +
                              string.Join("\n", result.Errors.Take(10))
                            : "Configuration validation completed with warnings.";

                        SetStatus("Configuration validation failed", true);
                        await _dialogService.ShowWarningAsync(message, "Validation Results");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating configuration");
                    SetStatus($"Error validating configuration: {ex.Message}", true);
                    await _dialogService.ShowErrorAsync(
                        $"Failed to validate configuration: {ex.Message}",
                        "Validation Error");
                }
            }, "Validating configuration...");
        }

        #endregion

        #region Helper Methods

        private async Task RefreshServiceStatusAsync()
        {
            try
            {
                var status = await _serviceController.GetServiceStatusAsync(ServiceName);
                var isRunning = status == ServiceControllerStatus.Running;

                IsServiceRunning = isRunning;
                ServiceStatus = $"Status: {status}";
            }
            catch (Exception ex)
            {
                IsServiceRunning = false;
                ServiceStatus = $"Status: Error - {ex.Message}";
                _logger.LogError(ex, "Error refreshing service status");
            }
        }

        private async Task AddToRecentConfigsAsync(string configPath)
        {
            if (!string.IsNullOrEmpty(configPath))
            {
                await _settingsService.AddRecentConfigAsync(configPath);
                var recentConfigs = await _settingsService.GetRecentConfigsAsync();
                RecentConfigs = recentConfigs;
            }
        }

        private void SetStatus(string message, bool isError)
        {
            StatusMessage = message;
            IsStatusError = isError;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the ViewModel with default values.
        /// </summary>
        private async Task InitializeConfigAsync()
        {
            try
            {
                var settings = await _settingsService.GetSettingsAsync();
                ConfigPath = settings.DefaultConfigPath;
                ServiceName = settings.DefaultServiceName;

                var recentConfigs = await _settingsService.GetRecentConfigsAsync();
                RecentConfigs = recentConfigs;

                await RefreshServiceStatusAsync();

                SetStatus("Ready", false);
                _logger.LogInformation("ConfigViewModel initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing ConfigViewModel");
                SetStatus($"Error initializing: {ex.Message}", true);
            }
        }

        #endregion

        #region BaseViewModel Overrides

        /// <inheritdoc />
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            await InitializeConfigAsync();
        }

        #endregion
    }
}