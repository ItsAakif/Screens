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
    /// Unsplash API provider for wallpapers
    /// </summary>
    public class UnsplashProvider : IWallpaperProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILoggingService? _loggingService;
        private string? _apiKey;

        public string Name => "Unsplash";
        public string DisplayName => "Unsplash";
        public bool RequiresApiKey => true;
        private const string BaseUrl = "https://api.unsplash.com";

        public UnsplashProvider(HttpClient httpClient, ILoggingService? loggingService = null)
        {
            _httpClient = httpClient;
            _loggingService = loggingService;
        }

        public void SetApiKey(string apiKey)
        {
            _loggingService?.LogInformation($"Unsplash SetApiKey called with key length: {apiKey?.Length ?? 0}");
            _apiKey = apiKey;
            // Unsplash API expects "Client-ID {apiKey}" format
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Client-ID {apiKey}");
            _loggingService?.LogInformation($"Unsplash Authorization header set to: Client-ID {apiKey?.Substring(0, Math.Min(10, apiKey?.Length ?? 0))}...");
        }

        public async Task<bool> ValidateApiKeyAsync()
        {
            if (string.IsNullOrEmpty(_apiKey))
                return false;

            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/users/USERNAME");
                // Don't check response status - we just want to ensure the key format is valid
                return !string.IsNullOrEmpty(_apiKey);
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<string>> GetCategoriesAsync()
        {
            // Return common Unsplash topics/categories
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
                "Urban",
                "Textures",
                "Animals",
                "Food"
            };
        }

        public async Task<List<WallpaperModel>> GetFeaturedWallpapersAsync(int page = 1, int pageSize = 20)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    _loggingService?.LogWarning("Unsplash API key not configured");
                    return new List<Wallpaper>();
                }

                // Fetch desktop wallpapers only (landscape orientation)
                var url = $"{BaseUrl}/photos?page={page}&per_page={pageSize * 2}&order_by=popular&orientation=landscape";
                _loggingService?.LogInformation($"Unsplash API Request: {url}");
                _loggingService?.LogInformation($"Authorization header: {_httpClient.DefaultRequestHeaders.Authorization}");

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _loggingService?.LogError($"Unsplash API error: {response.StatusCode}");
                    _loggingService?.LogError($"Error response: {errorContent}");
                    return new List<Wallpaper>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var photos = JsonSerializer.Deserialize<List<JsonElement>>(content);

                var wallpapers = new List<WallpaperModel>();
                foreach (var photo in photos)
                {
                    var width = photo.GetProperty("width").GetInt32();
                    var height = photo.GetProperty("height").GetInt32();

                    // Filter for desktop wallpapers only (minimum 1920x1080 or similar landscape ratio)
                    if (width < 1920 || height < 1080 || width < height)
                        continue;

                    wallpapers.Add(new WallpaperModel
                    {
                        Id = photo.GetProperty("id").GetString() ?? string.Empty,
                        Title = photo.GetProperty("description").GetString()
                            ?? photo.GetProperty("alt_description").GetString()
                            ?? "Untitled",
                        ImageUrl = photo.GetProperty("urls").GetProperty("regular").GetString() ?? string.Empty,
                        ThumbnailUrl = photo.GetProperty("urls").GetProperty("small").GetString() ?? string.Empty,
                        Category = "Featured",
                        Dimensions = $"{width}x{height}",
                        SourceProvider = "Unsplash",
                        Author = photo.GetProperty("user").GetProperty("name").GetString() ?? "Unknown",
                        AuthorUrl = photo.GetProperty("user").GetProperty("links").GetProperty("html").GetString() ?? string.Empty,
                        SourceUrl = photo.GetProperty("links").GetProperty("html").GetString() ?? string.Empty,
                        IsFavorite = false
                    });

                    // Stop if we've collected enough desktop wallpapers
                    if (wallpapers.Count >= pageSize)
                        break;
                }

                _loggingService?.LogInformation($"Fetched {wallpapers.Count} wallpapers from Unsplash");
                return wallpapers;
            }
            catch (Exception ex)
            {
                _loggingService?.LogError("Failed to fetch featured wallpapers from Unsplash", ex);
                return new List<Wallpaper>();
            }
        }

        public async Task<List<WallpaperModel>> SearchWallpapersAsync(string query, int page = 1, int pageSize = 20)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    _loggingService?.LogWarning("Unsplash API key not configured");
                    return new List<Wallpaper>();
                }

                // Fetch desktop wallpapers only (landscape orientation)
                var response = await _httpClient.GetAsync(
                    $"{BaseUrl}/search/photos?query={Uri.EscapeDataString(query)}&page={page}&per_page={pageSize * 2}&orientation=landscape");

                if (!response.IsSuccessStatusCode)
                {
                    _loggingService?.LogError($"Unsplash API error: {response.StatusCode}");
                    return new List<Wallpaper>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(content);
                var photos = data.GetProperty("results").EnumerateArray();

                var wallpapers = new List<WallpaperModel>();
                foreach (var photo in photos)
                {
                    var width = photo.GetProperty("width").GetInt32();
                    var height = photo.GetProperty("height").GetInt32();

                    // Filter for desktop wallpapers only (minimum 1920x1080 or similar landscape ratio)
                    if (width < 1920 || height < 1080 || width < height)
                        continue;

                    wallpapers.Add(new WallpaperModel
                    {
                        Id = photo.GetProperty("id").GetString() ?? string.Empty,
                        Title = photo.GetProperty("description").GetString()
                            ?? photo.GetProperty("alt_description").GetString()
                            ?? query,
                        ImageUrl = photo.GetProperty("urls").GetProperty("regular").GetString() ?? string.Empty,
                        ThumbnailUrl = photo.GetProperty("urls").GetProperty("small").GetString() ?? string.Empty,
                        Category = query,
                        Dimensions = $"{width}x{height}",
                        SourceProvider = "Unsplash",
                        Author = photo.GetProperty("user").GetProperty("name").GetString() ?? "Unknown",
                        AuthorUrl = photo.GetProperty("user").GetProperty("links").GetProperty("html").GetString() ?? string.Empty,
                        SourceUrl = photo.GetProperty("links").GetProperty("html").GetString() ?? string.Empty,
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
                _loggingService?.LogError($"Failed to search Unsplash for: {query}", ex);
                return new List<Wallpaper>();
            }
        }

        public async Task<List<WallpaperModel>> GetWallpapersByCategoryAsync(string category, int page = 1, int pageSize = 20)
        {
            // For categories, just use search
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
