using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace PrivoxyManager
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            CheckAdminWarning();
            _ = RefreshServiceStatusAsync();
        }

        // -------------------------------------------------------
        // Helpers
        // -------------------------------------------------------

        private string ConfigPath => ConfigPathTextBox.Text.Trim();
        private string ServiceName => ServiceNameTextBox.Text.Trim();

        private bool IsAdministrator =>
            new WindowsPrincipal(WindowsIdentity.GetCurrent())
                .IsInRole(WindowsBuiltInRole.Administrator);

        private void CheckAdminWarning()
        {
            AdminWarningPanel.Visibility =
                IsAdministrator
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void SetStatus(string message, bool isError = false)
        {
            Dispatcher.Invoke(() =>
            {
                StatusTextBlock.Text = message;
                StatusTextBlock.Foreground = isError ? Brushes.DarkRed : Brushes.DarkGreen;
            });
        }

        private void SetServiceStatus(string text, Brush color)
        {
            Dispatcher.Invoke(() =>
            {
                ServiceStatusTextBlock.Text = text;
                ServiceStatusTextBlock.Foreground = color;
            });
        }

        private async Task<bool> EnsureConfigExistsAsync()
        {
            return await Task.Run(() =>
            {
                if (File.Exists(ConfigPath)) return true;

                Dispatcher.Invoke(() =>
                    MessageBox.Show(
                        this,
                        $"Config file does not exist:\n{ConfigPath}",
                        "Config not found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning));

                return false;
            });
        }

        // -------------------------------------------------------
        // Load config
        // -------------------------------------------------------

        private async void LoadConfig_Click(object sender, RoutedEventArgs e)
        {
            if (!await EnsureConfigExistsAsync()) return;

            try
            {
                string text = await File.ReadAllTextAsync(ConfigPath, Encoding.UTF8);
                ConfigEditorTextBox.Text = text;
                SetStatus("Config loaded.");
            }
            catch (Exception ex)
            {
                SetStatus($"Error loading config: {ex.Message}", true);
            }
        }

        // -------------------------------------------------------
        // Save config
        // -------------------------------------------------------

        private async void SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string text = ConfigEditorTextBox.Text;
                await File.WriteAllTextAsync(ConfigPath, text, Encoding.UTF8);
                SetStatus("Config saved.");
            }
            catch (Exception ex)
            {
                SetStatus($"Error saving config: {ex.Message}", true);
            }
        }

        private async void SaveAndRestart_Click(object sender, RoutedEventArgs e)
        {
            await SaveConfigInternalAsync();
            await RestartServiceInternalAsync();
        }

        private async Task SaveConfigInternalAsync()
        {
            try
            {
                string text = ConfigEditorTextBox.Text;
                await File.WriteAllTextAsync(ConfigPath, text, Encoding.UTF8);
                SetStatus("Config saved.");
            }
            catch (Exception ex)
            {
                SetStatus($"Error saving config: {ex.Message}", true);
            }
        }

        // -------------------------------------------------------
        // Service control
        // -------------------------------------------------------

        private async Task RefreshServiceStatusAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    using var sc = new ServiceController(ServiceName);
                    var status = sc.Status;

                    SetServiceStatus($"Status: {status}",
                        status == ServiceControllerStatus.Running
                            ? Brushes.DarkGreen
                            : Brushes.DarkOrange);
                }
                catch (Exception ex)
                {
                    SetServiceStatus($"Status: not found ({ex.Message})", Brushes.DarkRed);
                }
            });
        }

        private async Task StartServiceInternalAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    using var sc = new ServiceController(ServiceName);

                    if (sc.Status == ServiceControllerStatus.Running)
                    {
                        SetStatus("Service already running.");
                        return;
                    }

                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(20));
                    SetStatus("Service started.");
                }
                catch (Exception ex)
                {
                    SetStatus($"Error starting service: {ex.Message}", true);
                }
            });

            await RefreshServiceStatusAsync();
        }

        private async Task StopServiceInternalAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    using var sc = new ServiceController(ServiceName);

                    if (sc.Status == ServiceControllerStatus.Stopped)
                    {
                        SetStatus("Service already stopped.");
                        return;
                    }

                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(20));
                    SetStatus("Service stopped.");
                }
                catch (Exception ex)
                {
                    SetStatus($"Error stopping service: {ex.Message}", true);
                }
            });

            await RefreshServiceStatusAsync();
        }

        private async Task RestartServiceInternalAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    using var sc = new ServiceController(ServiceName);

                    if (sc.Status != ServiceControllerStatus.Stopped)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(20));
                    }

                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(20));

                    SetStatus("Service restarted.");
                }
                catch (Exception ex)
                {
                    SetStatus($"Error restarting service: {ex.Message}", true);
                }
            });

            await RefreshServiceStatusAsync();
        }

        private void StartService_Click(object sender, RoutedEventArgs e)
        {
            _ = StartServiceInternalAsync();
        }

        private void StopService_Click(object sender, RoutedEventArgs e)
        {
            _ = StopServiceInternalAsync();
        }

        private void RestartService_Click(object sender, RoutedEventArgs e)
        {
            _ = RestartServiceInternalAsync();
        }

        // -------------------------------------------------------
        // Run as Administrator
        // -------------------------------------------------------

        private void RunAsAdmin_Click(object sender, RoutedEventArgs e)
        {
            if (IsAdministrator)
            {
                MessageBox.Show("Already running as administrator.");
                return;
            }

            string exePath = Process.GetCurrentProcess().MainModule!.FileName!;

            var psi = new ProcessStartInfo(exePath)
            {
                Verb = "runas",
                UseShellExecute = true
            };

            try
            {
                Process.Start(psi);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Run as admin cancelled or failed:\n{ex.Message}");
            }
        }

        // -------------------------------------------------------
        // Detect service
        // -------------------------------------------------------

        private void DetectService_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var found = ServiceController
                    .GetServices()
                    .FirstOrDefault(s =>
                        s.ServiceName.Contains("privoxy", StringComparison.OrdinalIgnoreCase) ||
                        s.DisplayName.Contains("privoxy", StringComparison.OrdinalIgnoreCase));

                if (found == null)
                {
                    MessageBox.Show(
                        "Privoxy service not found.",
                        "Detection",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                ServiceNameTextBox.Text = found.ServiceName;
                _ = RefreshServiceStatusAsync();
                SetStatus($"Detected service: {found.ServiceName}");
            }
            catch (Exception ex)
            {
                SetStatus($"Error detecting service: {ex.Message}", true);
            }
        }

        // -------------------------------------------------------
        // Browse file
        // -------------------------------------------------------

        private void BrowseConfig_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Privoxy config (config.txt)|config.txt|All files (*.*)|*.*",
                FileName = ConfigPath
            };

            if (ofd.ShowDialog(this) == true)
                ConfigPathTextBox.Text = ofd.FileName;
        }
    }
}
