using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Screens.WinUI.Models;
using Screens.WinUI.Services;
using WallpaperModel = Screens.WinUI.Models.Wallpaper;

namespace Screens.WinUI.Providers
{
    /// <summary>
    /// Pexels API provider for wallpapers
    /// </summary>
    public class PexelsProvider : IWallpaperProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILoggingService? _loggingService;
        private string? _apiKey;

        public string Name => "Pexels";
        public string DisplayName => "Pexels";
        public bool RequiresApiKey => true;
        private const string BaseUrl = "https://api.pexels.com/v1";

        public PexelsProvider(HttpClient httpClient, ILoggingService? loggingService = null)
        {
            _httpClient = httpClient;
            _loggingService = loggingService;
        }

        public void SetApiKey(string apiKey)
        {
            _apiKey = apiKey;
            // Pexels API expects just the API key in the Authorization header
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", apiKey);
        }

        public async Task<bool> ValidateApiKeyAsync()
        {
            if (string.IsNullOrEmpty(_apiKey))
                return false;

            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/curated?per_page=1");
                // Just check if we can make the request
                return response.StatusCode != System.Net.HttpStatusCode.Unauthorized;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<string>> GetCategoriesAsync()
        {
            // Pexels doesn't have explicit categories, but common search terms
            return new List<string>
            {
                "All",
                "Nature",
                "Architecture",
                "Travel",
                "Technology",
                "Abstract",
                "Minimal",
                "Space",
                "Ocean",
                "Forest",
                "Mountain",
                "City"
            };
        }

        public async Task<List<WallpaperModel>> GetFeaturedWallpapersAsync(int page = 1, int pageSize = 20)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    _loggingService?.LogWarning("Pexels API key not configured");
                    return new List<Wallpaper>();
                }

                // Fetch desktop wallpapers only (landscape orientation, high resolution)
                var response = await _httpClient.GetAsync(
                    $"{BaseUrl}/curated?page={page}&per_page={pageSize * 2}&orientation=landscape");

                if (!response.IsSuccessStatusCode)
                {
                    _loggingService?.LogError($"Pexels API error: {response.StatusCode}");
                    return new List<Wallpaper>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(content);
                var photos = data.GetProperty("photos").EnumerateArray();

                var wallpapers = new List<WallpaperModel>();
                foreach (var photo in photos)
                {
                    var width = photo.GetProperty("width").GetInt32();
                    var height = photo.GetProperty("height").GetInt32();

                    // Filter for desktop wallpapers only (minimum 1920x1080 or similar landscape ratio)
                    if (width < 1920 || height < 1080 || width < height)
                        continue;

                    var photographer = photo.GetProperty("photographer");
                    var src = photo.GetProperty("src");

                    wallpapers.Add(new WallpaperModel
                    {
                        Id = photo.GetProperty("id").GetInt32().ToString(),
                        Title = photo.GetProperty("alt").GetString() ?? "Curated Photo",
                        ImageUrl = src.GetProperty("large2x").GetString() ?? src.GetProperty("original").GetString() ?? string.Empty,
                        ThumbnailUrl = src.GetProperty("large").GetString() ?? src.GetProperty("medium").GetString() ?? string.Empty,
                        Category = "Curated",
                        Dimensions = $"{width}x{height}",
                        SourceProvider = "Pexels",
                        Author = photographer.GetString() ?? "Unknown",
                        AuthorUrl = photo.GetProperty("photographer_url").GetString() ?? string.Empty,
                        SourceUrl = photo.GetProperty("url").GetString() ?? string.Empty,
                        IsFavorite = false
                    });

                    // Stop if we've collected enough desktop wallpapers
                    if (wallpapers.Count >= pageSize)
                        break;
                }

                _loggingService?.LogInformation($"Fetched {wallpapers.Count} wallpapers from Pexels");
                return wallpapers;
            }
            catch (Exception ex)
            {
                _loggingService?.LogError("Failed to fetch featured wallpapers from Pexels", ex);
                return new List<Wallpaper>();
            }
        }

        public async Task<List<WallpaperModel>> SearchWallpapersAsync(string query, int page = 1, int pageSize = 20)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    _loggingService?.LogWarning("Pexels API key not configured");
                    return new List<Wallpaper>();
                }

                // Fetch desktop wallpapers only (landscape orientation)
                var response = await _httpClient.GetAsync(
                    $"{BaseUrl}/search?query={Uri.EscapeDataString(query)}&page={page}&per_page={pageSize * 2}&orientation=landscape");

                if (!response.IsSuccessStatusCode)
                {
                    _loggingService?.LogError($"Pexels API error: {response.StatusCode}");
                    return new List<Wallpaper>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(content);
                var photos = data.GetProperty("photos").EnumerateArray();

                var wallpapers = new List<WallpaperModel>();
                foreach (var photo in photos)
                {
                    var width = photo.GetProperty("width").GetInt32();
                    var height = photo.GetProperty("height").GetInt32();

                    // Filter for desktop wallpapers only (minimum 1920x1080 or similar landscape ratio)
                    if (width < 1920 || height < 1080 || width < height)
                        continue;

                    var photographer = photo.GetProperty("photographer");
                    var src = photo.GetProperty("src");

                    wallpapers.Add(new WallpaperModel
                    {
                        Id = photo.GetProperty("id").GetInt32().ToString(),
                        Title = photo.GetProperty("alt").GetString() ?? query,
                        ImageUrl = src.GetProperty("large2x").GetString() ?? src.GetProperty("original").GetString() ?? string.Empty,
                        ThumbnailUrl = src.GetProperty("large").GetString() ?? src.GetProperty("medium").GetString() ?? string.Empty,
                        Category = query,
                        Dimensions = $"{width}x{height}",
                        SourceProvider = "Pexels",
                        Author = photographer.GetString() ?? "Unknown",
                        AuthorUrl = photo.GetProperty("photographer_url").GetString() ?? string.Empty,
                        SourceUrl = photo.GetProperty("url").GetString() ?? string.Empty,
                        IsFavorite = false
                    });

                    // Stop if we've collected enough desktop wallpapers
                    if (wallpapers.Count >= pageSize)
                        break;
                }

                _loggingService?.LogInformation($"Found {wallpapers.Count} wallpapers for query: {query}");
                return wallpapers;
            }
            catch (Exception ex)
            {
                _loggingService?.LogError($"Failed to search Pexels for: {query}", ex);
                return new List<Wallpaper>();
            }
        }

        public async Task<List<WallpaperModel>> GetWallpapersByCategoryAsync(string category, int page = 1, int pageSize = 20)
        {
            // Pexels doesn't have category endpoints, so we search by category name
            return await SearchWallpapersAsync(category, page, pageSize);
        }

        public async Task<string> DownloadWallpaperAsync(WallpaperModel wallpaper, string destinationPath)
        {
            try
            {
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
    }
}
