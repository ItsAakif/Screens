using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Screens.WinUI.Services;
using Screens.WinUI.Models;
using Screens.WinUI;
using System;
using System.IO;
using Windows.Media.Core;
using Windows.Media.Playback;
using Microsoft.Extensions.DependencyInjection;

namespace Screens.WinUI.Views
{
    public sealed partial class WallpaperPreviewDialog : ContentDialog
    {
        private readonly IWallpaperService _wallpaperService;
        private readonly ILiveWallpaperService _liveWallpaperService;
        private readonly ILoggingService _loggingService;

        public Wallpaper? Wallpaper { get; set; }

        public WallpaperPreviewDialog()
        {
            this.InitializeComponent();
            Opened += WallpaperPreviewDialog_Opened;

            // Get services from DI container
            var app = (App)App.Current;
            var services = app.Services;
            _wallpaperService = services.GetRequiredService<IWallpaperService>();
            _liveWallpaperService = services.GetRequiredService<ILiveWallpaperService>();
            _loggingService = services.GetRequiredService<ILoggingService>();
        }

        private async void WallpaperPreviewDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            if (Wallpaper != null)
            {
                Title = Wallpaper.Title;

                var imagePath = Wallpaper.LocalPath ?? Wallpaper.ImageUrl;

                if (File.Exists(imagePath))
                {
                    LoadingRing.IsActive = false;

                    if (Wallpaper.IsLiveWallpaper)
                    {
                        // Use MediaPlayer for GIF/video
                        LiveBadge.Visibility = Visibility.Visible;

                        var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(imagePath);
                        var mediaSource = MediaSource.CreateFromStorageFile(file);

                        MediaPlayerElement.Source = mediaSource;
                        MediaPlayerElement.AutoPlay = true;
                        MediaPlayerElement.AreTransportControlsEnabled = true;
                        MediaPlayerElement.MediaPlayer.IsLoopingEnabled = true;
                    }
                    else
                    {
                        // Static image
                        var bitmap = new BitmapImage();
                        bitmap.UriSource = new Uri(imagePath);
                        // Would need to set to Image element, but we're using MediaPlayerElement
                        // For static images, we could use an Image element instead
                    }
                }
            }
        }

        private async void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Set as desktop wallpaper
            if (Wallpaper != null)
            {
                try
                {
                    var imagePath = Wallpaper.LocalPath ?? Wallpaper.ImageUrl;

                    if (Wallpaper.IsLiveWallpaper)
                    {
                        await _liveWallpaperService.SetLiveWallpaperAsync(imagePath);
                        _loggingService.LogInformation($"Desktop wallpaper set from preview: {Wallpaper.Title}");
                    }
                    else
                    {
                        await _wallpaperService.SetDesktopWallpaperAsync(imagePath);
                        _loggingService.LogInformation($"Desktop wallpaper set from preview: {Wallpaper.Title}");
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.LogError($"Failed to set desktop wallpaper from preview: {Wallpaper?.Title}", ex);
                }
            }
        }

        private async void OnSecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Set as lock screen wallpaper
            if (Wallpaper != null)
            {
                try
                {
                    var imagePath = Wallpaper.LocalPath ?? Wallpaper.ImageUrl;

                    // Lock screen doesn't support animation, but we'll set it anyway
                    await _wallpaperService.SetLockScreenWallpaperAsync(imagePath);
                    _loggingService.LogInformation($"Lock screen set from preview: {Wallpaper.Title}");
                }
                catch (Exception ex)
                {
                    _loggingService.LogError($"Failed to set lock screen from preview: {Wallpaper?.Title}", ex);
                }
            }
        }
    }
}
