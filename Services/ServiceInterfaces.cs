using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Screens.WinUI.Models;

namespace Screens.WinUI.Services
{
    public interface INavigationService
    {
        void Initialize(Frame frame);
        void Navigate(Type pageType);
        void Navigate(Type pageType, object parameter);
        void GoBack();
        bool CanGoBack { get; }
    }

    public interface ILoggingService
    {
        Task InitializeAsync();
        void LogInformation(string message);
        void LogWarning(string message);
        void LogError(string message, Exception? exception = null);
    }

    public interface IImageCacheService
    {
        Task InitializeAsync();
        Task<string> GetOrDownloadImageAsync(string imageUrl);
        Task ClearCacheAsync();
        long GetCacheSize();
    }

    public interface IDatabaseService
    {
        Task InitializeAsync();
        Task<List<T>> QueryAsync<T>(string sql, object? parameters = null);
        Task ExecuteAsync(string sql, object? parameters = null);
        void Dispose();
    }

    public interface ISettingsService
    {
        Task<AppSettings> LoadSettingsAsync();
        Task SaveSettingsAsync(AppSettings settings);
    }

    public interface IWallpaperRepository
    {
        Task<List<Wallpaper>> GetFavoritesAsync(int page = 1, int pageSize = 20);
        Task<bool> IsFavoriteAsync(string wallpaperId);
        Task AddFavoriteAsync(Wallpaper wallpaper);
        Task RemoveFavoriteAsync(string wallpaperId);
        Task AddToDownloadHistoryAsync(Wallpaper wallpaper);
        Task<List<Wallpaper>> GetDownloadHistoryAsync(int page = 1, int pageSize = 50);
        Task ClearDownloadHistoryAsync();
        Task RemoveFromDownloadHistoryAsync(string wallpaperId);
    }

    public interface ILiveWallpaperService
    {
        bool IsRunning { get; }
        bool IsPaused { get; }
        event EventHandler<bool>? IsRunningChanged;
        Task SetLiveWallpaperAsync(string imagePath);
        Task StopLiveWallpaperAsync();
        void PauseLiveWallpaper();
        void ResumeLiveWallpaper();
    }

    public interface IWallpaperService
    {
        Task SetDesktopWallpaperAsync(string imagePath);
        Task SetDesktopWallpaperAsync(string imagePath, WallpaperStyle style);
        Task SetLockScreenWallpaperAsync(string imagePath);
    }
}
