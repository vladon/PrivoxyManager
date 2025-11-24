using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PrivoxyManager
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            CheckAdminWarning();
            RefreshServiceStatus();
        }

        private readonly SolidColorBrush ColorError = new SolidColorBrush(Color.FromRgb(220, 60, 60));
        private readonly SolidColorBrush ColorWarning = new SolidColorBrush(Color.FromRgb(255, 200, 0));
        private readonly SolidColorBrush ColorInfo = new SolidColorBrush(Color.FromRgb(80, 180, 255));
        private readonly SolidColorBrush ColorDebug = new SolidColorBrush(Color.FromRgb(160, 160, 160));
        private readonly SolidColorBrush ColorSuccess = new SolidColorBrush(Color.FromRgb(0, 200, 120));
        private readonly SolidColorBrush ColorDefault = new SolidColorBrush(Color.FromRgb(240, 240, 240));


        // -------------------------------------------------------
        // Helpers
        // -------------------------------------------------------

        private string ConfigPath => ConfigPathTextBox.Text.Trim();
        private string ServiceName => ServiceNameTextBox.Text.Trim();

        private FileStream? _logStream;
        private StreamReader? _logReader;
        private System.Windows.Threading.DispatcherTimer? _logTimer;
        private readonly List<TextBlock> _allLogEntries = new List<TextBlock>();

        private string LogPath => Path.Combine(
            Path.GetDirectoryName(ConfigPath)!,
            "privoxy.log"
        );


        private bool IsAdministrator =>
            new WindowsPrincipal(WindowsIdentity.GetCurrent())
                .IsInRole(WindowsBuiltInRole.Administrator);

        private void CheckAdminWarning()
        {
            AdminWarningPanel.Visibility =
                IsAdministrator ? Visibility.Collapsed : Visibility.Visible;
        }

        private void SetStatus(string message, bool isError = false)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = isError ? Brushes.DarkRed : Brushes.DarkGreen;
        }

        private void SetServiceStatus(string message, Brush color)
        {
            ServiceStatusTextBlock.Text = message;
            ServiceStatusTextBlock.Foreground = color;
        }

        private bool EnsureConfigExists()
        {
            if (File.Exists(ConfigPath))
                return true;

            MessageBox.Show(
                this,
                $"Config file does not exist:\n{ConfigPath}",
                "Config not found",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return false;
        }

        // -------------------------------------------------------
        // Config load / save
        // -------------------------------------------------------

        private void LoadConfig_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureConfigExists())
                return;

            try
            {
                string text = File.ReadAllText(ConfigPath, Encoding.UTF8);
                ConfigEditorTextBox.Text = text;
                SetStatus("Config loaded.");
            }
            catch (Exception ex)
            {
                SetStatus($"Error loading config: {ex.Message}", true);
            }
        }

        private void SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            SaveConfigInternal();
        }

        private void SaveConfigInternal()
        {
            try
            {
                string text = ConfigEditorTextBox.Text;
                File.WriteAllText(ConfigPath, text, Encoding.UTF8);
                SetStatus("Config saved.");
            }
            catch (Exception ex)
            {
                SetStatus($"Error saving config: {ex.Message}", true);
            }
        }

        private void SaveAndRestart_Click(object sender, RoutedEventArgs e)
        {
            SaveConfigInternal();
            RestartServiceInternal();
        }

        // -------------------------------------------------------
        // Service control
        // -------------------------------------------------------

        private void RefreshServiceStatus()
        {
            try
            {
                using var sc = new ServiceController(ServiceName);
                var status = sc.Status;

                SetServiceStatus(
                    $"Status: {status}",
                    status == ServiceControllerStatus.Running
                        ? Brushes.DarkGreen
                        : Brushes.DarkOrange);
            }
            catch (Exception ex)
            {
                SetServiceStatus($"Status: not found ({ex.Message})", Brushes.DarkRed);
            }
        }

        private void StartServiceInternal()
        {
            try
            {
                using var sc = new ServiceController(ServiceName);

                if (sc.Status == ServiceControllerStatus.Running)
                {
                    SetStatus("Service already running.");
                    RefreshServiceStatus();
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

            RefreshServiceStatus();
        }

        private void StopServiceInternal()
        {
            try
            {
                using var sc = new ServiceController(ServiceName);

                if (sc.Status == ServiceControllerStatus.Stopped)
                {
                    SetStatus("Service already stopped.");
                    RefreshServiceStatus();
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

            RefreshServiceStatus();
        }

        private void RestartServiceInternal()
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

            RefreshServiceStatus();
        }

        private void StartService_Click(object sender, RoutedEventArgs e)
        {
            StartServiceInternal();
        }

        private void StopService_Click(object sender, RoutedEventArgs e)
        {
            StopServiceInternal();
        }

        private void RestartService_Click(object sender, RoutedEventArgs e)
        {
            RestartServiceInternal();
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
                RefreshServiceStatus();
                SetStatus($"Detected service: {found.ServiceName}");
            }
            catch (Exception ex)
            {
                SetStatus($"Error detecting service: {ex.Message}", true);
            }
        }

        // -------------------------------------------------------
        // Browse config
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

        private void StartLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!File.Exists(LogPath))
                {
                    MessageBox.Show($"Log file not found:\n{LogPath}");
                    return;
                }

                // Close previous if exists
                StopLogInternal();

                _logStream = new FileStream(
                    LogPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite
                );

                _logReader = new StreamReader(_logStream);

                // Move to end of file
                _logStream.Seek(0, SeekOrigin.End);

                _logTimer = new System.Windows.Threading.DispatcherTimer();
                _logTimer.Interval = TimeSpan.FromMilliseconds(300);
                _logTimer.Tick += LogTimer_Tick;
                _logTimer.Start();

                AppendLogMessage($"--- Log started: {DateTime.Now} ---");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start log:\n{ex.Message}");
            }
        }

        private void LogTimer_Tick(object? sender, EventArgs e)
        {
            if (_logReader == null) return;

            while (!_logReader.EndOfStream)
            {
                string? line = _logReader.ReadLine();
                if (line != null)
                {
                    AppendColoredLog(line);
                }
            }
        }

        private void StopLog_Click(object sender, RoutedEventArgs e)
        {
            StopLogInternal();
            AppendLogMessage($"--- Log stopped: {DateTime.Now} ---");
        }

        private void StopLogInternal()
        {
            _logTimer?.Stop();
            _logTimer = null;

            _logReader?.Dispose();
            _logReader = null;

            _logStream?.Dispose();
            _logStream = null;
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            LogList.Items.Clear();
            _allLogEntries.Clear();
        }


        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            StopLogInternal();
            base.OnClosing(e);
        }

        private void AppendColoredLog(string line)
        {
            Brush color = ColorDefault;
            string u = line.ToUpperInvariant();

            if (u.Contains("ERROR") || u.Contains("FATAL"))
                color = ColorError;
            else if (u.Contains("WARNING") || u.Contains("WARN"))
                color = ColorWarning;
            else if (u.Contains("CONNECT") || u.Contains("REQUEST"))
                color = ColorSuccess;
            else if (u.Contains("CONFIG") || u.Contains("INITIALIZ") || u.Contains("START"))
                color = ColorInfo;
            else if (u.Contains("DEBUG"))
                color = ColorDebug;

            var tb = new TextBlock
            {
                Text = line,
                Foreground = color,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13
            };

            _allLogEntries.Add(tb);
            ApplyFilter();
        }

        private void AppendLogMessage(string message)
        {
            var tb = new TextBlock
            {
                Text = message,
                Foreground = ColorDefault,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13
            };

            _allLogEntries.Add(tb);
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            LogList.Items.Clear();
            string filterText = FilterTextBox.Text.Trim();

            foreach (var entry in _allLogEntries)
            {
                if (string.IsNullOrEmpty(filterText) ||
                    entry.Text.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    LogList.Items.Add(entry);
                }
            }

            if (LogList.Items.Count > 0)
            {
                LogList.ScrollIntoView(LogList.Items[LogList.Items.Count - 1]);
            }
        }

        private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ClearFilter_Click(object sender, RoutedEventArgs e)
        {
            FilterTextBox.Text = string.Empty;
            ApplyFilter();
        }


    }
}
