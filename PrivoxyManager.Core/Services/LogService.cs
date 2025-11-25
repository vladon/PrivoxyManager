using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PrivoxyManager.Services.Interfaces;
using LogLevel = PrivoxyManager.Services.Interfaces.LogLevel;

namespace PrivoxyManager.Services
{
    /// <summary>
    /// Provides functionality for monitoring Privoxy log files.
    /// </summary>
    public class LogService : ILogService, IDisposable
    {
        private readonly ILogger<LogService> _logger;
        private readonly ConcurrentQueue<LogEntry> _logEntries = new();
        private readonly object _lockObject = new();
        private FileStream? _logStream;
        private StreamReader? _logReader;
        private System.Timers.Timer? _monitorTimer;
        private int _maxLogEntries = 1000;
        private LogMonitoringStatus _status = LogMonitoringStatus.Stopped;
        private string? _logPath;

        /// <inheritdoc />
        public event EventHandler<LogEntryEventArgs>? LogEntryAdded;

        /// <inheritdoc />
        public event EventHandler<LogStatusChangedEventArgs>? StatusChanged;

        /// <inheritdoc />
        public LogMonitoringStatus Status
        {
            get => _status;
            private set
            {
                if (_status != value)
                {
                    _status = value;
                    StatusChanged?.Invoke(this, new LogStatusChangedEventArgs(value));
                }
            }
        }

        /// <inheritdoc />
        public string? LogPath => _logPath;

        /// <inheritdoc />
        public async Task StartMonitoringAsync(string logPath, int refreshInterval = 300)
        {
            try
            {
                if (Status == LogMonitoringStatus.Running)
                {
                    _logger.LogWarning("Log monitoring is already running");
                    return;
                }

                Status = LogMonitoringStatus.Starting;
                _logPath = logPath;

                _logger.LogInformation("Starting log monitoring for: {LogPath}", logPath);

                if (!File.Exists(logPath))
                {
                    Status = LogMonitoringStatus.Error;
                    StatusChanged?.Invoke(this, new LogStatusChangedEventArgs(
                        LogMonitoringStatus.Error, $"Log file not found: {logPath}"));
                    return;
                }

                // Close previous if exists
                await StopMonitoringAsync();

                _logStream = new FileStream(
                    logPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite);

                _logReader = new StreamReader(_logStream, Encoding.UTF8);

                // Move to end of file to read only new entries
                _logStream.Seek(0, SeekOrigin.End);

                _monitorTimer = new System.Timers.Timer(refreshInterval);
                _monitorTimer.Elapsed += OnMonitorTimer;
                _monitorTimer.AutoReset = true;
                _monitorTimer.Start();

                Status = LogMonitoringStatus.Running;

                // Add system entry
                AddLogEntry(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Level = LogLevel.Info,
                    Message = $"Log monitoring started for: {logPath}",
                    OriginalLine = $"--- Log started: {DateTime.Now} ---",
                    IsSystemEntry = true,
                    Color = "#0078D4"
                });

                _logger.LogInformation("Log monitoring started successfully");
            }
            catch (Exception ex)
            {
                Status = LogMonitoringStatus.Error;
                StatusChanged?.Invoke(this, new LogStatusChangedEventArgs(
                    LogMonitoringStatus.Error, ex.Message));
                _logger.LogError(ex, "Error starting log monitoring");
            }
        }

        /// <inheritdoc />
        public async Task StopMonitoringAsync()
        {
            try
            {
                if (Status != LogMonitoringStatus.Running)
                {
                    return;
                }

                _logger.LogInformation("Stopping log monitoring");

                _monitorTimer?.Stop();
                _monitorTimer?.Dispose();
                _monitorTimer = null;

                _logReader?.Dispose();
                _logReader = null;

                _logStream?.Dispose();
                _logStream = null;

                Status = LogMonitoringStatus.Stopped;

                // Add system entry
                AddLogEntry(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Level = LogLevel.Info,
                    Message = "Log monitoring stopped",
                    OriginalLine = $"--- Log stopped: {DateTime.Now} ---",
                    IsSystemEntry = true,
                    Color = "#605E5C"
                });

                _logger.LogInformation("Log monitoring stopped successfully");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping log monitoring");
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<LogEntry>> GetLogEntriesAsync(string? filter = null)
        {
            await Task.CompletedTask;
            
            lock (_lockObject)
            {
                var entries = _logEntries.ToList();
                
                if (!string.IsNullOrEmpty(filter))
                {
                    entries = entries
                        .Where(e => e.Message.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                                   e.OriginalLine.Contains(filter, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                return entries;
            }
        }

        /// <inheritdoc />
        public async Task ClearLogEntriesAsync()
        {
            try
            {
                lock (_lockObject)
                {
                    _logEntries.Clear();
                }

                _logger.LogInformation("Log entries cleared");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing log entries");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<LogEntry>> GetFilteredLogEntriesAsync(string filter, int maxEntries = 1000)
        {
            var entries = await GetLogEntriesAsync(filter);
            return entries.TakeLast(maxEntries);
        }

        /// <inheritdoc />
        public void SetMaxLogEntries(int maxEntries)
        {
            if (maxEntries <= 0)
            {
                throw new ArgumentException("Max entries must be greater than 0", nameof(maxEntries));
            }

            _maxLogEntries = maxEntries;

            lock (_lockObject)
            {
                // Remove excess entries if needed
                while (_logEntries.Count > _maxLogEntries)
                {
                    _logEntries.TryDequeue(out _);
                }
            }

            _logger.LogInformation("Max log entries set to: {MaxEntries}", maxEntries);
        }

        /// <inheritdoc />
        public async Task<LogStatistics> GetLogStatisticsAsync()
        {
            await Task.CompletedTask;
            
            lock (_lockObject)
            {
                var entries = _logEntries.ToList();
                
                return new LogStatistics
                {
                    TotalEntries = entries.Count,
                    DebugEntries = entries.Count(e => e.Level == LogLevel.Debug),
                    InfoEntries = entries.Count(e => e.Level == LogLevel.Info),
                    WarningEntries = entries.Count(e => e.Level == LogLevel.Warning),
                    ErrorEntries = entries.Count(e => e.Level == LogLevel.Error),
                    FatalEntries = entries.Count(e => e.Level == LogLevel.Fatal),
                    OldestEntry = entries.FirstOrDefault()?.Timestamp,
                    NewestEntry = entries.LastOrDefault()?.Timestamp
                };
            }
        }

        private void OnMonitorTimer(object? sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (_logReader == null)
                    return;

                while (!_logReader.EndOfStream)
                {
                    var line = _logReader.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        var logEntry = ParseLogLine(line);
                        AddLogEntry(logEntry);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading log file");
                Status = LogMonitoringStatus.Error;
                StatusChanged?.Invoke(this, new LogStatusChangedEventArgs(
                    LogMonitoringStatus.Error, ex.Message));
            }
        }

        private void AddLogEntry(LogEntry entry)
        {
            lock (_lockObject)
            {
                _logEntries.Enqueue(entry);

                // Remove excess entries
                while (_logEntries.Count > _maxLogEntries)
                {
                    _logEntries.TryDequeue(out _);
                }
            }

            LogEntryAdded?.Invoke(this, new LogEntryEventArgs(entry));
        }

        private static LogEntry ParseLogLine(string line)
        {
            var timestamp = DateTime.Now;
            var level = LogLevel.Info;
            var color = "#605E5C";

            var upperLine = line.ToUpperInvariant();

            if (upperLine.Contains("ERROR") || upperLine.Contains("FATAL"))
            {
                level = upperLine.Contains("FATAL") ? LogLevel.Fatal : LogLevel.Error;
                color = upperLine.Contains("FATAL") ? "#D13438" : "#DC3C3C";
            }
            else if (upperLine.Contains("WARNING") || upperLine.Contains("WARN"))
            {
                level = LogLevel.Warning;
                color = "#FFC000";
            }
            else if (upperLine.Contains("CONNECT") || upperLine.Contains("REQUEST"))
            {
                level = LogLevel.Info;
                color = "#107C10";
            }
            else if (upperLine.Contains("CONFIG") || upperLine.Contains("INITIALIZ") || upperLine.Contains("START"))
            {
                level = LogLevel.Info;
                color = "#0078D4";
            }
            else if (upperLine.Contains("DEBUG"))
            {
                level = LogLevel.Debug;
                color = "#A0A0A0";
            }

            // Try to extract timestamp from log line
            var timestampMatch = Regex.Match(line, @"(\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2})");
            if (timestampMatch.Success && DateTime.TryParse(timestampMatch.Groups[1].Value, out var parsedTimestamp))
            {
                timestamp = parsedTimestamp;
            }

            return new LogEntry
            {
                Timestamp = timestamp,
                Level = level,
                Message = line,
                OriginalLine = line,
                Color = color,
                IsSystemEntry = false
            };
        }

        public void Dispose()
        {
            try
            {
                _monitorTimer?.Dispose();
                _logReader?.Dispose();
                _logStream?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing LogService");
            }
        }
    }
}