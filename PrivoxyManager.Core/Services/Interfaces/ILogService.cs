using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrivoxyManager.Services.Interfaces
{
    /// <summary>
    /// Provides functionality for monitoring Privoxy log files.
    /// </summary>
    public interface ILogService
    {
        /// <summary>
        /// Occurs when a new log entry is available.
        /// </summary>
        event EventHandler<LogEntryEventArgs>? LogEntryAdded;

        /// <summary>
        /// Occurs when the log monitoring status changes.
        /// </summary>
        event EventHandler<LogStatusChangedEventArgs>? StatusChanged;

        /// <summary>
        /// Gets the current status of log monitoring.
        /// </summary>
        LogMonitoringStatus Status { get; }

        /// <summary>
        /// Gets the current log file path.
        /// </summary>
        string? LogPath { get; }

        /// <summary>
        /// Starts monitoring the specified log file.
        /// </summary>
        /// <param name="logPath">The path to the log file.</param>
        /// <param name="refreshInterval">The refresh interval in milliseconds.</param>
        /// <returns>A task representing the start operation.</returns>
        Task StartMonitoringAsync(string logPath, int refreshInterval = 300);

        /// <summary>
        /// Stops monitoring the log file.
        /// </summary>
        /// <returns>A task representing the stop operation.</returns>
        Task StopMonitoringAsync();

        /// <summary>
        /// Gets all log entries currently in memory.
        /// </summary>
        /// <param name="filter">Optional filter text.</param>
        /// <returns>A list of log entries.</returns>
        Task<IEnumerable<LogEntry>> GetLogEntriesAsync(string? filter = null);

        /// <summary>
        /// Clears all log entries from memory.
        /// </summary>
        /// <returns>A task representing the clear operation.</returns>
        Task ClearLogEntriesAsync();

        /// <summary>
        /// Gets log entries that match the specified filter.
        /// </summary>
        /// <param name="filter">The filter text.</param>
        /// <param name="maxEntries">Maximum number of entries to return.</param>
        /// <returns>A list of filtered log entries.</returns>
        Task<IEnumerable<LogEntry>> GetFilteredLogEntriesAsync(string filter, int maxEntries = 1000);

        /// <summary>
        /// Sets the maximum number of log entries to keep in memory.
        /// </summary>
        /// <param name="maxEntries">Maximum number of entries.</param>
        void SetMaxLogEntries(int maxEntries);

        /// <summary>
        /// Gets statistics about the log entries.
        /// </summary>
        /// <returns>Log statistics.</returns>
        Task<LogStatistics> GetLogStatisticsAsync();
    }

    /// <summary>
    /// Represents the status of log monitoring.
    /// </summary>
    public enum LogMonitoringStatus
    {
        /// <summary>
        /// Not currently monitoring any log file.
        /// </summary>
        Stopped,

        /// <summary>
        /// Currently starting to monitor a log file.
        /// </summary>
        Starting,

        /// <summary>
        /// Actively monitoring a log file.
        /// </summary>
        Running,

        /// <summary>
        /// An error occurred during monitoring.
        /// </summary>
        Error
    }

    /// <summary>
    /// Represents a single log entry.
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Gets or sets the timestamp of the log entry.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the log level (Error, Warning, Info, Debug).
        /// </summary>
        public LogLevel Level { get; set; }

        /// <summary>
        /// Gets or sets the full log message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the original log line.
        /// </summary>
        public string OriginalLine { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the color to use when displaying this entry.
        /// </summary>
        public string Color { get; set; } = "#605E5C";

        /// <summary>
        /// Gets or sets a value indicating whether this is a system-generated entry.
        /// </summary>
        public bool IsSystemEntry { get; set; }
    }

    /// <summary>
    /// Represents the severity level of a log entry.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Debug information.
        /// </summary>
        Debug,

        /// <summary>
        /// General information.
        /// </summary>
        Info,

        /// <summary>
        /// Warning message.
        /// </summary>
        Warning,

        /// <summary>
        /// Error message.
        /// </summary>
        Error,

        /// <summary>
        /// Fatal error message.
        /// </summary>
        Fatal
    }

    /// <summary>
    /// Event arguments for log entry events.
    /// </summary>
    public class LogEntryEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the log entry.
        /// </summary>
        public LogEntry LogEntry { get; }

        /// <summary>
        /// Initializes a new instance of the LogEntryEventArgs class.
        /// </summary>
        /// <param name="logEntry">The log entry.</param>
        public LogEntryEventArgs(LogEntry logEntry)
        {
            LogEntry = logEntry;
        }
    }

    /// <summary>
    /// Event arguments for log status change events.
    /// </summary>
    public class LogStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the new status.
        /// </summary>
        public LogMonitoringStatus Status { get; }

        /// <summary>
        /// Gets the error message if status is Error.
        /// </summary>
        public string? ErrorMessage { get; }

        /// <summary>
        /// Initializes a new instance of the LogStatusChangedEventArgs class.
        /// </summary>
        /// <param name="status">The new status.</param>
        /// <param name="errorMessage">Optional error message.</param>
        public LogStatusChangedEventArgs(LogMonitoringStatus status, string? errorMessage = null)
        {
            Status = status;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// Represents statistics about log entries.
    /// </summary>
    public class LogStatistics
    {
        /// <summary>
        /// Gets or sets the total number of entries.
        /// </summary>
        public int TotalEntries { get; set; }

        /// <summary>
        /// Gets or sets the number of debug entries.
        /// </summary>
        public int DebugEntries { get; set; }

        /// <summary>
        /// Gets or sets the number of info entries.
        /// </summary>
        public int InfoEntries { get; set; }

        /// <summary>
        /// Gets or sets the number of warning entries.
        /// </summary>
        public int WarningEntries { get; set; }

        /// <summary>
        /// Gets or sets the number of error entries.
        /// </summary>
        public int ErrorEntries { get; set; }

        /// <summary>
        /// Gets or sets the number of fatal entries.
        /// </summary>
        public int FatalEntries { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the oldest entry.
        /// </summary>
        public DateTime? OldestEntry { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the newest entry.
        /// </summary>
        public DateTime? NewestEntry { get; set; }
    }
}