using Screens.WinUI.Models;
using System;
using System.Threading.Tasks;

namespace Screens.WinUI.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly IDatabaseService _databaseService;

        public SettingsService(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<AppSettings> LoadSettingsAsync()
        {
            await _databaseService.InitializeAsync();

            var result = new AppSettings();
            try
            {
                result.SelectedProvider = await Get("SelectedProvider") ?? "Local";

                var themeStr = await Get("Theme");
                result.Theme = Enum.TryParse<AppTheme>(themeStr, out var t) ? t : AppTheme.Dark;

                var gridStr = await Get("GridSize");
                result.GridSize = Enum.TryParse<GridSize>(gridStr, out var g) ? g : GridSize.Medium;

                var styleStr = await Get("WallpaperStyle");
                result.WallpaperStyle = Enum.TryParse<WallpaperStyle>(styleStr, out var s) ? s : WallpaperStyle.Fill;

                var cacheMB = await Get("CacheSizeLimitMB");
                result.CacheSizeLimitMB = long.TryParse(cacheMB, out var c) ? c : 1000;

                var startMin = await Get("StartMinimized");
                result.StartMinimized = startMin == "true";

                var minTray = await Get("MinimizeToTray");
                result.MinimizeToTray = minTray == "true";

                var startWin = await Get("StartWithWindows");
                result.StartWithWindows = startWin == "true";
            }
            catch
            {
                // Return defaults on any error
            }
            return result;
        }

        public async Task SaveSettingsAsync(AppSettings settings)
        {
            await _databaseService.InitializeAsync();
            await Set("SelectedProvider",   settings.SelectedProvider);
            await Set("Theme",              settings.Theme.ToString());
            await Set("GridSize",           settings.GridSize.ToString());
            await Set("WallpaperStyle",     settings.WallpaperStyle.ToString());
            await Set("CacheSizeLimitMB",   settings.CacheSizeLimitMB.ToString());
            await Set("StartMinimized",     settings.StartMinimized ? "true" : "false");
            await Set("MinimizeToTray",     settings.MinimizeToTray ? "true" : "false");
            await Set("StartWithWindows",   settings.StartWithWindows ? "true" : "false");
        }

        private async Task<string?> Get(string key)
        {
            var rows = await _databaseService.QueryAsync<dynamic>(
                "SELECT Value FROM Settings WHERE Key = @Key",
                new { Key = key });
            if (rows.Count > 0)
            {
                var row = (IDictionary<string, object?>)(object)rows[0];
                return row["Value"]?.ToString();
            }
            return null;
        }

        private async Task Set(string key, string value) =>
            await _databaseService.ExecuteAsync(
                "INSERT OR REPLACE INTO Settings (Key, Value) VALUES (@Key, @Value)",
                new { Key = key, Value = value });
    }
}
