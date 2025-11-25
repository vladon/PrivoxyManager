using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PrivoxyManager.Services.Interfaces;

namespace PrivoxyManager.Services
{
    /// <summary>
    /// Provides functionality for managing application settings.
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private readonly ILogger<SettingsService> _logger;
        private readonly string _settingsFilePath;
        private AppSettings? _cachedSettings;

        /// <summary>
        /// Initializes a new instance of the SettingsService class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public SettingsService(ILogger<SettingsService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "PrivoxyManager");
            
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }

            _settingsFilePath = Path.Combine(appFolder, "settings.json");
        }

        /// <inheritdoc />
        public async Task<AppSettings> GetSettingsAsync()
        {
            try
            {
                if (_cachedSettings != null)
                {
                    return _cachedSettings;
                }

                _logger.LogInformation("Loading settings from: {SettingsPath}", _settingsFilePath);

                if (!File.Exists(_settingsFilePath))
                {
                    _logger.LogInformation("Settings file not found, creating default settings");
                    _cachedSettings = new AppSettings();
                    await SaveSettingsAsync(_cachedSettings);
                    return _cachedSettings;
                }

                var json = await File.ReadAllTextAsync(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _cachedSettings = settings ?? new AppSettings();
                _logger.LogInformation("Settings loaded successfully");
                return _cachedSettings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading settings from: {SettingsPath}", _settingsFilePath);
                _cachedSettings = new AppSettings();
                return _cachedSettings;
            }
        }

        /// <inheritdoc />
        public async Task SaveSettingsAsync(AppSettings settings)
        {
            try
            {
                _logger.LogInformation("Saving settings to: {SettingsPath}", _settingsFilePath);

                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(_settingsFilePath, json);
                _cachedSettings = settings;

                _logger.LogInformation("Settings saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving settings to: {SettingsPath}", _settingsFilePath);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<T> GetSettingAsync<T>(string key, T defaultValue = default!)
        {
            try
            {
                var settings = await GetSettingsAsync();
                var property = typeof(AppSettings).GetProperty(key);
                
                if (property != null)
                {
                    var value = property.GetValue(settings);
                    if (value is T typedValue)
                    {
                        return typedValue;
                    }
                }

                _logger.LogWarning("Setting not found or invalid type: {Key}", key);
                return defaultValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting setting: {Key}", key);
                return defaultValue;
            }
        }

        /// <inheritdoc />
        public async Task SetSettingAsync<T>(string key, T value)
        {
            try
            {
                var settings = await GetSettingsAsync();
                var property = typeof(AppSettings).GetProperty(key);
                
                if (property != null && property.CanWrite)
                {
                    property.SetValue(settings, value);
                    await SaveSettingsAsync(settings);
                    _logger.LogInformation("Setting updated: {Key} = {Value}", key, value);
                }
                else
                {
                    _logger.LogWarning("Setting not found or read-only: {Key}", key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting value: {Key} = {Value}", key, value);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task ResetSettingsAsync()
        {
            try
            {
                _logger.LogInformation("Resetting settings to default values");
                
                _cachedSettings = new AppSettings();
                await SaveSettingsAsync(_cachedSettings);
                
                _logger.LogInformation("Settings reset successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting settings");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task AddRecentConfigAsync(string configPath)
        {
            try
            {
                var settings = await GetSettingsAsync();
                var recentConfigs = await GetRecentConfigsAsync();

                // Remove if already exists
                recentConfigs.RemoveAll(path => string.Equals(path, configPath, StringComparison.OrdinalIgnoreCase));
                
                // Add to the beginning
                recentConfigs.Insert(0, configPath);
                
                // Keep only the maximum number
                while (recentConfigs.Count > settings.MaxRecentConfigs)
                {
                    recentConfigs.RemoveAt(recentConfigs.Count - 1);
                }

                // Save the updated list
                var recentConfigsJson = JsonSerializer.Serialize(recentConfigs);
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var appFolder = Path.Combine(appDataPath, "PrivoxyManager");
                var recentConfigsPath = Path.Combine(appFolder, "recent_configs.json");

                await File.WriteAllTextAsync(recentConfigsPath, recentConfigsJson);
                
                _logger.LogInformation("Added recent config: {ConfigPath}", configPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding recent config: {ConfigPath}", configPath);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<List<string>> GetRecentConfigsAsync()
        {
            try
            {
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var appFolder = Path.Combine(appDataPath, "PrivoxyManager");
                var recentConfigsPath = Path.Combine(appFolder, "recent_configs.json");

                if (!File.Exists(recentConfigsPath))
                {
                    return new List<string>();
                }

                var json = await File.ReadAllTextAsync(recentConfigsPath);
                var recentConfigs = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();

                // Filter out non-existent files
                var validConfigs = recentConfigs.Where(File.Exists).ToList();

                _logger.LogInformation("Loaded {Count} recent configurations", validConfigs.Count);
                return validConfigs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading recent configurations");
                return new List<string>();
            }
        }

        /// <inheritdoc />
        public async Task ClearRecentConfigsAsync()
        {
            try
            {
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var appFolder = Path.Combine(appDataPath, "PrivoxyManager");
                var recentConfigsPath = Path.Combine(appFolder, "recent_configs.json");

                if (File.Exists(recentConfigsPath))
                {
                    File.Delete(recentConfigsPath);
                }

                _logger.LogInformation("Recent configurations cleared");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing recent configurations");
                throw;
            }
        }
    }
}