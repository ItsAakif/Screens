using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Screens.WinUI.Models;

namespace Screens.WinUI.Services
{
    public class WallpaperRepository : IWallpaperRepository
    {
        private readonly IDatabaseService _databaseService;

        public WallpaperRepository(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        //  Favourites 

        public async Task<List<Wallpaper>> GetFavoritesAsync(int page = 1, int pageSize = 20)
        {
            await _databaseService.InitializeAsync();
            var offset = (page - 1) * pageSize;

            var rows = await _databaseService.QueryAsync<dynamic>(
                "SELECT * FROM Favorites ORDER BY DateAdded DESC LIMIT @Limit OFFSET @Offset",
                new { Limit = pageSize, Offset = offset });

            var result = new List<Wallpaper>();
            foreach (IDictionary<string, object?> r in rows)
                result.Add(RowToWallpaper(r));
            return result;
        }

        public async Task<bool> IsFavoriteAsync(string wallpaperId)
        {
            await _databaseService.InitializeAsync();
            var rows = await _databaseService.QueryAsync<dynamic>(
                "SELECT COUNT(*) AS Count FROM Favorites WHERE Id = @Id",
                new { Id = wallpaperId });

            if (rows.Count > 0)
            {
                var row = (IDictionary<string, object?>)(object)rows[0];
                return Convert.ToInt64(row["Count"]) > 0;
            }
            return false;
        }

        public async Task AddFavoriteAsync(Wallpaper wallpaper)
        {
            await _databaseService.InitializeAsync();
            await _databaseService.ExecuteAsync(@"
                INSERT OR REPLACE INTO Favorites
                    (Id, Title, Category, ImageUrl, ThumbnailUrl, Dimensions,
                     LocalPath, SourceProvider, Description, Author, AuthorUrl,
                     SourceUrl, FileSize, DateAdded, IsLiveWallpaper)
                VALUES
                    (@Id, @Title, @Category, @ImageUrl, @ThumbnailUrl, @Dimensions,
                     @LocalPath, @SourceProvider, @Description, @Author, @AuthorUrl,
                     @SourceUrl, @FileSize, @DateAdded, @IsLiveWallpaper)",
                new
                {
                    Id              = wallpaper.Id,
                    Title           = wallpaper.Title ?? string.Empty,
                    Category        = wallpaper.Category ?? string.Empty,
                    ImageUrl        = wallpaper.ImageUrl ?? string.Empty,
                    ThumbnailUrl    = wallpaper.ThumbnailUrl,
                    Dimensions      = wallpaper.Dimensions,
                    LocalPath       = wallpaper.LocalPath,
                    SourceProvider  = wallpaper.SourceProvider ?? "Local",
                    Description     = wallpaper.Description,
                    Author          = wallpaper.Author,
                    AuthorUrl       = wallpaper.AuthorUrl,
                    SourceUrl       = wallpaper.SourceUrl,
                    FileSize        = wallpaper.FileSize,
                    DateAdded       = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    IsLiveWallpaper = wallpaper.IsLiveWallpaper ? 1 : 0
                });
        }

        public async Task RemoveFavoriteAsync(string wallpaperId)
        {
            await _databaseService.InitializeAsync();
            await _databaseService.ExecuteAsync(
                "DELETE FROM Favorites WHERE Id = @Id",
                new { Id = wallpaperId });
        }

        //  Download History 

        public async Task AddToDownloadHistoryAsync(Wallpaper wallpaper)
        {
            await _databaseService.InitializeAsync();
            var path = wallpaper.LocalPath ?? wallpaper.ImageUrl ?? string.Empty;
            await _databaseService.ExecuteAsync(@"
                INSERT INTO DownloadHistory
                    (WallpaperId, LocalPath, Title, Category, IsLiveWallpaper, DownloadDate)
                VALUES
                    (@WallpaperId, @LocalPath, @Title, @Category, @IsLiveWallpaper, @DownloadDate)",
                new
                {
                    WallpaperId     = wallpaper.Id,
                    LocalPath       = path,
                    Title           = wallpaper.Title ?? string.Empty,
                    Category        = wallpaper.Category ?? string.Empty,
                    IsLiveWallpaper = wallpaper.IsLiveWallpaper ? 1 : 0,
                    DownloadDate    = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                });
        }

        public async Task<List<Wallpaper>> GetDownloadHistoryAsync(int page = 1, int pageSize = 50)
        {
            await _databaseService.InitializeAsync();
            var offset = (page - 1) * pageSize;

            var rows = await _databaseService.QueryAsync<dynamic>(
                "SELECT * FROM DownloadHistory ORDER BY DownloadDate DESC LIMIT @Limit OFFSET @Offset",
                new { Limit = pageSize, Offset = offset });

            var result = new List<Wallpaper>();
            foreach (IDictionary<string, object?> r in rows)
            {
                result.Add(new Wallpaper
                {
                    Id              = r["WallpaperId"]?.ToString() ?? string.Empty,
                    LocalPath       = r["LocalPath"]?.ToString(),
                    ImageUrl        = r["LocalPath"]?.ToString() ?? string.Empty,
                    ThumbnailUrl    = r["LocalPath"]?.ToString(),
                    Title           = r["Title"]?.ToString() ?? string.Empty,
                    Category        = r["Category"]?.ToString() ?? string.Empty,
                    SourceProvider  = "Local",
                    IsLiveWallpaper = Convert.ToInt64(r["IsLiveWallpaper"] ?? 0) == 1
                });
            }
            return result;
        }

        public async Task ClearDownloadHistoryAsync()
        {
            await _databaseService.InitializeAsync();
            await _databaseService.ExecuteAsync("DELETE FROM DownloadHistory");
        }

        public async Task RemoveFromDownloadHistoryAsync(string wallpaperId)
        {
            await _databaseService.InitializeAsync();
            await _databaseService.ExecuteAsync(
                "DELETE FROM DownloadHistory WHERE WallpaperId = @Id",
                new { Id = wallpaperId });
        }

        //  Helpers 

        private static Wallpaper RowToWallpaper(IDictionary<string, object?> r) => new()
        {
            Id              = r["Id"]?.ToString() ?? string.Empty,
            Title           = r["Title"]?.ToString() ?? string.Empty,
            Category        = r["Category"]?.ToString() ?? string.Empty,
            ImageUrl        = r["ImageUrl"]?.ToString() ?? string.Empty,
            ThumbnailUrl    = r["ThumbnailUrl"]?.ToString(),
            Dimensions      = r["Dimensions"]?.ToString(),
            LocalPath       = r["LocalPath"]?.ToString(),
            SourceProvider  = r["SourceProvider"]?.ToString() ?? "Local",
            Description     = r["Description"]?.ToString(),
            Author          = r["Author"]?.ToString(),
            AuthorUrl       = r["AuthorUrl"]?.ToString(),
            SourceUrl       = r["SourceUrl"]?.ToString(),
            FileSize        = Convert.ToInt64(r["FileSize"] ?? 0),
            IsLiveWallpaper = Convert.ToInt64(r["IsLiveWallpaper"] ?? 0) == 1,
            IsFavorite      = true
        };
    }
}
