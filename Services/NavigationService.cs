using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace Screens.WinUI.Services
{
    public class NavigationService : INavigationService
    {
        private Frame? _frame;

        public void Initialize(Frame frame)
        {
            _frame = frame;
        }

        public void Navigate(Type pageType)
        {
            _frame?.Navigate(pageType);
        }

        public void Navigate(Type pageType, object parameter)
        {
            _frame?.Navigate(pageType, parameter);
        }

        public void GoBack()
        {
            if (CanGoBack)
            {
                _frame?.GoBack();
            }
        }

        public bool CanGoBack => _frame?.CanGoBack ?? false;
    }
}
