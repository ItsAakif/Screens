using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Screens.WinUI.Services;
using Screens.WinUI.Views;
using Screens.WinUI.Models;
using WallpaperModel = Screens.WinUI.Models.Wallpaper;
using Screens.WinUI.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace Screens.WinUI.ViewModels
{
    public partial class DownloadHistoryViewModel : ViewModelBase
    {
        private readonly IWallpaperService _wallpaperService;
        private readonly ILiveWallpaperService _liveWallpaperService;
        private readonly IWallpaperRepository _wallpaperRepository;
        private readonly ILoggingService _loggingService;

        [ObservableProperty]
        private ObservableCollection<WallpaperModel> _downloadHistory = new();

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private int _totalDownloads = 0;

        [ObservableProperty]
        private string _emptyStateMessage = "No download history yet. Wallpapers you set will appear here.";

        // Computed properties used by XAML
        public Visibility IsEmpty => TotalDownloads == 0 ? Visibility.Visible : Visibility.Collapsed;
        public string TotalDownloadsText => TotalDownloads == 0 ? "" : $"Total: {TotalDownloads} wallpapers";

        public DownloadHistoryViewModel(
            IWallpaperService wallpaperService,
            ILiveWallpaperService liveWallpaperService,
            IWallpaperRepository wallpaperRepository,
            ILoggingService loggingService)
        {
            _wallpaperService     = wallpaperService;
            _liveWallpaperService = liveWallpaperService;
            _wallpaperRepository  = wallpaperRepository;
            _loggingService       = loggingService;
        }

        public async Task InitializeAsync() => await LoadDownloadHistoryAsync();

        [RelayCommand]
        private async Task LoadDownloadHistoryAsync()
        {
            try
            {
                IsLoading = true;
                ClearError();

                var history = await _wallpaperRepository.GetDownloadHistoryAsync();
                DownloadHistory.Clear();
                foreach (var item in history)
                    DownloadHistory.Add(item);

                TotalDownloads = DownloadHistory.Count;
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(TotalDownloadsText));

                if (TotalDownloads == 0)
                    EmptyStateMessage = "No download history yet. Wallpapers you set will appear here.";

                _loggingService.LogInformation($"Loaded {TotalDownloads} items from download history");
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Failed to load download history", ex);
                SetError($"Failed to load history: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SetAsDesktopWallpaper(WallpaperModel wallpaper)
        {
            try
            {
                SetBusy(true); ClearError();
                var imagePath = wallpaper.LocalPath ?? wallpaper.ImageUrl;
                if (wallpaper.IsLiveWallpaper)
                    await _liveWallpaperService.SetLiveWallpaperAsync(imagePath);
                else
                    await _wallpaperService.SetDesktopWallpaperAsync(imagePath);
                _loggingService.LogInformation($"Desktop wallpaper set: {wallpaper.Title}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Failed to set desktop wallpaper: {wallpaper.Title}", ex);
                SetError($"Failed to set wallpaper: {ex.Message}");
            }
            finally { SetBusy(false); }
        }

        [RelayCommand]
        private async Task SetAsLockScreen(WallpaperModel wallpaper)
        {
            try
            {
                SetBusy(true); ClearError();
                await _wallpaperService.SetLockScreenWallpaperAsync(wallpaper.LocalPath ?? wallpaper.ImageUrl);
                _loggingService.LogInformation($"Lock screen set: {wallpaper.Title}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Failed to set lock screen: {wallpaper.Title}", ex);
                SetError($"Failed to set lock screen: {ex.Message}");
            }
            finally { SetBusy(false); }
        }

        [RelayCommand]
        private void PreviewWallpaper(WallpaperModel wallpaper)
        {
            new WallpaperPreviewDialog { Wallpaper = wallpaper }.ShowAsync();
        }

        [RelayCommand]
        private async Task ClearHistory()
        {
            try
            {
                SetBusy(true); ClearError();
                await _wallpaperRepository.ClearDownloadHistoryAsync();
                DownloadHistory.Clear();
                TotalDownloads = 0;
                EmptyStateMessage = "History cleared.";
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(TotalDownloadsText));
                _loggingService.LogInformation("Download history cleared");
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Failed to clear download history", ex);
                SetError($"Failed to clear history: {ex.Message}");
            }
            finally { SetBusy(false); }
        }

        [RelayCommand]
        private async Task RemoveFromHistory(WallpaperModel wallpaper)
        {
            try
            {
                SetBusy(true); ClearError();
                await _wallpaperRepository.RemoveFromDownloadHistoryAsync(wallpaper.Id);
                DownloadHistory.Remove(wallpaper);
                TotalDownloads = DownloadHistory.Count;
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(TotalDownloadsText));
                _loggingService.LogInformation($"Removed from history: {wallpaper.Title}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Failed to remove from history: {wallpaper.Title}", ex);
                SetError($"Failed to remove: {ex.Message}");
            }
            finally { SetBusy(false); }
        }
    }
}
