using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Screens.WinUI.Services;
using Screens.WinUI.Models;
using AppSettingsModel = Screens.WinUI.Models.AppSettings;
using Screens.WinUI.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Screens.WinUI.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;
        private readonly IImageCacheService _imageCacheService;
        private readonly ILiveWallpaperService _liveWallpaperService;
        private readonly ILoggingService _loggingService;

        [ObservableProperty]
        private AppSettingsModel _settings = new();

        [ObservableProperty]
        private ObservableCollection<string> _availableProviders = new();

        [ObservableProperty]
        private string _selectedProvider = "Local";

        [ObservableProperty]
        private AppTheme _selectedTheme = AppTheme.Dark;

        [ObservableProperty]
        private GridSize _selectedGridSize = GridSize.Medium;

        [ObservableProperty]
        private WallpaperStyle _selectedWallpaperStyle = WallpaperStyle.Fill;

        [ObservableProperty]
        private bool _isLiveWallpaperRunning = false;

        [ObservableProperty]
        private bool _isLiveWallpaperPaused = false;

        // Computed properties for XAML  avoids missing converters
        public InfoBarSeverity LiveWallpaperSeverity    => _isLiveWallpaperRunning ? InfoBarSeverity.Success : InfoBarSeverity.Informational;
        public string          LiveWallpaperStatusTitle => _isLiveWallpaperRunning ? "Live Wallpaper Active" : "No Live Wallpaper";
        public Visibility      StopButtonVisibility     => _isLiveWallpaperRunning ? Visibility.Visible : Visibility.Collapsed;

        // Manual commands  not relying on [RelayCommand] source generation
        public IAsyncRelayCommand SaveSettingsAsyncCommand     { get; }
        public IAsyncRelayCommand ClearCacheAsyncCommand       { get; }
        public IAsyncRelayCommand StopLiveWallpaperAsyncCommand { get; }
        public IRelayCommand      PauseLiveWallpaperCommand    { get; }
        public IRelayCommand      ResumeLiveWallpaperCommand   { get; }

        partial void OnIsLiveWallpaperRunningChanged(bool value)
        {
            OnPropertyChanged(nameof(LiveWallpaperSeverity));
            OnPropertyChanged(nameof(LiveWallpaperStatusTitle));
            OnPropertyChanged(nameof(StopButtonVisibility));
        }

        partial void OnSelectedProviderChanged(string value)           => Settings.SelectedProvider = value;
        partial void OnSelectedGridSizeChanged(GridSize value)         => Settings.GridSize = value;
        partial void OnSelectedWallpaperStyleChanged(WallpaperStyle value) => Settings.WallpaperStyle = value;

        partial void OnSelectedThemeChanged(AppTheme value)
        {
            Settings.Theme = value;
            ApplyTheme(value);
        }

        public SettingsViewModel(
            ISettingsService settingsService,
            IImageCacheService imageCacheService,
            ILiveWallpaperService liveWallpaperService,
            ILoggingService loggingService)
        {
            _settingsService      = settingsService;
            _imageCacheService    = imageCacheService;
            _liveWallpaperService = liveWallpaperService;
            _loggingService       = loggingService;

            SaveSettingsAsyncCommand      = new AsyncRelayCommand(SaveSettingsAsync);
            ClearCacheAsyncCommand        = new AsyncRelayCommand(ClearCacheAsync);
            StopLiveWallpaperAsyncCommand = new AsyncRelayCommand(StopLiveWallpaperAsync);
            PauseLiveWallpaperCommand     = new RelayCommand(PauseLiveWallpaper);
            ResumeLiveWallpaperCommand    = new RelayCommand(ResumeLiveWallpaper);

            _liveWallpaperService.IsRunningChanged += (s, e) =>
            {
                IsLiveWallpaperRunning = _liveWallpaperService.IsRunning;
                IsLiveWallpaperPaused  = _liveWallpaperService.IsPaused;
            };
        }

        public async Task InitializeAsync() => await LoadSettingsAsync();

        private async Task LoadSettingsAsync()
        {
            try
            {
                Settings = await _settingsService.LoadSettingsAsync();
                SelectedProvider       = Settings.SelectedProvider;
                SelectedTheme          = Settings.Theme;
                SelectedGridSize       = Settings.GridSize;
                SelectedWallpaperStyle = Settings.WallpaperStyle;

                AvailableProviders.Clear();
                AvailableProviders.Add("Local");

                ApplyTheme(Settings.Theme);
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Failed to load settings", ex);
                SetError($"Failed to load settings: {ex.Message}");
            }
        }

        private async Task SaveSettingsAsync()
        {
            try
            {
                SetBusy(true);
                ClearError();
                Settings.SelectedProvider  = SelectedProvider;
                Settings.Theme             = SelectedTheme;
                Settings.GridSize          = SelectedGridSize;
                Settings.WallpaperStyle    = SelectedWallpaperStyle;
                await _settingsService.SaveSettingsAsync(Settings);
                _loggingService.LogInformation("Settings saved");
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Failed to save settings", ex);
                SetError($"Failed to save settings: {ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task ClearCacheAsync()
        {
            try
            {
                SetBusy(true);
                ClearError();
                await _imageCacheService.ClearCacheAsync();
                _loggingService.LogInformation("Cache cleared");
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Failed to clear cache", ex);
                SetError($"Failed to clear cache: {ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task StopLiveWallpaperAsync()
        {
            try
            {
                await _liveWallpaperService.StopLiveWallpaperAsync();
                IsLiveWallpaperRunning = false;
                IsLiveWallpaperPaused  = false;
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Failed to stop live wallpaper", ex);
                SetError($"Failed to stop: {ex.Message}");
            }
        }

        private void PauseLiveWallpaper()
        {
            try
            {
                _liveWallpaperService.PauseLiveWallpaper();
                IsLiveWallpaperPaused = _liveWallpaperService.IsPaused;
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Failed to pause live wallpaper", ex);
            }
        }

        private void ResumeLiveWallpaper()
        {
            try
            {
                _liveWallpaperService.ResumeLiveWallpaper();
                IsLiveWallpaperPaused = _liveWallpaperService.IsPaused;
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Failed to resume live wallpaper", ex);
            }
        }

        private void ApplyTheme(AppTheme theme)
        {
            try
            {
                if (Application.Current is App app && app.MainWindow?.Content is FrameworkElement root)
                {
                    root.RequestedTheme = theme switch
                    {
                        AppTheme.Dark  => ElementTheme.Dark,
                        AppTheme.Light => ElementTheme.Light,
                        _              => ElementTheme.Default
                    };
                }
                _loggingService.LogInformation($"Theme applied: {theme}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Failed to apply theme", ex);
            }
        }
    }
}
