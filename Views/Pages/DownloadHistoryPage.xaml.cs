using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Screens.WinUI.ViewModels;

namespace Screens.WinUI.Views.Pages
{
    public sealed partial class DownloadHistoryPage : Page
    {
        public DownloadHistoryViewModel? ViewModel { get; private set; }

        public DownloadHistoryPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is DownloadHistoryViewModel viewModel)
            {
                ViewModel   = viewModel;
                DataContext  = ViewModel;
                await ViewModel.InitializeAsync();
            }
        }
    }
}
