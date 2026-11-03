using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Screens.WinUI.ViewModels;
using System.Threading.Tasks;

namespace Screens.WinUI.Views.Pages
{
    public sealed partial class HomePage : Page
    {
        public HomeViewModel? ViewModel { get; private set; }

        public HomePage()
        {
            this.InitializeComponent();
            this.SizeChanged += OnSizeChanged;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (WallpapersGrid.ItemsPanelRoot is ItemsWrapGrid panel)
            {
                panel.MaximumRowsOrColumns = e.NewSize.Width switch
                {
                    < 800  => 2,
                    < 1200 => 3,
                    < 1600 => 4,
                    < 2000 => 5,
                    _      => 6
                };
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is HomeViewModel viewModel)
            {
                ViewModel   = viewModel;
                DataContext  = ViewModel;

                ViewModel.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(ViewModel.IsLoading))
                    {
                        LoadingRing.Visibility = ViewModel.IsLoading
                            ? Visibility.Visible
                            : Visibility.Collapsed;
                    }
                };

                _ = InitializeAsync();
            }
        }

        private async Task InitializeAsync()
        {
            if (ViewModel == null) return;

            await ViewModel.InitializeAsync();
            LoadingRing.Visibility = Visibility.Collapsed;
        }
    }
}
