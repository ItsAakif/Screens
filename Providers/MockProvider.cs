using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Screens.WinUI.Models;
using Screens.WinUI.Services;
using WallpaperModel = Screens.WinUI.Models.Wallpaper;

namespace Screens.WinUI.Providers
{
    /// <summary>
    /// Mock provider for testing and offline mode
    /// </summary>
    public class MockProvider : IWallpaperProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILoggingService? _loggingService;
        private readonly List<WallpaperModel> _mockWallpapers;
        private readonly Random _random = new Random();

        public string Name => "MockProvider";
        public string DisplayName => "Sample Images";
        public bool RequiresApiKey => false;

        public MockProvider(HttpClient httpClient, ILoggingService? loggingService = null)
        {
            _httpClient = httpClient;
            _loggingService = loggingService;

            // Initialize mock data
            _mockWallpapers = GenerateMockWallpapers();
        }

        public void SetApiKey(string apiKey)
        {
            // Mock provider doesn't need an API key
        }

        public Task<bool> ValidateApiKeyAsync()
        {
            return Task.FromResult(true);
        }

        public Task<List<string>> GetCategoriesAsync()
        {
            return Task.FromResult(new List<string>
            {
                "All",
                "Nature",
                "Urban",
                "Abstract",
                "Minimal",
                "Technology",
                "Space"
            });
        }

        public async Task<List<WallpaperModel>> GetFeaturedWallpapersAsync(int page = 1, int pageSize = 20)
        {
            await Task.Delay(200); // Simulate network delay

            var skip = (page - 1) * pageSize;
            return _mockWallpapers.Skip(skip).Take(pageSize).ToList();
        }

        public async Task<List<WallpaperModel>> SearchWallpapersAsync(string query, int page = 1, int pageSize = 20)
        {
            await Task.Delay(200);

            // Simple search - returns all for now, in production would filter
            var skip = (page - 1) * pageSize;
            return _mockWallpapers.Skip(skip).Take(pageSize).ToList();
        }

        public async Task<List<WallpaperModel>> GetWallpapersByCategoryAsync(string category, int page = 1, int pageSize = 20)
        {
            await Task.Delay(200);

            // Filter by category
            var filtered = category.Equals("All", StringComparison.OrdinalIgnoreCase)
                ? _mockWallpapers
                : _mockWallpapers.Where(w => w.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();

            var skip = (page - 1) * pageSize;
            return filtered.Skip(skip).Take(pageSize).ToList();
        }

        public async Task<string> DownloadWallpaperAsync(WallpaperModel wallpaper, string destinationPath)
        {
            try
            {
                // For mock provider, we'll just use the picsum.photos URL
                var response = await _httpClient.GetAsync(wallpaper.ImageUrl);
                response.EnsureSuccessStatusCode();

                var bytes = await response.Content.ReadAsByteArrayAsync();
                await System.IO.File.WriteAllBytesAsync(destinationPath, bytes);

                _loggingService?.LogInformation($"Downloaded wallpaper: {wallpaper.Title}");
                return destinationPath;
            }
            catch (Exception ex)
            {
                _loggingService?.LogError($"Failed to download wallpaper: {wallpaper.Title}", ex);
                throw;
            }
        }

        private List<WallpaperModel> GenerateMockWallpapers()
        {
            var wallpapers = new List<WallpaperModel>();
            var categories = new[] { "Nature", "Urban", "Abstract", "Minimal", "Technology", "Space" };
            var titles = new[]
            {
                "Mountain Sunrise", "City Lights", "Ocean Waves", "Forest Path",
                "Desert Dunes", "Snowy Peaks", "City Skyline", "Starry Night",
                "Neon Lights", "Abstract Flow", "Minimal Geometry", "Space Nebula",
                "Northern Lights", "Tropical Beach", "Autumn Forest", "City Bridge",
                "Desert Sunset", "Mountain Lake", "Night City", "Galaxy Stars"
            };

            for (int i = 0; i < titles.Length; i++)
            {
                var width = _random.Next(1920, 3841);
                var height = _random.Next(1080, 2161);

                wallpapers.Add(new WallpaperModel
                {
                    Id = $"mock_{i + 1}",
                    Title = titles[i],
                    Category = categories[i % categories.Length],
                    ImageUrl = $"https://picsum.photos/1920/1080?random={i + 1}",
                    ThumbnailUrl = $"https://picsum.photos/300/200?random={i + 1}",
                    Dimensions = $"{width}x{height}",
                    Description = $"Beautiful {titles[i].ToLower()} wallpaper",
                    Author = "Mock Provider",
                    SourceUrl = $"https://picsum.photos",
                    FileSize = _random.Next(500000, 5000000),
                    SourceProvider = Name
                });
            }

            return wallpapers;
        }
    }
}
