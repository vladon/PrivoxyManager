using System;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using PrivoxyManager.Commands;
using PrivoxyManager.Services.Interfaces;

namespace PrivoxyManager.ViewModels
{
    /// <summary>
    /// ViewModel for the main window.
    /// </summary>
    public class MainWindowViewModel : BaseViewModel
    {
        private readonly ILogger<MainWindowViewModel> _logger;
        private readonly IDialogService _dialogService;
        private readonly ISettingsService _settingsService;

        private bool _isAdministrator;
        private int _selectedTabIndex;
        private string _statusMessage = string.Empty;
        private bool _isStatusError;

        /// <summary>
        /// Initializes a new instance of the MainWindowViewModel class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="dialogService">The dialog service.</param>
        /// <param name="settingsService">The settings service.</param>
        public MainWindowViewModel(
            ILogger<MainWindowViewModel> logger,
            IDialogService dialogService,
            ISettingsService settingsService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            InitializeCommands();
            _ = InitializeProperties();
        }

        #region Properties

        /// <summary>
        /// Gets a value indicating whether the application is running with administrator privileges.
        /// </summary>
        public bool IsAdministrator
        {
            get => _isAdministrator;
            private set => SetProperty(ref _isAdministrator, value);
        }

        /// <summary>
        /// Gets or sets the selected tab index.
        /// </summary>
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        /// <summary>
        /// Gets or sets the status message to display.
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

        #endregion

        #region Commands

        /// <summary>
        /// Gets the command to run the application as administrator.
        /// </summary>
        public ICommand RunAsAdminCommand { get; private set; }

        /// <summary>
        /// Gets the command to show the about dialog.
        /// </summary>
        public ICommand ShowAboutCommand { get; private set; }

        /// <summary>
        /// Gets the command to show the settings dialog.
        /// </summary>
        public ICommand ShowSettingsCommand { get; private set; }

        /// <summary>
        /// Gets the command to exit the application.
        /// </summary>
        public ICommand ExitCommand { get; private set; }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            RunAsAdminCommand = new RelayCommand(async () => await RunAsAdminAsync(), () => !IsAdministrator);
            ShowAboutCommand = new RelayCommand(async () => await ShowAboutAsync());
            ShowSettingsCommand = new RelayCommand(async () => await ShowSettingsAsync());
            ExitCommand = new RelayCommand(Exit);
        }

        private async Task InitializeProperties()
        {
            try
            {
                IsAdministrator = IsRunningAsAdministrator();

                var settings = await _settingsService.GetSettingsAsync();
                SelectedTabIndex = settings.SelectedTabIndex;

                SetStatus("Application initialized successfully", false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing MainWindowViewModel");
                SetStatus($"Error initializing application: {ex.Message}", true);
            }
        }

        #endregion

        #region Command Implementations

        private async Task RunAsAdminAsync()
        {
            try
            {
                if (IsAdministrator)
                return;

                var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    FileName = currentProcess.MainModule?.FileName
                };

                System.Diagnostics.Process.Start(startInfo);

                // Request application shutdown
                await _dialogService.ShowInformationAsync(
                    "Application will restart with administrator privileges.",
                    "Run as Administrator");

                // Signal to close the application
                System.Windows.Application.Current?.Shutdown();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restarting as administrator");
                await _dialogService.ShowErrorAsync(
                    $"Failed to restart as administrator: {ex.Message}",
                    "Error");
            }
        }

        private async Task ShowAboutAsync()
        {
            try
            {
                var version = System.Reflection.Assembly.GetExecutingAssembly()
                    .GetName().Version?.ToString() ?? "Unknown";

                var message = $"Privoxy Manager\n\nVersion: {version}\n\n" +
                           "A modern WPF application for managing Privoxy proxy server.\n\n" +
                           "Features:\n" +
                           "• Configuration file management\n" +
                           "• Windows service control\n" +
                           "• Real-time log monitoring\n" +
                           "• Configuration validation\n" +
                           "• Backup and restore functionality";

                await _dialogService.ShowInformationAsync(message, "About Privoxy Manager");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing about dialog");
                await _dialogService.ShowErrorAsync(
                    $"Failed to show about dialog: {ex.Message}",
                    "Error");
            }
        }

        private async Task ShowSettingsAsync()
        {
            try
            {
                await _dialogService.ShowInformationAsync(
                    "Settings dialog will be implemented in a future version.",
                    "Settings");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing settings dialog");
                await _dialogService.ShowErrorAsync(
                    $"Failed to show settings dialog: {ex.Message}",
                    "Error");
            }
        }

        private void Exit()
        {
            try
            {
                System.Windows.Application.Current?.Shutdown();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exiting application");
            }
        }

        #endregion

        #region Helper Methods

        private static bool IsRunningAsAdministrator()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private void SetStatus(string message, bool isError)
        {
            StatusMessage = message;
            IsStatusError = isError;
            
            if (isError)
            {
                _logger.LogWarning("Status error: {Message}", message);
            }
            else
            {
                _logger.LogInformation("Status: {Message}", message);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Saves the current state to settings.
        /// </summary>
        public async Task SaveStateAsync()
        {
            try
            {
                var settings = await _settingsService.GetSettingsAsync();
                settings.SelectedTabIndex = SelectedTabIndex;
                await _settingsService.SaveSettingsAsync(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving state");
                SetStatus($"Error saving state: {ex.Message}", true);
            }
        }

        #endregion

        #region BaseViewModel Overrides

        /// <inheritdoc />
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            await InitializeProperties();
        }

        #endregion
    }
}