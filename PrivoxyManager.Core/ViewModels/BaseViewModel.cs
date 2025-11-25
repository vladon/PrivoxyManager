using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PrivoxyManager.ViewModels
{
    /// <summary>
    /// Base class for all ViewModels that implements INotifyPropertyChanged.
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged, IDisposable
    {
        private bool _isBusy;
        private string? _busyMessage;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets or sets a value indicating whether the ViewModel is currently busy.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        /// <summary>
        /// Gets or sets the busy message to display when IsBusy is true.
        /// </summary>
        public string? BusyMessage
        {
            get => _busyMessage;
            set => SetProperty(ref _busyMessage, value);
        }

        /// <summary>
        /// Sets the busy state with an optional message.
        /// </summary>
        /// <param name="isBusy">Whether the ViewModel should be marked as busy.</param>
        /// <param name="message">Optional message to display while busy.</param>
        protected void SetBusy(bool isBusy, string? message = null)
        {
            IsBusy = isBusy;
            BusyMessage = message;
        }

        /// <summary>
        /// Executes an action while setting the busy state.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="message">Optional message to display while busy.</param>
        protected async Task ExecuteBusyAsync(Func<Task> action, string? message = null)
        {
            try
            {
                SetBusy(true, message);
                await action();
            }
            finally
            {
                SetBusy(false);
            }
        }

        /// <summary>
        /// Executes an action while setting the busy state.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="message">Optional message to display while busy.</param>
        protected async Task<T> ExecuteBusyAsync<T>(Func<Task<T>> action, string? message = null)
        {
            try
            {
                SetBusy(true, message);
                return await action();
            }
            finally
            {
                SetBusy(false);
            }
        }

        /// <summary>
        /// Sets the property value and raises the PropertyChanged event if the value has changed.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="field">The field storing the property value.</param>
        /// <param name="value">The new value.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>True if the property value changed; otherwise, false.</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Raises the PropertyChanged event for the specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raises the PropertyChanged event for multiple properties.
        /// </summary>
        /// <param name="propertyNames">The names of the properties that changed.</param>
        protected void OnPropertyChanged(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                OnPropertyChanged(propertyName);
            }
        }

        /// <summary>
        /// Called when the ViewModel is being initialized.
        /// </summary>
        public virtual async Task InitializeAsync()
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Called when the ViewModel is being disposed.
        /// </summary>
        protected virtual void DisposeCore()
        {
        }

        #region IDisposable Implementation

        private bool _disposed;

        /// <summary>
        /// Disposes the ViewModel and releases all resources.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                DisposeCore();
                _disposed = true;
            }
        }

        #endregion
    }
}