using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Diagnostics;
using System.Security.Principal;


namespace PrivoxyManager
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            _ = RefreshServiceStatusAsync();
        }

        // --- Вспомогательные методы ---

        private string ConfigPath => ConfigPathTextBox.Text.Trim();
        private string ServiceName => ServiceNameTextBox.Text.Trim();

        private void SetStatus(string message, bool isError = false)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = isError ? System.Windows.Media.Brushes.DarkRed
                                                 : System.Windows.Media.Brushes.DarkGreen;
        }

        private async Task RefreshServiceStatusAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    using var sc = new ServiceController(ServiceName);
                    var status = sc.Status;

                    Dispatcher.Invoke(() =>
                    {
                        ServiceStatusTextBlock.Text = $"Status: {status}";
                        ServiceStatusTextBlock.Foreground =
                            status == ServiceControllerStatus.Running
                                ? System.Windows.Media.Brushes.DarkGreen
                                : System.Windows.Media.Brushes.DarkOrange;
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        ServiceStatusTextBlock.Text = $"Status: not found ({ex.Message})";
                        ServiceStatusTextBlock.Foreground = System.Windows.Media.Brushes.DarkRed;
                    });
                }
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

        // --- Обработчики кнопок ---

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
            await Dispatcher.InvokeAsync(() => SaveConfig_Click(sender, e),
                DispatcherPriority.Background);

            await RestartServiceInternalAsync();
        }

        private async void StartService_Click(object sender, RoutedEventArgs e)
        {
            await StartServiceInternalAsync();
        }

        private async void StopService_Click(object sender, RoutedEventArgs e)
        {
            await StopServiceInternalAsync();
        }

        private async void RestartService_Click(object sender, RoutedEventArgs e)
        {
            await RestartServiceInternalAsync();
        }

        // --- Работа со службой ---

        private async Task StartServiceInternalAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    using var sc = new ServiceController(ServiceName);
                    if (sc.Status == ServiceControllerStatus.Running)
                    {
                        Dispatcher.Invoke(() => SetStatus("Service already running."));
                        return;
                    }

                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(20));
                    Dispatcher.Invoke(() => SetStatus("Service started."));
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => SetStatus($"Error starting service: {ex.Message}", true));
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
                        Dispatcher.Invoke(() => SetStatus("Service already stopped."));
                        return;
                    }

                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(20));
                    Dispatcher.Invoke(() => SetStatus("Service stopped."));
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => SetStatus($"Error stopping service: {ex.Message}", true));
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

                    Dispatcher.Invoke(() => SetStatus("Service restarted."));
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => SetStatus($"Error restarting service: {ex.Message}", true));
                }
            });

            await RefreshServiceStatusAsync();
        }

        // --- Поиск службы автоматически ---

        private void DetectService_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var privoxyService = ServiceController
                    .GetServices()
                    .FirstOrDefault(s =>
                        s.ServiceName.Contains("privoxy", StringComparison.OrdinalIgnoreCase) ||
                        s.DisplayName.Contains("privoxy", StringComparison.OrdinalIgnoreCase));

                if (privoxyService == null)
                {
                    MessageBox.Show(this,
                        "Privoxy service not found. Check that it is installed.",
                        "Detection",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                ServiceNameTextBox.Text = privoxyService.ServiceName;
                _ = RefreshServiceStatusAsync();
                SetStatus($"Detected service: {privoxyService.ServiceName}");
            }
            catch (Exception ex)
            {
                SetStatus($"Error detecting service: {ex.Message}", true);
            }
        }

        private bool IsAdministrator =>
            new WindowsPrincipal(WindowsIdentity.GetCurrent())
            .IsInRole(WindowsBuiltInRole.Administrator);

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
                Verb = "runas",       // triggers UAC
                UseShellExecute = true
            };

            try
            {
                Process.Start(psi);
                Application.Current.Shutdown(); // close the non-admin instance
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Run as admin cancelled or failed:\n{ex.Message}");
            }
        }
    }
}
