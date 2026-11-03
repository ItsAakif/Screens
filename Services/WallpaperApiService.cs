using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Screens.WinUI.Models;
using Screens.WinUI.Providers;

namespace Screens.WinUI.Services
{
    /// <summary>
    /// Service for managing wallpaper providers and API interactions
    /// </summary>
    public interface IWallpaperApiService
    {
        /// <summary>
        /// Register a wallpaper provider
        /// </summary>
        void RegisterProvider(IWallpaperProvider provider);

        /// <summary>
        /// Get all registered providers
        /// </summary>
        List<IWallpaperProvider> GetProviders();

        /// <summary>
        /// Set the active provider by name
        /// </summary>
        bool SetProvider(string providerName);

        /// <summary>
        /// Get the current active provider
        /// </summary>
        IWallpaperProvider? GetCurrentProvider();

        /// <summary>
        /// Get categories from the current provider
        /// </summary>
        Task<List<string>> GetCategoriesAsync();

        /// <summary>
        /// Get featured wallpapers from the current provider
        /// </summary>
        Task<List<Wallpaper>> GetFeaturedWallpapersAsync(int page = 1, int pageSize = 20);

        /// <summary>
        /// Search wallpapers using the current provider
        /// </summary>
        Task<List<Wallpaper>> SearchWallpapersAsync(string query, int page = 1, int pageSize = 20);

        /// <summary>
        /// Get wallpapers by category from the current provider
        /// </summary>
        Task<List<Wallpaper>> GetWallpapersByCategoryAsync(string category, int page = 1, int pageSize = 20);

        /// <summary>
        /// Get the current provider name
        /// </summary>
        string? CurrentProviderName { get; }
    }

    public class WallpaperApiService : IWallpaperApiService
    {
        private readonly Dictionary<string, IWallpaperProvider> _providers;
        private IWallpaperProvider? _currentProvider;

        public string? CurrentProviderName => _currentProvider?.Name;

        public WallpaperApiService()
        {
            _providers = new Dictionary<string, IWallpaperProvider>(StringComparer.OrdinalIgnoreCase);
        }

        public void RegisterProvider(IWallpaperProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            _providers[provider.Name] = provider;

            // Set as default if first provider
            if (_currentProvider == null)
            {
                _currentProvider = provider;
            }
        }

        public List<IWallpaperProvider> GetProviders()
        {
            return _providers.Values.ToList();
        }

        public bool SetProvider(string providerName)
        {
            if (_providers.TryGetValue(providerName, out var provider))
            {
                _currentProvider = provider;
                return true;
            }
            return false;
        }

        public IWallpaperProvider? GetCurrentProvider()
        {
            return _currentProvider;
        }

        public async Task<List<string>> GetCategoriesAsync()
        {
            if (_currentProvider == null)
                return new List<string>();

            return await _currentProvider.GetCategoriesAsync();
        }

        public async Task<List<Wallpaper>> GetFeaturedWallpapersAsync(int page = 1, int pageSize = 20)
        {
            if (_currentProvider == null)
                return new List<Wallpaper>();

            return await _currentProvider.GetFeaturedWallpapersAsync(page, pageSize);
        }

        public async Task<List<Wallpaper>> SearchWallpapersAsync(string query, int page = 1, int pageSize = 20)
        {
            if (_currentProvider == null)
                return new List<Wallpaper>();

            return await _currentProvider.SearchWallpapersAsync(query, page, pageSize);
        }

        public async Task<List<Wallpaper>> GetWallpapersByCategoryAsync(string category, int page = 1, int pageSize = 20)
        {
            if (_currentProvider == null)
                return new List<Wallpaper>();

            return await _currentProvider.GetWallpapersByCategoryAsync(category, page, pageSize);
        }
    }
}
