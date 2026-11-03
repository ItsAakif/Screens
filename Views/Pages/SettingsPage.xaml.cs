using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Screens.WinUI.ViewModels;

namespace Screens.WinUI.Views.Pages
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsViewModel? ViewModel { get; private set; }

        public SettingsPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is SettingsViewModel viewModel)
            {
                ViewModel   = viewModel;
                DataContext  = ViewModel;
                await ViewModel.InitializeAsync();
            }
        }
    }
}
