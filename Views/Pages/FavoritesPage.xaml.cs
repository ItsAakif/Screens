using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Screens.WinUI.ViewModels;

namespace Screens.WinUI.Views.Pages
{
    public sealed partial class FavoritesPage : Page
    {
        public FavoritesViewModel? ViewModel { get; private set; }

        public FavoritesPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is FavoritesViewModel viewModel)
            {
                ViewModel = viewModel;
                DataContext = ViewModel;
                _ = ViewModel.InitializeAsync();
            }
        }
    }
}
