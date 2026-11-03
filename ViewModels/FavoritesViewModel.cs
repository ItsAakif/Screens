using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Screens.WinUI.Services;
using Screens.WinUI.Models;
using WallpaperModel = Screens.WinUI.Models.Wallpaper;
using Screens.WinUI.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace Screens.WinUI.ViewModels
{
    public partial class FavoritesViewModel : ViewModelBase
    {
        private readonly IWallpaperRepository _wallpaperRepository;
        private readonly IWallpaperService _wallpaperService;
        private readonly ILiveWallpaperService _liveWallpaperService;
        private readonly ILoggingService _loggingService;

        [ObservableProperty]
        private ObservableCollection<WallpaperModel> _favorites = new();

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private int _totalFavorites = 0;

        public Visibility IsEmpty => TotalFavorites == 0 ? Visibility.Visible : Visibility.Collapsed;

        public FavoritesViewModel(
            IWallpaperRepository wallpaperRepository,
            IWallpaperService wallpaperService,
            ILiveWallpaperService liveWallpaperService,
            ILoggingService loggingService)
        {
            _wallpaperRepository  = wallpaperRepository;
            _wallpaperService     = wallpaperService;
            _liveWallpaperService = liveWallpaperService;
            _loggingService       = loggingService;
        }

        public async Task InitializeAsync() => await LoadFavoritesAsync();

        [RelayCommand]
        private async Task RefreshFavorites() => await LoadFavoritesAsync();

        private async Task LoadFavoritesAsync()
        {
            try
            {
                IsLoading = true;
                ClearError();

                var loadedFavorites = await _wallpaperRepository.GetFavoritesAsync();
                Favorites.Clear();
                foreach (var fav in loadedFavorites)
                    Favorites.Add(fav);

                TotalFavorites = Favorites.Count;
                OnPropertyChanged(nameof(IsEmpty));
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Failed to load favorites", ex);
                SetError($"Failed to load favorites: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task RemoveFromFavorites(WallpaperModel wallpaper)
        {
            try
            {
                SetBusy(true); ClearError();
                await _wallpaperRepository.RemoveFavoriteAsync(wallpaper.Id);
                Favorites.Remove(wallpaper);
                TotalFavorites--;
                OnPropertyChanged(nameof(IsEmpty));
                _loggingService.LogInformation($"Removed from favorites: {wallpaper.Title}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Failed to remove from favorites: {wallpaper.Title}", ex);
                SetError($"Failed to remove: {ex.Message}");
            }
            finally { SetBusy(false); }
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
    }
}
