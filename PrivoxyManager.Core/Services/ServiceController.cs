using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using PrivoxyManager.Services.Interfaces;

namespace PrivoxyManager.Services
{
    /// <summary>
    /// Provides functionality for managing Windows services.
    /// </summary>
    public class ServiceController : IServiceController
    {
        private readonly ILogger<ServiceController> _logger;

        /// <summary>
        /// Initializes a new instance of the ServiceController class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public ServiceController(ILogger<ServiceController> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<ServiceControllerStatus> GetServiceStatusAsync(string serviceName)
        {
            try
            {
                _logger.LogInformation("Getting status for service: {ServiceName}", serviceName);

                using var sc = new System.ServiceProcess.ServiceController(serviceName);
                var status = sc.Status;

                _logger.LogInformation("Service {ServiceName} status: {Status}", serviceName, status);
                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting status for service: {ServiceName}", serviceName);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task StartServiceAsync(string serviceName, TimeSpan? timeout = null)
        {
            try
            {
                _logger.LogInformation("Starting service: {ServiceName}", serviceName);

                using var sc = new System.ServiceProcess.ServiceController(serviceName);
                
                if (sc.Status == ServiceControllerStatus.Running)
                {
                    _logger.LogInformation("Service {ServiceName} is already running", serviceName);
                    return;
                }

                sc.Start();
                
                var waitTimeout = timeout ?? TimeSpan.FromSeconds(20);
                sc.WaitForStatus(ServiceControllerStatus.Running, waitTimeout);

                _logger.LogInformation("Service {ServiceName} started successfully", serviceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting service: {ServiceName}", serviceName);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task StopServiceAsync(string serviceName, TimeSpan? timeout = null)
        {
            try
            {
                _logger.LogInformation("Stopping service: {ServiceName}", serviceName);

                using var sc = new System.ServiceProcess.ServiceController(serviceName);
                
                if (sc.Status == ServiceControllerStatus.Stopped)
                {
                    _logger.LogInformation("Service {ServiceName} is already stopped", serviceName);
                    return;
                }

                sc.Stop();
                
                var waitTimeout = timeout ?? TimeSpan.FromSeconds(20);
                sc.WaitForStatus(ServiceControllerStatus.Stopped, waitTimeout);

                _logger.LogInformation("Service {ServiceName} stopped successfully", serviceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping service: {ServiceName}", serviceName);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task RestartServiceAsync(string serviceName, TimeSpan? timeout = null)
        {
            try
            {
                _logger.LogInformation("Restarting service: {ServiceName}", serviceName);

                using var sc = new System.ServiceProcess.ServiceController(serviceName);
                
                var waitTimeout = timeout ?? TimeSpan.FromSeconds(20);

                if (sc.Status != ServiceControllerStatus.Stopped)
                {
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, waitTimeout);
                }

                sc.Start();
                sc.WaitForStatus(ServiceControllerStatus.Running, waitTimeout);

                _logger.LogInformation("Service {ServiceName} restarted successfully", serviceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restarting service: {ServiceName}", serviceName);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> ServiceExistsAsync(string serviceName)
        {
            try
            {
                _logger.LogInformation("Checking if service exists: {ServiceName}", serviceName);

                var services = System.ServiceProcess.ServiceController.GetServices();
                var exists = services.Any(s => s.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));

                _logger.LogInformation("Service {ServiceName} exists: {Exists}", serviceName, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if service exists: {ServiceName}", serviceName);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ServiceInfo>> FindServicesAsync(string searchTerm, bool ignoreCase = true)
        {
            try
            {
                _logger.LogInformation("Searching for services with term: {SearchTerm}", searchTerm);

                var services = System.ServiceProcess.ServiceController.GetServices();
                var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

                var matchingServices = services
                    .Where(s => s.ServiceName.Contains(searchTerm, comparison) ||
                                s.DisplayName.Contains(searchTerm, comparison))
                    .Select(s => CreateServiceInfo(s))
                    .ToList();

                _logger.LogInformation("Found {Count} matching services", matchingServices.Count);
                return matchingServices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for services with term: {SearchTerm}", searchTerm);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<ServiceInfo> GetServiceInfoAsync(string serviceName)
        {
            try
            {
                _logger.LogInformation("Getting detailed info for service: {ServiceName}", serviceName);

                var services = System.ServiceProcess.ServiceController.GetServices();
                var service = services.FirstOrDefault(s => s.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));

                if (service == null)
                {
                    throw new ArgumentException($"Service '{serviceName}' not found", nameof(serviceName));
                }

                var serviceInfo = CreateServiceInfo(service);
                
                // Try to get additional information from the registry
                try
                {
                    var registryInfo = GetServiceInfoFromRegistry(serviceName);
                    serviceInfo.Description = registryInfo.Description;
                    serviceInfo.PathName = registryInfo.PathName;
                    serviceInfo.ServiceAccount = registryInfo.ServiceAccount;
                    serviceInfo.StartType = registryInfo.StartType;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not get additional service info from registry for: {ServiceName}", serviceName);
                }

                _logger.LogInformation("Retrieved info for service: {ServiceName}", serviceName);
                return serviceInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting info for service: {ServiceName}", serviceName);
                throw;
            }
        }

        private static ServiceInfo CreateServiceInfo(System.ServiceProcess.ServiceController service)
        {
            return new ServiceInfo
            {
                ServiceName = service.ServiceName,
                DisplayName = service.DisplayName,
                Status = service.Status,
                ServiceType = service.ServiceType
            };
        }

        private static ServiceInfo GetServiceInfoFromRegistry(string serviceName)
        {
            var serviceInfo = new ServiceInfo();
            
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    $@"SYSTEM\CurrentControlSet\Services\{serviceName}");
                
                if (key != null)
                {
                    serviceInfo.Description = key.GetValue("Description")?.ToString();
                    serviceInfo.PathName = key.GetValue("ImagePath")?.ToString();
                    serviceInfo.ServiceAccount = key.GetValue("ObjectName")?.ToString();

                    var startType = key.GetValue("Start")?.ToString();
                    serviceInfo.StartType = startType switch
                    {
                        "2" => PrivoxyManager.Services.Interfaces.ServiceStartMode.Automatic,
                        "3" => PrivoxyManager.Services.Interfaces.ServiceStartMode.Manual,
                        "4" => PrivoxyManager.Services.Interfaces.ServiceStartMode.Disabled,
                        _ => PrivoxyManager.Services.Interfaces.ServiceStartMode.Manual
                    };
                }
            }
            catch (Exception ex)
            {
                // Log the exception but don't throw - we can still return basic info
                System.Diagnostics.Debug.WriteLine($"Error reading from registry: {ex.Message}");
            }

            return serviceInfo;
        }
    }
}