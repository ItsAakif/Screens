using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Screens.WinUI.Views.Pages;
using Screens.WinUI.ViewModels;
using Screens.WinUI.Views;
using Screens.WinUI.Services;
using System;

namespace Screens.WinUI.Views
{
    public sealed partial class MainWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly INavigationService _navigationService;
        private readonly ILoggingService _loggingService;

        public MainWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider   = serviceProvider;
            _navigationService = serviceProvider.GetRequiredService<INavigationService>();
            _loggingService    = serviceProvider.GetRequiredService<ILoggingService>();
            Title = "Screens - Wallpaper Manager";

            try
            {
                _navigationService.Initialize(ContentFrame);
                AppWindow.Resize(new Windows.Graphics.SizeInt32(1400, 900));
                NavigateToPage("home");
                NavView.SelectedItem = NavHome;
                _loggingService.LogInformation("MainWindow initialized");
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error in MainWindow constructor", ex);
            }
        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item && item.Tag != null)
                NavigateToPage(item.Tag.ToString());
        }

        private void TogglePaneButton_Click(object sender, RoutedEventArgs e)
        {
            NavView.IsPaneOpen = !NavView.IsPaneOpen;
            TogglePaneText.Text = NavView.IsPaneOpen ? "Collapse" : "Expand";
        }

        private void NavigateToPage(string? tag)
        {
            try
            {
                Type? pageType = tag switch
                {
                    "home"     => typeof(HomePage),
                    "explore"  => typeof(ExplorePage),
                    "favorites"=> typeof(FavoritesPage),
                    "history"  => typeof(DownloadHistoryPage),
                    "settings" => typeof(SettingsPage),
                    "generate" => typeof(GeneratePage),
                    _ => null
                };

                if (pageType is null) return;

                object? viewModel = tag switch
                {
                    "home"     => _serviceProvider.GetService<HomeViewModel>(),
                    "explore"  => _serviceProvider.GetService<ExploreViewModel>(),
                    "favorites"=> _serviceProvider.GetService<FavoritesViewModel>(),
                    "history"  => _serviceProvider.GetService<DownloadHistoryViewModel>(),
                    "settings" => _serviceProvider.GetService<SettingsViewModel>(),
                    "generate" => _serviceProvider.GetService<GenerateViewModel>(),
                    _ => null
                };

                if (viewModel != null)
                    _navigationService.Navigate(pageType, viewModel);
                else
                    _loggingService.LogWarning($"ViewModel not resolved for tag: {tag}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Navigation error for tag: {tag}", ex);
            }
        }
    }
}
