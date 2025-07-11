using System;
using System.ComponentModel;


namespace Wpf_project_log.ViewModels
{
    // notify the view that a property has changed and update automatically
    public class BaseViewModel : INotifyPropertyChanged
    {
        // Here tells the WPF to refresh the page
        public event PropertyChangedEventHandler PropertyChanged;

        // compares the old and new values and updates
        protected bool SetProperty<T>(ref T oldValue, T value, string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(oldValue, value)) // compare with all type
                return false;

            oldValue = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
