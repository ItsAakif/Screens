using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Screens.WinUI.Services;
using System;
using System.Threading.Tasks;

namespace Screens.WinUI.Helpers
{
    /// <summary>
    /// Helper class for cached image loading in XAML
    /// </summary>
    public static class ImageCacheHelper
    {
        private static IImageCacheService? _imageCacheService;

        public static void Initialize(IImageCacheService imageCacheService)
        {
            _imageCacheService = imageCacheService;
        }

        /// <summary>
        /// Attached property for setting cached image source
        /// </summary>
        public static readonly DependencyProperty CachedSourceProperty =
            DependencyProperty.RegisterAttached(
                "CachedSource",
                typeof(string),
                typeof(ImageCacheHelper),
                new PropertyMetadata(null, OnCachedSourceChanged)
            );

        public static string GetCachedSource(Image obj)
        {
            return (string)obj.GetValue(CachedSourceProperty);
        }

        public static void SetCachedSource(Image obj, string value)
        {
            obj.SetValue(CachedSourceProperty, value);
        }

        private static async void OnCachedSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Image image && e.NewValue is string imageUrl && !string.IsNullOrEmpty(imageUrl))
            {
                if (_imageCacheService == null)
                {
                    // Fallback to direct source if cache service not available
                    image.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(imageUrl));
                    return;
                }

                try
                {
                    // Show placeholder or current image while loading
                    image.Opacity = 0.6;

                    // Get cached or download image
                    var localPath = await _imageCacheService.GetOrDownloadImageAsync(imageUrl);

                    // Set the image source
                    image.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(localPath));
                    image.Opacity = 1.0;
                }
                catch
                {
                    // On error, try direct loading
                    try
                    {
                        image.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(imageUrl));
                        image.Opacity = 1.0;
                    }
                    catch
                    {
                        // If all fails, show error state
                        image.Opacity = 0.3;
                    }
                }
            }
        }
    }
}
