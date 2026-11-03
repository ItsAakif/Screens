using System.Collections.Generic;
using System.Threading.Tasks;
using Screens.WinUI.Models;
using WallpaperModel = Screens.WinUI.Models.Wallpaper;

namespace Screens.WinUI.Providers
{
    /// <summary>
    /// Interface for wallpaper provider implementations
    /// </summary>
    public interface IWallpaperProvider
    {
        /// <summary>
        /// Unique name of the provider
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Display name for UI
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Whether this provider requires an API key
        /// </summary>
        bool RequiresApiKey { get; }

        /// <summary>
        /// Gets available categories from this provider
        /// </summary>
        Task<List<string>> GetCategoriesAsync();

        /// <summary>
        /// Gets featured/trending wallpapers
        /// </summary>
        Task<List<WallpaperModel>> GetFeaturedWallpapersAsync(int page = 1, int pageSize = 20);

        /// <summary>
        /// Searches for wallpapers by query
        /// </summary>
        Task<List<WallpaperModel>> SearchWallpapersAsync(string query, int page = 1, int pageSize = 20);

        /// <summary>
        /// Gets wallpapers by category
        /// </summary>
        Task<List<WallpaperModel>> GetWallpapersByCategoryAsync(string category, int page = 1, int pageSize = 20);

        /// <summary>
        /// Downloads a wallpaper to local cache
        /// </summary>
        Task<string> DownloadWallpaperAsync(WallpaperModel wallpaper, string destinationPath);

        /// <summary>
        /// Sets the API key for this provider
        /// </summary>
        void SetApiKey(string apiKey);

        /// <summary>
        /// Validates the current API key
        /// </summary>
        Task<bool> ValidateApiKeyAsync();
    }

    /// <summary>
    /// Request parameters for wallpaper queries
    /// </summary>
    public class WallpaperRequest
    {
        public string? Query { get; set; }
        public string? Category { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Provider { get; set; }
        public string? Orientation { get; set; } // "landscape", "portrait", "squarish"
        public string? Size { get; set; } // "raw", "full", "regular", "small"
        public string? Color { get; set; } // Hex color or color name
    }
}
