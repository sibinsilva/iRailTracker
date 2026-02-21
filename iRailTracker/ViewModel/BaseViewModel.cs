using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace iRailTracker.ViewModel
{
    public class BaseViewModel : INotifyPropertyChanged
    {

        #region Events

        public event EventHandler<string>? ErrorOccurred;
        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion

        #region Methods
        protected bool SetProperty<T>(
            ref T backingStore,
            T value,
            [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected virtual void ShowError(string message)
        {
            ErrorOccurred?.Invoke(this, message);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}