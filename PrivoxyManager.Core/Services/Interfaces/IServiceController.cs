using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace PrivoxyManager.Services.Interfaces
{
    /// <summary>
    /// Provides functionality for managing Windows services.
    /// </summary>
    public interface IServiceController
    {
        /// <summary>
        /// Gets the current status of the specified service.
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <returns>The current service status.</returns>
        Task<ServiceControllerStatus> GetServiceStatusAsync(string serviceName);

        /// <summary>
        /// Starts the specified service.
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="timeout">The timeout to wait for the service to start.</param>
        /// <returns>A task representing the start operation.</returns>
        Task StartServiceAsync(string serviceName, TimeSpan? timeout = null);

        /// <summary>
        /// Stops the specified service.
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="timeout">The timeout to wait for the service to stop.</param>
        /// <returns>A task representing the stop operation.</returns>
        Task StopServiceAsync(string serviceName, TimeSpan? timeout = null);

        /// <summary>
        /// Restarts the specified service.
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="timeout">The timeout to wait for the service operations.</param>
        /// <returns>A task representing the restart operation.</returns>
        Task RestartServiceAsync(string serviceName, TimeSpan? timeout = null);

        /// <summary>
        /// Checks if the specified service exists.
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <returns>True if the service exists; otherwise, false.</returns>
        Task<bool> ServiceExistsAsync(string serviceName);

        /// <summary>
        /// Searches for services that match the specified criteria.
        /// </summary>
        /// <param name="searchTerm">The search term to match against service names and display names.</param>
        /// <param name="ignoreCase">Whether to ignore case when matching.</param>
        /// <returns>A list of matching services.</returns>
        Task<IEnumerable<ServiceInfo>> FindServicesAsync(string searchTerm, bool ignoreCase = true);

        /// <summary>
        /// Gets detailed information about the specified service.
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <returns>Detailed service information.</returns>
        Task<ServiceInfo> GetServiceInfoAsync(string serviceName);
    }

    /// <summary>
    /// Represents information about a Windows service.
    /// </summary>
    public class ServiceInfo
    {
        /// <summary>
        /// Gets or sets the service name.
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name of the service.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current status of the service.
        /// </summary>
        public ServiceControllerStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the service type.
        /// </summary>
        public ServiceType ServiceType { get; set; }

        /// <summary>
        /// Gets or sets the start type of the service.
        /// </summary>
        public ServiceStartMode StartType { get; set; }

        /// <summary>
        /// Gets or sets the description of the service.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the path to the service executable.
        /// </summary>
        public string? PathName { get; set; }

        /// <summary>
        /// Gets or sets the service account under which the service runs.
        /// </summary>
        public string? ServiceAccount { get; set; }
    }

    /// <summary>
    /// Represents the start mode of a Windows service.
    /// </summary>
    public enum ServiceStartMode
    {
        /// <summary>
        /// The service is started automatically by the system during startup.
        /// </summary>
        Automatic,

        /// <summary>
        /// The service is started manually by a user.
        /// </summary>
        Manual,

        /// <summary>
        /// The service is disabled and cannot be started.
        /// </summary>
        Disabled
    }
}