using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Screens.WinUI.Services;
using Screens.WinUI.Views;
using Screens.WinUI.Models;
using WallpaperModel = Screens.WinUI.Models.Wallpaper;
using Screens.WinUI.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Screens.WinUI.ViewModels
{
    public partial class HomeViewModel : ViewModelBase
    {
        private readonly IWallpaperService _wallpaperService;
        private readonly ILiveWallpaperService _liveWallpaperService;
        private readonly IImageCacheService _imageCacheService;
        private readonly IWallpaperRepository _wallpaperRepository;
        private readonly ILoggingService _loggingService;

        [ObservableProperty]
        private ObservableCollection<WallpaperModel> _wallpapers = new();

        [ObservableProperty]
        private bool _isLoading = true;

        public HomeViewModel(
            IWallpaperService wallpaperService,
            ILiveWallpaperService liveWallpaperService,
            IImageCacheService imageCacheService,
            IWallpaperRepository wallpaperRepository,
            ILoggingService loggingService)
        {
            _wallpaperService     = wallpaperService;
            _liveWallpaperService = liveWallpaperService;
            _imageCacheService    = imageCacheService;
            _wallpaperRepository  = wallpaperRepository;
            _loggingService       = loggingService;
        }

        public async Task InitializeAsync() => await LoadWallpapersAsync();

        private async Task LoadWallpapersAsync()
        {
            try
            {
                IsLoading = true;
                Wallpapers.Clear();

                var wallpaperPath = Path.Combine(AppContext.BaseDirectory, "Wallpaper");

                // Scan files on background thread to keep UI responsive
                var items = await Task.Run(() => ScanWallpapers(wallpaperPath, maxPerFolder: 3));

                foreach (var item in items)
                    Wallpapers.Add(item);

                _loggingService.LogInformation($"Loaded {Wallpapers.Count} wallpapers");
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Failed to load wallpapers", ex);
                SetError($"Failed to load wallpapers: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private static List<WallpaperModel> ScanWallpapers(string wallpaperPath, int maxPerFolder = int.MaxValue)
        {
            var result = new List<WallpaperModel>();
            if (!Directory.Exists(wallpaperPath)) return result;

            foreach (var folder in Directory.GetDirectories(wallpaperPath))
            {
                var categoryName = Path.GetFileName(folder);
                var files = Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(IsImageFile)
                    .Take(maxPerFolder);

                foreach (var file in files)
                {
                    var isGif = Path.GetExtension(file).Equals(".gif", StringComparison.OrdinalIgnoreCase);
                    result.Add(new WallpaperModel
                    {
                        Id              = Guid.NewGuid().ToString(),
                        Title           = Path.GetFileNameWithoutExtension(file),
                        Category        = categoryName,
                        ImageUrl        = file,
                        ThumbnailUrl    = file,
                        LocalPath       = file,
                        SourceProvider  = "Local",
                        IsLiveWallpaper = isGif,
                        FileSize        = new FileInfo(file).Length,
                        Dimensions      = "1920x1080"
                    });
                }
            }
            return result;
        }

        [RelayCommand]
        private async Task SetAsDesktopWallpaper(WallpaperModel wallpaper)
        {
            try
            {
                SetBusy(true);
                ClearError();
                var imagePath = wallpaper.LocalPath ?? wallpaper.ImageUrl;
                if (wallpaper.IsLiveWallpaper)
                    await _liveWallpaperService.SetLiveWallpaperAsync(imagePath);
                else
                    await _wallpaperService.SetDesktopWallpaperAsync(imagePath);
                await _wallpaperRepository.AddToDownloadHistoryAsync(wallpaper);
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
                SetBusy(true);
                ClearError();
                var imagePath = wallpaper.LocalPath ?? wallpaper.ImageUrl;
                await _wallpaperService.SetLockScreenWallpaperAsync(imagePath);
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
            var previewDialog = new WallpaperPreviewDialog { Wallpaper = wallpaper };
            previewDialog.ShowAsync();
        }

        [RelayCommand]
        private async Task AddToFavorites(WallpaperModel wallpaper)
        {
            try
            {
                SetBusy(true);
                ClearError();
                await _wallpaperRepository.AddFavoriteAsync(wallpaper);
                wallpaper.IsFavorite = true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Failed to add to favorites: {wallpaper.Title}", ex);
                SetError($"Failed to add to favorites: {ex.Message}");
            }
            finally { SetBusy(false); }
        }

        [RelayCommand]
        private async Task RemoveFromFavorites(WallpaperModel wallpaper)
        {
            try
            {
                SetBusy(true);
                ClearError();
                await _wallpaperRepository.RemoveFavoriteAsync(wallpaper.Id);
                wallpaper.IsFavorite = false;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Failed to remove from favorites: {wallpaper.Title}", ex);
                SetError($"Failed to remove from favorites: {ex.Message}");
            }
            finally { SetBusy(false); }
        }

        private static bool IsImageFile(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLower();
            return ext is ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp";
        }
    }
}
