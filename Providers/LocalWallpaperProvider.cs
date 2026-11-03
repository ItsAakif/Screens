using Screens.WinUI.Models;

namespace Screens.WinUI.Providers
{
    /// <summary>
    /// Provider for local wallpapers from the Wallpaper folder (WinUI 3 version)
    /// </summary>
    public class LocalWallpaperProvider : IWallpaperProvider
    {
        private readonly string _wallpaperBasePath;
        private readonly Dictionary<string, List<Wallpaper>> _categoryCache;
        private readonly List<string> _allCategories;

        public string Name => "Local";
        public string DisplayName => "Local Wallpapers";
        public bool RequiresApiKey => false;

        public LocalWallpaperProvider(string wallpaperBasePath)
        {
            _wallpaperBasePath = wallpaperBasePath;
            _categoryCache = new Dictionary<string, List<Wallpaper>>();
            _allCategories = new List<string>();

            // Initialize categories on startup
            LoadCategories();
        }

        public void SetApiKey(string apiKey)
        {
            // Local provider doesn't need API key
        }

        public System.Threading.Tasks.Task<bool> ValidateApiKeyAsync()
        {
            return System.Threading.Tasks.Task.FromResult(true);
        }

        /// <summary>
        /// Get all available categories (folder names)
        /// </summary>
        public List<string> GetCategories()
        {
            return new List<string>(_allCategories);
        }

        /// <summary>
        /// Load all category folders
        /// </summary>
        private void LoadCategories()
        {
            try
            {
                if (!System.IO.Directory.Exists(_wallpaperBasePath))
                {
                    return;
                }

                _allCategories.Clear();
                var directories = System.IO.Directory.GetDirectories(_wallpaperBasePath);

                foreach (var dir in directories)
                {
                    var categoryName = System.IO.Path.GetFileName(dir);
                    _allCategories.Add(categoryName);
                }

                // Sort alphabetically
                _allCategories.Sort();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading categories: {ex.Message}");
            }
        }

        public async System.Threading.Tasks.Task<List<string>> GetCategoriesAsync()
        {
            return await System.Threading.Tasks.Task.FromResult(GetCategories());
        }

        /// <summary>
        /// Get wallpapers from a specific category (folder)
        /// </summary>
        public async System.Threading.Tasks.Task<List<Wallpaper>> GetWallpapersByCategoryAsync(string category, int page = 1, int pageSize = 20)
        {
            return await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var categoryPath = System.IO.Path.Combine(_wallpaperBasePath, category);

                    if (!System.IO.Directory.Exists(categoryPath))
                    {
                        return new List<Wallpaper>();
                    }

                    // Return cached if available
                    if (_categoryCache.ContainsKey(category))
                    {
                        return _categoryCache[category];
                    }

                    var wallpapers = new List<Wallpaper>();
                    var files = System.IO.Directory.GetFiles(categoryPath, "*.*", System.IO.SearchOption.TopDirectoryOnly)
                        .Where(f => IsImageFile(f))
                        .OrderBy(f => f)
                        .ToList();

                    foreach (var file in files)
                    {
                        var fileName = System.IO.Path.GetFileNameWithoutExtension(file);
                        var extension = System.IO.Path.GetExtension(file).ToLower();
                        var isGif = extension == ".gif";

                        // For WinUI 3, we'll get dimensions differently (simplified for now)
                        var fileInfo = new System.IO.FileInfo(file);
                        string dimensions = FormatFileSize(fileInfo.Length);

                        var wallpaper = new Wallpaper
                        {
                            Id = GenerateId(file),
                            Title = fileName,
                            Category = category,
                            ImageUrl = file, // Use local path directly
                            ThumbnailUrl = file,
                            Dimensions = dimensions,
                            LocalPath = file,
                            SourceProvider = Name,
                            Description = $"{category} wallpaper",
                            FileSize = fileInfo.Length,
                            IsLiveWallpaper = isGif
                        };

                        wallpapers.Add(wallpaper);
                    }

                    // Cache the results
                    _categoryCache[category] = wallpapers;

                    return wallpapers;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading wallpapers from {category}: {ex.Message}");
                    return new List<Wallpaper>();
                }
            });
        }

        /// <summary>
        /// Search wallpapers across all categories
        /// </summary>
        public async System.Threading.Tasks.Task<List<Wallpaper>> SearchWallpapersAsync(string query, int page = 1, int pageSize = 20)
        {
            return await System.Threading.Tasks.Task.Run(async () =>
            {
                var allWallpapers = new List<Wallpaper>();

                foreach (var category in _allCategories)
                {
                    var wallpapers = await GetWallpapersByCategoryAsync(category);
                    allWallpapers.AddRange(wallpapers);
                }

                // Filter by query
                var filtered = allWallpapers
                    .Where(w => w.Title.Contains(query, System.StringComparison.OrdinalIgnoreCase) ||
                               w.Category.Contains(query, System.StringComparison.OrdinalIgnoreCase))
                    .ToList();

                return filtered;
            });
        }

        /// <summary>
        /// Get featured wallpapers (random selection from all categories)
        /// </summary>
        public async System.Threading.Tasks.Task<List<Wallpaper>> GetFeaturedWallpapersAsync(int page = 1, int pageSize = 20)
        {
            return await System.Threading.Tasks.Task.Run(async () =>
            {
                var allWallpapers = new List<Wallpaper>();
                var random = new Random();

                // Get wallpapers from random categories until we have enough
                var shuffledCategories = _allCategories.OrderBy(x => random.Next()).ToList();

                foreach (var category in shuffledCategories)
                {
                    if (allWallpapers.Count >= pageSize)
                        break;

                    var wallpapers = await GetWallpapersByCategoryAsync(category);
                    allWallpapers.AddRange(wallpapers.Take(3)); // Take 3 from each category
                }

                return allWallpapers.Take(pageSize).ToList();
            });
        }

        /// <summary>
        /// Download wallpaper (for local files, just return the path)
        /// </summary>
        public async System.Threading.Tasks.Task<string> DownloadWallpaperAsync(Wallpaper wallpaper, string destinationPath)
        {
            // For local files, copy to destination
            try
            {
                if (System.IO.File.Exists(wallpaper.LocalPath))
                {
                    System.IO.File.Copy(wallpaper.LocalPath, destinationPath, true);
                    return destinationPath;
                }
                return wallpaper.LocalPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to copy wallpaper: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Clear the category cache
        /// </summary>
        public void ClearCache()
        {
            _categoryCache.Clear();
        }

        /// <summary>
        /// Reload categories from disk
        /// </summary>
        public void RefreshCategories()
        {
            ClearCache();
            LoadCategories();
        }

        private bool IsImageFile(string filePath)
        {
            var extension = System.IO.Path.GetExtension(filePath).ToLower();
            return extension == ".jpg" || extension == ".jpeg" ||
                   extension == ".png" || extension == ".gif" ||
                   extension == ".bmp" || extension == ".webp";
        }

        private string GenerateId(string filePath)
        {
            // Create a unique ID based on file path
            var hash = filePath.GetHashCode();
            return $"local_{Math.Abs(hash)}";
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
