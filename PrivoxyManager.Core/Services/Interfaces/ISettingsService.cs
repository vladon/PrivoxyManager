using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrivoxyManager.Services.Interfaces
{
    /// <summary>
    /// Provides functionality for managing application settings.
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// Gets the application settings.
        /// </summary>
        /// <returns>The current application settings.</returns>
        Task<AppSettings> GetSettingsAsync();

        /// <summary>
        /// Saves the application settings.
        /// </summary>
        /// <param name="settings">The settings to save.</param>
        /// <returns>A task representing the save operation.</returns>
        Task SaveSettingsAsync(AppSettings settings);

        /// <summary>
        /// Gets a specific setting value.
        /// </summary>
        /// <typeparam name="T">The type of the setting value.</typeparam>
        /// <param name="key">The setting key.</param>
        /// <param name="defaultValue">The default value if the setting doesn't exist.</param>
        /// <returns>The setting value.</returns>
        Task<T> GetSettingAsync<T>(string key, T defaultValue = default!);

        /// <summary>
        /// Sets a specific setting value.
        /// </summary>
        /// <typeparam name="T">The type of the setting value.</typeparam>
        /// <param name="key">The setting key.</param>
        /// <param name="value">The setting value.</param>
        /// <returns>A task representing the set operation.</returns>
        Task SetSettingAsync<T>(string key, T value);

        /// <summary>
        /// Resets all settings to their default values.
        /// </summary>
        /// <returns>A task representing the reset operation.</returns>
        Task ResetSettingsAsync();

        /// <summary>
        /// Adds a recent configuration file to the history.
        /// </summary>
        /// <param name="configPath">The path to the configuration file.</param>
        /// <returns>A task representing the add operation.</returns>
        Task AddRecentConfigAsync(string configPath);

        /// <summary>
        /// Gets the list of recent configuration files.
        /// </summary>
        /// <returns>A list of recent configuration file paths.</returns>
        Task<List<string>> GetRecentConfigsAsync();

        /// <summary>
        /// Clears the recent configuration history.
        /// </summary>
        /// <returns>A task representing the clear operation.</returns>
        Task ClearRecentConfigsAsync();
    }

    /// <summary>
    /// Represents the application settings.
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Gets or sets the default configuration file path.
        /// </summary>
        public string DefaultConfigPath { get; set; } = @"D:\tools\privoxy\config.txt";

        /// <summary>
        /// Gets or sets the default service name.
        /// </summary>
        public string DefaultServiceName { get; set; } = "Privoxy";

        /// <summary>
        /// Gets or sets the window width.
        /// </summary>
        public double WindowWidth { get; set; } = 1100;

        /// <summary>
        /// Gets or sets the window height.
        /// </summary>
        public double WindowHeight { get; set; } = 700;

        /// <summary>
        /// Gets or sets the window left position.
        /// </summary>
        public double WindowLeft { get; set; } = double.NaN;

        /// <summary>
        /// Gets or sets the window top position.
        /// </summary>
        public double WindowTop { get; set; } = double.NaN;

        /// <summary>
        /// Gets or sets the window state (normal, maximized, minimized).
        /// </summary>
        public string WindowState { get; set; } = "Normal";

        /// <summary>
        /// Gets or sets the selected tab index.
        /// </summary>
        public int SelectedTabIndex { get; set; } = 0;

        /// <summary>
        /// Gets or sets whether to start with Windows.
        /// </summary>
        public bool StartWithWindows { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to minimize to tray.
        /// </summary>
        public bool MinimizeToTray { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to close to tray instead of exiting.
        /// </summary>
        public bool CloseToTray { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to show notifications.
        /// </summary>
        public bool ShowNotifications { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of recent configurations to keep.
        /// </summary>
        public int MaxRecentConfigs { get; set; } = 10;

        /// <summary>
        /// Gets or sets the theme (Light, Dark, System).
        /// </summary>
        public string Theme { get; set; } = "System";

        /// <summary>
        /// Gets or sets the log auto-refresh interval in milliseconds.
        /// </summary>
        public int LogRefreshInterval { get; set; } = 300;

        /// <summary>
        /// Gets or sets whether auto-scroll is enabled in the log viewer.
        /// </summary>
        public bool LogAutoScroll { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of log entries to keep in memory.
        /// </summary>
        public int MaxLogEntries { get; set; } = 1000;

        /// <summary>
        /// Gets or sets whether to automatically create configuration backups.
        /// </summary>
        public bool AutoBackupConfig { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of backup files to keep.
        /// </summary>
        public int MaxBackupFiles { get; set; } = 10;

        /// <summary>
        /// Gets or sets the backup directory path.
        /// </summary>
        public string BackupDirectory { get; set; } = "";
    }
}