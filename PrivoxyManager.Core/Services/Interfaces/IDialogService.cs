using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace PrivoxyManager.Services.Interfaces
{
    /// <summary>
    /// Provides functionality for displaying dialogs and interacting with the user.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Shows a message box with the specified message and title.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="title">The title of the message box.</param>
        /// <param name="button">The button to display.</param>
        /// <param name="icon">The icon to display.</param>
        /// <returns>The result of the message box.</returns>
        Task<MessageBoxResult> ShowMessageBoxAsync(
            string message,
            string title = "",
            MessageBoxButton button = MessageBoxButton.OK,
            MessageBoxImage icon = MessageBoxImage.Information);

        /// <summary>
        /// Shows an error message box.
        /// </summary>
        /// <param name="message">The error message to display.</param>
        /// <param name="title">The title of the error message box.</param>
        /// <returns>A task representing the show operation.</returns>
        Task ShowErrorAsync(string message, string title = "Error");

        /// <summary>
        /// Shows a warning message box.
        /// </summary>
        /// <param name="message">The warning message to display.</param>
        /// <param name="title">The title of the warning message box.</param>
        /// <returns>A task representing the show operation.</returns>
        Task ShowWarningAsync(string message, string title = "Warning");

        /// <summary>
        /// Shows an information message box.
        /// </summary>
        /// <param name="message">The information message to display.</param>
        /// <param name="title">The title of the information message box.</param>
        /// <returns>A task representing the show operation.</returns>
        Task ShowInformationAsync(string message, string title = "Information");

        /// <summary>
        /// Shows a confirmation dialog.
        /// </summary>
        /// <param name="message">The confirmation message to display.</param>
        /// <param name="title">The title of the confirmation dialog.</param>
        /// <returns>True if the user confirmed; otherwise, false.</returns>
        Task<bool> ShowConfirmationAsync(string message, string title = "Confirm");

        /// <summary>
        /// Shows an open file dialog.
        /// </summary>
        /// <param name="title">The title of the dialog.</param>
        /// <param name="filter">The file filter.</param>
        /// <param name="initialDirectory">The initial directory.</param>
        /// <param name="fileName">The initial file name.</param>
        /// <returns>The selected file path, or null if cancelled.</returns>
        Task<string?> ShowOpenFileDialogAsync(
            string title = "Open File",
            string filter = "All files (*.*)|*.*",
            string? initialDirectory = null,
            string? fileName = null);

        /// <summary>
        /// Shows a save file dialog.
        /// </summary>
        /// <param name="title">The title of the dialog.</param>
        /// <param name="filter">The file filter.</param>
        /// <param name="initialDirectory">The initial directory.</param>
        /// <param name="fileName">The initial file name.</param>
        /// <returns>The selected file path, or null if cancelled.</returns>
        Task<string?> ShowSaveFileDialogAsync(
            string title = "Save File",
            string filter = "All files (*.*)|*.*",
            string? initialDirectory = null,
            string? fileName = null);

        /// <summary>
        /// Shows a folder browser dialog.
        /// </summary>
        /// <param name="description">The description of the dialog.</param>
        /// <param name="selectedPath">The initially selected path.</param>
        /// <returns>The selected folder path, or null if cancelled.</returns>
        Task<string?> ShowFolderBrowserDialogAsync(
            string description = "Select Folder",
            string? selectedPath = null);

        /// <summary>
        /// Shows an input dialog to get text input from the user.
        /// </summary>
        /// <param name="prompt">The prompt to display.</param>
        /// <param name="title">The title of the dialog.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>The user input, or null if cancelled.</returns>
        Task<string?> ShowInputDialogAsync(
            string prompt,
            string title = "Input",
            string? defaultValue = null);

        /// <summary>
        /// Shows a selection dialog to choose from a list of options.
        /// </summary>
        /// <param name="items">The list of items to choose from.</param>
        /// <param name="title">The title of the dialog.</param>
        /// <param name="prompt">The prompt to display.</param>
        /// <param name="selectedIndex">The initially selected index.</param>
        /// <returns>The selected index, or -1 if cancelled.</returns>
        Task<int> ShowSelectionDialogAsync(
            IEnumerable<string> items,
            string title = "Select",
            string prompt = "Please select an item:",
            int selectedIndex = 0);

        /// <summary>
        /// Shows a progress dialog with the specified operation.
        /// </summary>
        /// <param name="operation">The operation to perform.</param>
        /// <param name="title">The title of the progress dialog.</param>
        /// <param name="canCancel">Whether the operation can be cancelled.</param>
        /// <returns>A task representing the operation.</returns>
        Task ShowProgressDialogAsync(
            Func<IProgressContext, Task> operation,
            string title = "Processing...",
            bool canCancel = true);
    }

    /// <summary>
    /// Provides context for reporting progress during long-running operations.
    /// </summary>
    public interface IProgressContext
    {
        /// <summary>
        /// Gets or sets the current progress value (0-100).
        /// </summary>
        int Progress { get; set; }

        /// <summary>
        /// Gets or sets the current status message.
        /// </summary>
        string Status { get; set; }

        /// <summary>
        /// Gets a value indicating whether the operation has been cancelled.
        /// </summary>
        bool IsCancelled { get; }

        /// <summary>
        /// Reports progress with the specified value and optional message.
        /// </summary>
        /// <param name="progress">The progress value (0-100).</param>
        /// <param name="message">Optional status message.</param>
        void Report(int progress, string? message = null);

        /// <summary>
        /// Reports a status message without changing the progress value.
        /// </summary>
        /// <param name="message">The status message.</param>
        void ReportStatus(string message);
    }
}