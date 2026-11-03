using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Screens.WinUI.Views;
using Screens.WinUI.Views.Pages;
using Screens.WinUI.Services;
using Screens.WinUI.ViewModels;
using Screens.WinUI.Mvvm;
using Screens.WinUI.Providers;
using Screens.WinUI.Helpers;
using System;

namespace Screens.WinUI
{
    public partial class App : Application
    {
        private Window? _window;
        private IServiceProvider? _serviceProvider;

        public IServiceProvider Services => _serviceProvider
            ?? throw new InvalidOperationException("Services have not been initialized.");

        public Window? MainWindow => _window;

        public App()
        {
            InitializeComponent();
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Core services
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<ILiveWallpaperService, LiveWallpaperService>();
            services.AddSingleton<IWallpaperService, WallpaperService>();
            services.AddSingleton<IDatabaseService, DatabaseService>();
            services.AddSingleton<IImageCacheService, ImageCacheService>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<ILoggingService, LoggingService>();
            services.AddSingleton<IWallpaperRepository, WallpaperRepository>();
            services.AddSingleton<IWallpaperApiService, WallpaperApiService>();

            // Wallpaper providers
            services.AddSingleton<LocalWallpaperProvider>(sp =>
            {
                var wallpaperPath   = System.IO.Path.Combine(AppContext.BaseDirectory, "Wallpaper");
                var loggingService  = sp.GetService<ILoggingService>();
                loggingService?.LogInformation($"Local wallpaper path: {wallpaperPath}");
                return new LocalWallpaperProvider(wallpaperPath);
            });

            // ViewModels
            services.AddTransient<HomeViewModel>();
            services.AddTransient<ExploreViewModel>();
            services.AddTransient<FavoritesViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<GenerateViewModel>();
            services.AddTransient<DownloadHistoryViewModel>();

            // Views
            services.AddTransient<MainWindow>();
            services.AddTransient<HomePage>();
            services.AddTransient<ExplorePage>();
            services.AddTransient<FavoritesPage>();
            services.AddTransient<SettingsPage>();
            services.AddTransient<GeneratePage>();
            services.AddTransient<DownloadHistoryPage>();
        }

        private async System.Threading.Tasks.Task InitializeServicesAsync()
        {
            try
            {
                var loggingService = _serviceProvider?.GetService<ILoggingService>();
                if (loggingService != null)
                {
                    await loggingService.InitializeAsync();
                    loggingService.LogInformation("Application starting...");
                }

                var databaseService = _serviceProvider?.GetService<IDatabaseService>();
                if (databaseService != null)
                {
                    await databaseService.InitializeAsync();
                    loggingService?.LogInformation("Database initialized");
                }

                var apiService = _serviceProvider?.GetService<IWallpaperApiService>();
                if (apiService is WallpaperApiService wallpaperApiService)
                {
                    var localProvider = _serviceProvider?.GetService<LocalWallpaperProvider>();
                    if (localProvider != null)
                    {
                        wallpaperApiService.RegisterProvider(localProvider);
                        wallpaperApiService.SetProvider("Local");
                    }
                }

                var imageCacheService = _serviceProvider?.GetService<IImageCacheService>();
                if (imageCacheService != null)
                {
                    await imageCacheService.InitializeAsync();
                    loggingService?.LogInformation("Image cache initialized");
                    ImageCacheHelper.Initialize(imageCacheService);
                }

                loggingService?.LogInformation("All services initialized");
            }
            catch (Exception ex)
            {
                var loggingService = _serviceProvider?.GetService<ILoggingService>();
                loggingService?.LogError("Failed to initialize services", ex);
            }
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            await InitializeServicesAsync();

            _window = _serviceProvider?.GetService<MainWindow>();
            _window?.Activate();
        }
    }
}
