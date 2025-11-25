using System.Windows;

namespace PrivoxyManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the window closing event to save application state.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected override async void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // Get the MainWindowViewModel and save state
                if (DataContext is ViewModels.MainWindowViewModel viewModel)
                {
                    await viewModel.SaveStateAsync();
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving state: {ex.Message}");
            }

            base.OnClosing(e);
        }
    }
}