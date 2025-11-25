using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using PrivoxyManager.Services.Interfaces;

namespace PrivoxyManager.Services
{
    /// <summary>
    /// Provides functionality for displaying dialogs and interacting with the user.
    /// </summary>
    public class DialogService : IDialogService
    {
        /// <inheritdoc />
        public Task<MessageBoxResult> ShowMessageBoxAsync(
            string message,
            string title = "",
            MessageBoxButton button = MessageBoxButton.OK,
            MessageBoxImage icon = MessageBoxImage.Information)
        {
            return Task.FromResult(MessageBox.Show(message, title, button, icon));
        }

        /// <inheritdoc />
        public Task ShowErrorAsync(string message, string title = "Error")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task ShowWarningAsync(string message, string title = "Warning")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task ShowInformationAsync(string message, string title = "Information")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<bool> ShowConfirmationAsync(string message, string title = "Confirm")
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return Task.FromResult(result == MessageBoxResult.Yes);
        }

        /// <inheritdoc />
        public Task<string?> ShowOpenFileDialogAsync(
            string title = "Open File",
            string filter = "All files (*.*)|*.*",
            string? initialDirectory = null,
            string? fileName = null)
        {
            var dialog = new OpenFileDialog
            {
                Title = title,
                Filter = filter,
                InitialDirectory = initialDirectory,
                FileName = fileName ?? string.Empty
            };

            var result = dialog.ShowDialog();
            return Task.FromResult(result == true ? dialog.FileName : null);
        }

        /// <inheritdoc />
        public Task<string?> ShowSaveFileDialogAsync(
            string title = "Save File",
            string filter = "All files (*.*)|*.*",
            string? initialDirectory = null,
            string? fileName = null)
        {
            var dialog = new SaveFileDialog
            {
                Title = title,
                Filter = filter,
                InitialDirectory = initialDirectory,
                FileName = fileName ?? string.Empty
            };

            var result = dialog.ShowDialog();
            return Task.FromResult(result == true ? dialog.FileName : null);
        }

        /// <inheritdoc />
        public Task<string?> ShowFolderBrowserDialogAsync(
            string description = "Select Folder",
            string? selectedPath = null)
        {
            var window = new Window
            {
                Title = description,
                Width = 500,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize
            };

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(10)
            };

            var label = new Label
            {
                Content = "Select folder path:",
                Margin = new Thickness(0, 0, 0, 10)
            };

            var textBox = new TextBox
            {
                Text = selectedPath ?? string.Empty,
                Margin = new Thickness(0, 0, 0, 10),
                Width = 400
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var browseButton = new Button
            {
                Content = "Browse...",
                Width = 80,
                Margin = new Thickness(0, 0, 10, 0)
            };

            var okButton = new Button
            {
                Content = "OK",
                Width = 80,
                IsDefault = true
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                IsCancel = true
            };

            buttonPanel.Children.Add(browseButton);
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            stackPanel.Children.Add(label);
            stackPanel.Children.Add(textBox);
            stackPanel.Children.Add(buttonPanel);

            window.Content = stackPanel;

            var result = new TaskCompletionSource<string?>();

            browseButton.Click += (s, e) =>
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Select Folder",
                    Filter = "Folders|*.none",
                    CheckFileExists = false,
                    CheckPathExists = true,
                    FileName = "Select Folder"
                };

                if (dialog.ShowDialog() == true)
                {
                    var folderPath = System.IO.Path.GetDirectoryName(dialog.FileName);
                    if (!string.IsNullOrEmpty(folderPath))
                    {
                        textBox.Text = folderPath;
                    }
                }
            };

            okButton.Click += (s, e) =>
            {
                result.SetResult(textBox.Text);
                window.Close();
            };

            cancelButton.Click += (s, e) =>
            {
                result.SetResult(null);
                window.Close();
            };

            window.Show();
            return result.Task;
        }

        /// <inheritdoc />
        public Task<string?> ShowInputDialogAsync(
            string prompt,
            string title = "Input",
            string? defaultValue = null)
        {
            var window = new Window
            {
                Title = title,
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize
            };

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(10)
            };

            var label = new Label
            {
                Content = prompt,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var textBox = new TextBox
            {
                Text = defaultValue ?? string.Empty,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var okButton = new Button
            {
                Content = "OK",
                Width = 80,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                IsCancel = true
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            stackPanel.Children.Add(label);
            stackPanel.Children.Add(textBox);
            stackPanel.Children.Add(buttonPanel);

            window.Content = stackPanel;

            var result = new TaskCompletionSource<string?>();

            okButton.Click += (s, e) =>
            {
                result.SetResult(textBox.Text);
                window.Close();
            };

            cancelButton.Click += (s, e) =>
            {
                result.SetResult(null);
                window.Close();
            };

            window.Show();
            return result.Task;
        }

        /// <inheritdoc />
        public Task<int> ShowSelectionDialogAsync(
            IEnumerable<string> items,
            string title = "Select",
            string prompt = "Please select an item:",
            int selectedIndex = 0)
        {
            var window = new Window
            {
                Title = title,
                Width = 400,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize
            };

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(10)
            };

            var label = new Label
            {
                Content = prompt,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var listBox = new ListBox
            {
                Margin = new Thickness(0, 0, 0, 10),
                Height = 150
            };

            foreach (var item in items)
            {
                listBox.Items.Add(item);
            }

            if (selectedIndex >= 0 && selectedIndex < listBox.Items.Count)
            {
                listBox.SelectedIndex = selectedIndex;
            }

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var okButton = new Button
            {
                Content = "OK",
                Width = 80,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                IsCancel = true
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            stackPanel.Children.Add(label);
            stackPanel.Children.Add(listBox);
            stackPanel.Children.Add(buttonPanel);

            window.Content = stackPanel;

            var result = new TaskCompletionSource<int>();

            okButton.Click += (s, e) =>
            {
                result.SetResult(listBox.SelectedIndex);
                window.Close();
            };

            cancelButton.Click += (s, e) =>
            {
                result.SetResult(-1);
                window.Close();
            };

            window.Show();
            return result.Task;
        }

        /// <inheritdoc />
        public Task ShowProgressDialogAsync(
            Func<IProgressContext, Task> operation,
            string title = "Processing...",
            bool canCancel = true)
        {
            var window = new Window
            {
                Title = title,
                Width = 400,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false
            };

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(20)
            };

            var progressBar = new ProgressBar
            {
                Height = 20,
                Margin = new Thickness(0, 0, 0, 10),
                IsIndeterminate = true
            };

            var statusLabel = new Label
            {
                Content = "Processing...",
                Margin = new Thickness(0, 0, 0, 10)
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                HorizontalAlignment = HorizontalAlignment.Right,
                Visibility = canCancel ? Visibility.Visible : Visibility.Collapsed
            };

            stackPanel.Children.Add(progressBar);
            stackPanel.Children.Add(statusLabel);
            stackPanel.Children.Add(cancelButton);

            window.Content = stackPanel;

            var progressContext = new ProgressContext(statusLabel, progressBar);
            var result = new TaskCompletionSource<bool>();
            var isCancelled = false;

            cancelButton.Click += (s, e) =>
            {
                isCancelled = true;
                progressContext.IsCancelled = true;
                result.SetResult(false);
                window.Close();
            };

            // Run the operation
            Task.Run(async () =>
            {
                try
                {
                    await operation(progressContext);
                    
                    if (!isCancelled)
                    {
                        Dispatcher.CurrentDispatcher.Invoke(() =>
                        {
                            result.SetResult(true);
                            window.Close();
                        });
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.CurrentDispatcher.Invoke(() =>
                    {
                        result.SetException(ex);
                        window.Close();
                    });
                }
            });

            window.ShowDialog();
            return result.Task;
        }
    }

    /// <summary>
    /// Implementation of IProgressContext for the DialogService.
    /// </summary>
    internal class ProgressContext : IProgressContext
    {
        private readonly Label _statusLabel;
        private readonly ProgressBar _progressBar;

        public ProgressContext(Label statusLabel, ProgressBar progressBar)
        {
            _statusLabel = statusLabel;
            _progressBar = progressBar;
        }

        public int Progress
        {
            get => (int)_progressBar.Value;
            set
            {
                _progressBar.IsIndeterminate = false;
                _progressBar.Value = value;
            }
        }

        public string Status
        {
            get => _statusLabel.Content?.ToString() ?? string.Empty;
            set => _statusLabel.Content = value;
        }

        public bool IsCancelled { get; set; }

        public void Report(int progress, string? message = null)
        {
            Progress = progress;
            if (!string.IsNullOrEmpty(message))
            {
                Status = message;
            }
        }

        public void ReportStatus(string message)
        {
            Status = message;
        }
    }
}