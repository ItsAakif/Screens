using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Screens.WinUI.ViewModels;

namespace Screens.WinUI.Views.Pages
{
    public sealed partial class GeneratePage : Page
    {
        public GenerateViewModel? ViewModel { get; private set; }

        public GeneratePage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is GenerateViewModel viewModel)
            {
                ViewModel = viewModel;
                DataContext = ViewModel;
                _ = ViewModel.InitializeAsync();
            }
        }
    }
}
