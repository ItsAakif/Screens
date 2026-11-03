using Windows.Storage;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Screens.WinUI.Services
{
    public class ImageCacheService : IImageCacheService
    {
        private string? _cachePath;
        private readonly HttpClient _httpClient;
        private readonly long _maxCacheSizeBytes;

        public ImageCacheService(long maxCacheSizeMB = 1000)
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _maxCacheSizeBytes = maxCacheSizeMB * 1024 * 1024;
        }

        public async Task InitializeAsync()
        {
            var localFolder = ApplicationData.Current.LocalFolder.Path;
            _cachePath = Path.Combine(localFolder, "Screens", "Cache");

            if (!Directory.Exists(_cachePath))
            {
                Directory.CreateDirectory(_cachePath);
            }
        }

        private string CachePath
        {
            get
            {
                if (_cachePath == null)
                {
                    throw new InvalidOperationException("ImageCacheService must be initialized before use. Call InitializeAsync() first.");
                }
                return _cachePath;
            }
        }

        public async Task<string> GetOrDownloadImageAsync(string imageUrl)
        {
            // Check if it's already a local file
            if (File.Exists(imageUrl))
            {
                return imageUrl;
            }

            var fileName = GenerateFileName(imageUrl);
            var localPath = Path.Combine(CachePath, fileName);

            // Return cached version if exists
            if (File.Exists(localPath))
            {
                return localPath;
            }

            // Download the image
            try
            {
                var response = await _httpClient.GetAsync(imageUrl);
                response.EnsureSuccessStatusCode();

                var imageData = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(localPath, imageData);

                return localPath;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to download image: {ex.Message}", ex);
            }
        }

        public async Task ClearCacheAsync()
        {
            if (Directory.Exists(CachePath))
            {
                var files = Directory.GetFiles(CachePath);
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // Ignore errors
                    }
                }
            }

            await Task.CompletedTask;
        }

        public long GetCacheSize()
        {
            if (!Directory.Exists(CachePath))
            {
                return 0;
            }

            var files = Directory.GetFiles(CachePath);
            long totalSize = 0;

            foreach (var file in files)
            {
                try
                {
                    totalSize += new FileInfo(file).Length;
                }
                catch
                {
                    // Ignore errors
                }
            }

            return totalSize;
        }

        private string GenerateFileName(string url)
        {
            var hash = url.GetHashCode();
            var extension = Path.GetExtension(new Uri(url).AbsolutePath);
            return $"{Math.Abs(hash)}{extension}";
        }
    }
}
