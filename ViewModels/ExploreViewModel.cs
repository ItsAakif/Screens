using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Screens.WinUI.Services;
using Screens.WinUI.Models;
using Screens.WinUI.Views;
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
    public partial class ExploreViewModel : ViewModelBase
    {
        private readonly IWallpaperService _wallpaperService;
        private readonly ILiveWallpaperService _liveWallpaperService;
        private readonly IImageCacheService _imageCacheService;
        private readonly IWallpaperRepository _wallpaperRepository;
        private readonly ILoggingService _loggingService;

        [ObservableProperty]
        private ObservableCollection<WallpaperModel> _wallpapers = new();

        [ObservableProperty]
        private ObservableCollection<string> _categories = new();

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private string _selectedCategory = "All";

        [ObservableProperty]
        private bool _isLoading = false;

        public ExploreViewModel(
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

        public async Task InitializeAsync()
        {
            await LoadCategoriesAsync();
            await LoadWallpapersAsync();
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                var wallpaperPath = Path.Combine(AppContext.BaseDirectory, "Wallpaper");
                var cats = await Task.Run(() =>
                {
                    var list = new List<string> { "All" };
                    if (Directory.Exists(wallpaperPath))
                        foreach (var folder in Directory.GetDirectories(wallpaperPath))
                            list.Add(Path.GetFileName(folder));
                    return list;
                });

                Categories.Clear();
                foreach (var c in cats) Categories.Add(c);

                _loggingService.LogInformation($"Loaded {Categories.Count} categories");
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Failed to load categories", ex);
            }
        }

        [RelayCommand]
        private async Task Search() => await LoadWallpapersAsync();

        [RelayCommand]
        private async Task Refresh() => await LoadWallpapersAsync();

        private async Task LoadWallpapersAsync()
        {
            try
            {
                IsLoading = true;
                Wallpapers.Clear();

                var wallpaperPath = Path.Combine(AppContext.BaseDirectory, "Wallpaper");
                var category      = SelectedCategory == "All" ? null : SelectedCategory;
                var query         = SearchQuery;

                // Scan on background thread
                var items = await Task.Run(() => ScanWallpapers(wallpaperPath, category, query));

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

        private static List<WallpaperModel> ScanWallpapers(string wallpaperPath, string? category, string? query)
        {
            var result = new List<WallpaperModel>();
            if (!Directory.Exists(wallpaperPath)) return result;

            foreach (var folder in Directory.GetDirectories(wallpaperPath))
            {
                var categoryName = Path.GetFileName(folder);
                if (category != null && categoryName != category) continue;

                IEnumerable<string> files = Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(IsImageFile);

                if (!string.IsNullOrWhiteSpace(query))
                    files = files.Where(f => Path.GetFileNameWithoutExtension(f)
                        .Contains(query, StringComparison.OrdinalIgnoreCase));

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
                SetBusy(true); ClearError();
                var imagePath = wallpaper.LocalPath ?? wallpaper.ImageUrl;
                if (wallpaper.IsLiveWallpaper)
                    await _liveWallpaperService.SetLiveWallpaperAsync(imagePath);
                else
                    await _wallpaperService.SetDesktopWallpaperAsync(imagePath);
                await _wallpaperRepository.AddToDownloadHistoryAsync(wallpaper);
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
        private async Task AddToFavorites(WallpaperModel wallpaper)
        {
            try
            {
                SetBusy(true); ClearError();
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
                SetBusy(true); ClearError();
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
