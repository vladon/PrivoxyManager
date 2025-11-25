using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using PrivoxyManager.Commands;
using PrivoxyManager.Services.Interfaces;

namespace PrivoxyManager.ViewModels
{
    /// <summary>
    /// ViewModel for the log monitoring tab.
    /// </summary>
    public class LogViewModel : BaseViewModel, IDisposable
    {
        private readonly ILogger<LogViewModel> _logger;
        private readonly ILogService _logService;
        private readonly IDialogService _dialogService;
        private readonly ISettingsService _settingsService;

        private string _filterText = string.Empty;
        private bool _autoScroll = true;
        private LogMonitoringStatus _status = LogMonitoringStatus.Stopped;
        private string _logPath = string.Empty;
        private List<LogEntry> _logEntries = new();
        private List<LogEntry> _filteredLogEntries = new();
        private bool _isMonitoring;

        /// <summary>
        /// Initializes a new instance of the LogViewModel class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="logService">The log service.</param>
        /// <param name="dialogService">The dialog service.</param>
        /// <param name="settingsService">The settings service.</param>
        public LogViewModel(
            ILogger<LogViewModel> logger,
            ILogService logService,
            IDialogService dialogService,
            ISettingsService settingsService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            InitializeCommands();
            SubscribeToLogService();
        }

        #region Properties

        /// <summary>
        /// Gets or sets the filter text for log entries.
        /// </summary>
        public string FilterText
        {
            get => _filterText;
            set
            {
                if (SetProperty(ref _filterText, value))
                {
                    ApplyFilter();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to auto-scroll to new log entries.
        /// </summary>
        public bool AutoScroll
        {
            get => _autoScroll;
            set => SetProperty(ref _autoScroll, value);
        }

        /// <summary>
        /// Gets the current log monitoring status.
        /// </summary>
        public LogMonitoringStatus Status
        {
            get => _status;
            private set
            {
                if (SetProperty(ref _status, value))
                {
                    IsMonitoring = value == LogMonitoringStatus.Running;
                }
            }
        }

        /// <summary>
        /// Gets or sets the path to the log file being monitored.
        /// </summary>
        public string LogPath
        {
            get => _logPath;
            private set => SetProperty(ref _logPath, value);
        }

        /// <summary>
        /// Gets the list of all log entries.
        /// </summary>
        public List<LogEntry> LogEntries
        {
            get => _logEntries;
            private set => SetProperty(ref _logEntries, value);
        }

        /// <summary>
        /// Gets the list of filtered log entries.
        /// </summary>
        public List<LogEntry> FilteredLogEntries
        {
            get => _filteredLogEntries;
            private set => SetProperty(ref _filteredLogEntries, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether log monitoring is active.
        /// </summary>
        public bool IsMonitoring
        {
            get => _isMonitoring;
            set => SetProperty(ref _isMonitoring, value);
        }

        /// <summary>
        /// Gets a value indicating whether any log entries exist.
        /// </summary>
        public bool HasLogEntries => LogEntries.Count > 0;

        /// <summary>
        /// Gets the count of filtered log entries.
        /// </summary>
        public int FilteredCount => FilteredLogEntries.Count;

        /// <summary>
        /// Gets the status text for display.
        /// </summary>
        public string StatusText => Status switch
        {
            LogMonitoringStatus.Stopped => "Stopped",
            LogMonitoringStatus.Starting => "Starting...",
            LogMonitoringStatus.Running => "Running",
            LogMonitoringStatus.Error => "Error",
            _ => "Unknown"
        };

        #endregion

        #region Commands

        /// <summary>
        /// Gets the command to start log monitoring.
        /// </summary>
        public ICommand StartLogCommand { get; private set; }

        /// <summary>
        /// Gets the command to stop log monitoring.
        /// </summary>
        public ICommand StopLogCommand { get; private set; }

        /// <summary>
        /// Gets the command to clear log entries.
        /// </summary>
        public ICommand ClearLogCommand { get; private set; }

        /// <summary>
        /// Gets the command to clear the filter.
        /// </summary>
        public ICommand ClearFilterCommand { get; private set; }

        /// <summary>
        /// Gets the command to browse for a log file.
        /// </summary>
        public ICommand BrowseLogCommand { get; private set; }

        /// <summary>
        /// Gets the command to show log statistics.
        /// </summary>
        public ICommand ShowStatisticsCommand { get; private set; }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            StartLogCommand = new RelayCommand(async () => await StartMonitoringAsync(), () => Status != LogMonitoringStatus.Running);
            StopLogCommand = new RelayCommand(async () => await StopMonitoringAsync(), () => Status == LogMonitoringStatus.Running);
            ClearLogCommand = new RelayCommand(async () => await ClearLogAsync(), () => HasLogEntries);
            ClearFilterCommand = new RelayCommand(ClearFilter);
            BrowseLogCommand = new RelayCommand(async () => await BrowseForLogFileAsync());
            ShowStatisticsCommand = new RelayCommand(async () => await ShowStatisticsAsync(), () => HasLogEntries);
        }

        private void SubscribeToLogService()
        {
            _logService.LogEntryAdded += OnLogEntryAdded;
            _logService.StatusChanged += OnLogStatusChanged;
        }

        #endregion

        #region Command Implementations

        private async Task StartMonitoringAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(LogPath))
                {
                    await _dialogService.ShowWarningAsync(
                        "Please specify a log file path to monitor.",
                        "Log Path Required");
                    return;
                }

                var settings = await _settingsService.GetSettingsAsync();
                await _logService.StartMonitoringAsync(LogPath, settings.LogRefreshInterval);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting log monitoring");
                await _dialogService.ShowErrorAsync(
                    $"Failed to start log monitoring: {ex.Message}",
                    "Start Error");
            }
        }

        private async Task StopMonitoringAsync()
        {
            try
            {
                await _logService.StopMonitoringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping log monitoring");
                await _dialogService.ShowErrorAsync(
                    $"Failed to stop log monitoring: {ex.Message}",
                    "Stop Error");
            }
        }

        private async Task ClearLogAsync()
        {
            try
            {
                var confirmed = await _dialogService.ShowConfirmationAsync(
                    "Are you sure you want to clear all log entries?",
                    "Clear Log");

                if (confirmed)
                {
                    await _logService.ClearLogEntriesAsync();
                    LogEntries.Clear();
                    FilteredLogEntries.Clear();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing log entries");
                await _dialogService.ShowErrorAsync(
                    $"Failed to clear log entries: {ex.Message}",
                    "Clear Error");
            }
        }

        private void ClearFilter()
        {
            FilterText = string.Empty;
        }

        private async Task BrowseForLogFileAsync()
        {
            try
            {
                var logPath = await _dialogService.ShowOpenFileDialogAsync(
                    "Select Privoxy Log File",
                    "Privoxy log (privoxy.log)|privoxy.log|Log files (*.log)|*.log|All files (*.*)|*.*",
                    initialDirectory: System.IO.Path.GetDirectoryName(LogPath));

                if (!string.IsNullOrEmpty(logPath))
                {
                    LogPath = logPath;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error browsing for log file");
                await _dialogService.ShowErrorAsync(
                    $"Failed to browse for log file: {ex.Message}",
                    "Browse Error");
            }
        }

        private async Task ShowStatisticsAsync()
        {
            try
            {
                var stats = await _logService.GetLogStatisticsAsync();
                
                var message = $"Log Statistics:\n\n" +
                             $"Total Entries: {stats.TotalEntries:N0}\n" +
                             $"Debug: {stats.DebugEntries:N0}\n" +
                             $"Info: {stats.InfoEntries:N0}\n" +
                             $"Warnings: {stats.WarningEntries:N0}\n" +
                             $"Errors: {stats.ErrorEntries:N0}\n" +
                             $"Fatal: {stats.FatalEntries:N0}";

                if (stats.OldestEntry.HasValue)
                {
                    message += $"\n\nOldest Entry: {stats.OldestEntry.Value:yyyy-MM-dd HH:mm:ss}";
                }

                if (stats.NewestEntry.HasValue)
                {
                    message += $"\nNewest Entry: {stats.NewestEntry.Value:yyyy-MM-dd HH:mm:ss}";
                }

                await _dialogService.ShowInformationAsync(message, "Log Statistics");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing log statistics");
                await _dialogService.ShowErrorAsync(
                    $"Failed to show log statistics: {ex.Message}",
                    "Statistics Error");
            }
        }

        #endregion

        #region Event Handlers

        private void OnLogEntryAdded(object? sender, LogEntryEventArgs e)
        {
            LogEntries.Add(e.LogEntry);
            ApplyFilter();
        }

        private void OnLogStatusChanged(object? sender, LogStatusChangedEventArgs e)
        {
            Status = e.Status;
            LogPath = _logService.LogPath ?? string.Empty;

            if (e.Status == LogMonitoringStatus.Error && !string.IsNullOrEmpty(e.ErrorMessage))
            {
                _ = Task.Run(async () =>
                {
                    await _dialogService.ShowErrorAsync(
                        $"Log monitoring error: {e.ErrorMessage}",
                        "Monitoring Error");
                });
            }
        }

        #endregion

        #region Helper Methods

        private void ApplyFilter()
        {
            if (string.IsNullOrEmpty(FilterText))
            {
                FilteredLogEntries = new List<LogEntry>(LogEntries);
            }
            else
            {
                var filter = FilterText.ToLowerInvariant();
                FilteredLogEntries = LogEntries
                    .Where(entry => entry.Message.ToLowerInvariant().Contains(filter) ||
                                   entry.OriginalLine.ToLowerInvariant().Contains(filter))
                    .ToList();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the log file path and optionally starts monitoring.
        /// </summary>
        /// <param name="logPath">The path to the log file.</param>
        /// <param name="autoStart">Whether to automatically start monitoring.</param>
        public async Task SetLogPathAsync(string logPath, bool autoStart = false)
        {
            LogPath = logPath;

            if (autoStart && Status != LogMonitoringStatus.Running)
            {
                await StartMonitoringAsync();
            }
        }

        /// <summary>
        /// Initializes the ViewModel with settings.
        /// </summary>
        private async Task InitializeLogAsync()
        {
            try
            {
                var settings = await _settingsService.GetSettingsAsync();
                AutoScroll = settings.LogAutoScroll;

                _ = _logService.SetMaxLogEntriesAsync(settings.MaxLogEntries);

                _logger.LogInformation("LogViewModel initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing LogViewModel");
            }
        }

        #endregion

        #region BaseViewModel Overrides

        /// <inheritdoc />
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            await InitializeLogAsync();
        }

        /// <inheritdoc />
        protected override void DisposeCore()
        {
            try
            {
                _logService.LogEntryAdded -= OnLogEntryAdded;
                _logService.StatusChanged -= OnLogStatusChanged;

                if (_logService is IDisposable disposableLogService)
                {
                    disposableLogService.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing LogViewModel");
            }
        }

        #endregion
    }
}