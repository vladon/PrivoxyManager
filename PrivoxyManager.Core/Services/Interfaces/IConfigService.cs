using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrivoxyManager.Services.Interfaces
{
    /// <summary>
    /// Provides functionality for managing Privoxy configuration files.
    /// </summary>
    public interface IConfigService
    {
        /// <summary>
        /// Loads the configuration file content.
        /// </summary>
        /// <param name="configPath">The path to the configuration file.</param>
        /// <returns>The configuration file content.</returns>
        Task<string> LoadConfigAsync(string configPath);

        /// <summary>
        /// Saves the configuration file content.
        /// </summary>
        /// <param name="configPath">The path to the configuration file.</param>
        /// <param name="content">The configuration content to save.</param>
        /// <returns>A task representing the save operation.</returns>
        Task SaveConfigAsync(string configPath, string content);

        /// <summary>
        /// Validates if the configuration file exists.
        /// </summary>
        /// <param name="configPath">The path to the configuration file.</param>
        /// <returns>True if the configuration file exists; otherwise, false.</returns>
        bool ConfigExists(string configPath);

        /// <summary>
        /// Gets the default log file path based on the configuration file path.
        /// </summary>
        /// <param name="configPath">The path to the configuration file.</param>
        /// <returns>The default log file path.</returns>
        string GetDefaultLogPath(string configPath);

        /// <summary>
        /// Validates the configuration content for basic syntax errors.
        /// </summary>
        /// <param name="content">The configuration content to validate.</param>
        /// <returns>A validation result with any errors found.</returns>
        Task<ConfigValidationResult> ValidateConfigAsync(string content);
    }

    /// <summary>
    /// Represents the result of a configuration validation.
    /// </summary>
    public class ConfigValidationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the configuration is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the list of validation errors.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of validation warnings.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();
    }
}