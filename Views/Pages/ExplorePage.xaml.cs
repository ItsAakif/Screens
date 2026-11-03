using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Screens.WinUI.ViewModels;
using System;

namespace Screens.WinUI.Views.Pages
{
    public sealed partial class ExplorePage : Page
    {
        public ExploreViewModel? ViewModel { get; private set; }

        public ExplorePage()
        {
            this.InitializeComponent();
            this.SizeChanged += OnSizeChanged;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (WallpapersGrid.ItemsPanelRoot is ItemsWrapGrid panel)
            {
                // Adjust columns based on window width
                double windowWidth = e.NewSize.Width;
                int maxColumns = windowWidth switch
                {
                    < 800 => 2,
                    < 1200 => 3,
                    < 1600 => 4,
                    < 2000 => 5,
                    _ => 6
                };
                panel.MaximumRowsOrColumns = maxColumns;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is ExploreViewModel viewModel)
            {
                ViewModel = viewModel;
                DataContext = ViewModel;

                // Subscribe to property changes
                ViewModel.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(ViewModel.IsLoading))
                    {
                        LoadingRing.Visibility = ViewModel.IsLoading ?
                            Microsoft.UI.Xaml.Visibility.Visible :
                            Microsoft.UI.Xaml.Visibility.Collapsed;
                    }

                    if (args.PropertyName == nameof(ViewModel.Wallpapers))
                    {
                        WallpapersGrid.ItemsSource = ViewModel.Wallpapers;
                    }

                    if (args.PropertyName == nameof(ViewModel.Categories))
                    {
                        CategoryComboBox.ItemsSource = ViewModel.Categories;
                        CategoryComboBox.SelectedItem = ViewModel.SelectedCategory;
                    }
                };

                // Initialize
                _ = InitializeAsync();
            }
        }

        private async System.Threading.Tasks.Task InitializeAsync()
        {
            if (ViewModel != null)
            {
                await ViewModel.InitializeAsync();

                // Set the data
                CategoryComboBox.ItemsSource = ViewModel.Categories;
                CategoryComboBox.SelectedItem = ViewModel.SelectedCategory;
                WallpapersGrid.ItemsSource = ViewModel.Wallpapers;
                LoadingRing.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.SearchQuery = SearchBox.Text;
            }
        }

        private async void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel != null && CategoryComboBox.SelectedItem is string selectedCategory)
            {
                ViewModel.SelectedCategory = selectedCategory;
                ViewModel.SearchCommand.Execute(null);
                await System.Threading.Tasks.Task.Delay(100); // Small delay for UI to update
                WallpapersGrid.ItemsSource = ViewModel.Wallpapers;
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.SearchCommand.Execute(null);
                WallpapersGrid.ItemsSource = ViewModel.Wallpapers;
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.RefreshCommand.Execute(null);
                WallpapersGrid.ItemsSource = ViewModel.Wallpapers;
            }
        }
    }
}
