using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PrivoxyManager.Services.Interfaces;

namespace PrivoxyManager.Services
{
    /// <summary>
    /// Provides functionality for managing Privoxy configuration files.
    /// </summary>
    public class ConfigService : IConfigService
    {
        private readonly ILogger<ConfigService> _logger;

        /// <summary>
        /// Initializes a new instance of the ConfigService class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public ConfigService(ILogger<ConfigService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<string> LoadConfigAsync(string configPath)
        {
            try
            {
                _logger.LogInformation("Loading configuration from: {ConfigPath}", configPath);

                if (!ConfigExists(configPath))
                {
                    throw new FileNotFoundException($"Configuration file not found: {configPath}");
                }

                var content = await File.ReadAllTextAsync(configPath, Encoding.UTF8);
                _logger.LogInformation("Configuration loaded successfully. Size: {Size} bytes", content.Length);

                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading configuration from: {ConfigPath}", configPath);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task SaveConfigAsync(string configPath, string content)
        {
            try
            {
                _logger.LogInformation("Saving configuration to: {ConfigPath}", configPath);

                // Ensure directory exists
                var directory = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllTextAsync(configPath, content, Encoding.UTF8);
                _logger.LogInformation("Configuration saved successfully. Size: {Size} bytes", content.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving configuration to: {ConfigPath}", configPath);
                throw;
            }
        }

        /// <inheritdoc />
        public bool ConfigExists(string configPath)
        {
            return File.Exists(configPath);
        }

        /// <inheritdoc />
        public string GetDefaultLogPath(string configPath)
        {
            var configDirectory = Path.GetDirectoryName(configPath);
            if (string.IsNullOrEmpty(configDirectory))
            {
                return "privoxy.log";
            }

            return Path.Combine(configDirectory, "privoxy.log");
        }

        /// <inheritdoc />
        public async Task<ConfigValidationResult> ValidateConfigAsync(string content)
        {
            var result = new ConfigValidationResult();

            try
            {
                _logger.LogInformation("Validating configuration content");

                if (string.IsNullOrWhiteSpace(content))
                {
                    result.Errors.Add("Configuration content is empty");
                    return result;
                }

                var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var lineNumber = 0;

                foreach (var line in lines)
                {
                    lineNumber++;
                    var trimmedLine = line.Trim();

                    // Skip empty lines and comments
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                        continue;

                    // Basic syntax validation
                    if (!IsValidConfigLine(trimmedLine))
                    {
                        result.Errors.Add($"Line {lineNumber}: Invalid syntax - '{trimmedLine}'");
                        continue;
                    }

                    // Check for common configuration issues
                    ValidateCommonIssues(trimmedLine, lineNumber, result);
                }

                // Check for required configuration directives
                ValidateRequiredDirectives(content, result);

                result.IsValid = result.Errors.Count == 0;

                _logger.LogInformation("Configuration validation completed. Valid: {IsValid}, Errors: {ErrorCount}, Warnings: {WarningCount}",
                    result.IsValid, result.Errors.Count, result.Warnings.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during configuration validation");
                result.Errors.Add($"Validation error: {ex.Message}");
                return result;
            }
        }

        private static bool IsValidConfigLine(string line)
        {
            // Valid patterns:
            // 1. Key Value (e.g., "listen-address 127.0.0.1:8118")
            // 2. Key "Value with spaces" (e.g., "confdir \"C:\\Program Files\\Privoxy\"")
            // 3. Key (boolean directives without value)

            var quotedValuePattern = @"^[a-zA-Z0-9\-_]+\s+"".*""\s*$";
            var keyValuePattern = @"^[a-zA-Z0-9\-_]+\s+[^\s""#].*$";
            var keyOnlyPattern = @"^[a-zA-Z0-9\-_]+\s*$";

            return Regex.IsMatch(line, quotedValuePattern) ||
                   Regex.IsMatch(line, keyValuePattern) ||
                   Regex.IsMatch(line, keyOnlyPattern);
        }

        private static void ValidateCommonIssues(string line, int lineNumber, ConfigValidationResult result)
        {
            // Check for common issues
            if (line.Contains("listen-address", StringComparison.OrdinalIgnoreCase))
            {
                // Validate listen-address format
                var match = Regex.Match(line, @"listen-address\s+([^\s]+)");
                if (match.Success)
                {
                    var address = match.Groups[1].Value;
                    if (!IsValidListenAddress(address))
                    {
                        result.Warnings.Add($"Line {lineNumber}: Potentially invalid listen-address format - '{address}'");
                    }
                }
            }

            if (line.Contains("confdir", StringComparison.OrdinalIgnoreCase))
            {
                // Check if confdir path exists (warning only)
                var match = Regex.Match(line, @"confdir\s+""?([^\s""]+)?");
                if (match.Success)
                {
                    var path = match.Groups[1].Value;
                    if (!Directory.Exists(path))
                    {
                        result.Warnings.Add($"Line {lineNumber}: confdir path does not exist - '{path}'");
                    }
                }
            }

            if (line.Contains("logfile", StringComparison.OrdinalIgnoreCase))
            {
                // Check if log directory is writable (warning only)
                var match = Regex.Match(line, @"logfile\s+""?([^\s""]+)?");
                if (match.Success)
                {
                    var logPath = match.Groups[1].Value;
                    var logDir = Path.GetDirectoryName(logPath);
                    if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                    {
                        result.Warnings.Add($"Line {lineNumber}: Log directory does not exist - '{logDir}'");
                    }
                }
            }
        }

        private static void ValidateRequiredDirectives(string content, ConfigValidationResult result)
        {
            // Check for recommended (but not required) directives
            if (!content.Contains("listen-address", StringComparison.OrdinalIgnoreCase))
            {
                result.Warnings.Add("No listen-address directive found - Privoxy may not be accessible");
            }

            if (!content.Contains("confdir", StringComparison.OrdinalIgnoreCase))
            {
                result.Warnings.Add("No confdir directive found - Using default configuration directory");
            }
        }

        private static bool IsValidListenAddress(string address)
        {
            // Basic validation for IP:PORT format
            var ipPortPattern = @"^\d{1,3}\.\d{1,3}\.\d{1,3}:\d{1,5}$";
            return Regex.IsMatch(address, ipPortPattern);
        }
    }
}