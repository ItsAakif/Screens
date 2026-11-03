using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Screens.WinUI.Mvvm
{
    public abstract class ViewModelBase : ObservableObject
    {
        private bool _isBusy;
        private string? _errorMessage;

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        protected void SetBusy(bool busy)
        {
            IsBusy = busy;
        }

        protected void SetError(string? error)
        {
            ErrorMessage = error;
        }

        protected void ClearError()
        {
            ErrorMessage = null;
        }
    }
}
