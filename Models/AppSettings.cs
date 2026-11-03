using System;
using System.Collections.Generic;

namespace Screens.WinUI.Models
{
    /// <summary>
    /// Application settings model
    /// </summary>
    public class AppSettings
    {
        // Wallpaper Provider Settings
        public string SelectedProvider { get; set; } = "Local";

        // Image Quality Settings
        public ImageQuality ImageQuality { get; set; } = ImageQuality.High;
        public string DownloadLocation { get; set; } = string.Empty;

        // Cache Settings
        public long CacheSizeLimitMB { get; set; } = 500;
        public int CacheExpirationDays { get; set; } = 7;

        // Appearance Settings
        public AppTheme Theme { get; set; } = AppTheme.Dark;
        public string AccentColor { get; set; } = "#60CDFF";
        public GridSize GridSize { get; set; } = GridSize.Medium;

        // Update Settings
        public bool AutoUpdateEnabled { get; set; } = true;
        public string LastUpdateCheck { get; set; } = string.Empty;

        // Advanced Settings
        public bool EnableTelemetry { get; set; } = false;
        public bool StartMinimized { get; set; } = false;
        public bool MinimizeToTray { get; set; } = false;
        public bool StartWithWindows { get; set; } = false;

        // Wallpaper Style
        public WallpaperStyle WallpaperStyle { get; set; } = WallpaperStyle.Fill;
    }

    /// <summary>
    /// Wallpaper display style options
    /// </summary>
    public enum WallpaperStyle
    {
        Center,
        Tile,
        Stretch,
        Fit,
        Fill,
        Span
    }

    /// <summary>
    /// Image quality options
    /// </summary>
    public enum ImageQuality
    {
        Low,
        Medium,
        High,
        Original
    }

    /// <summary>
    /// Application theme options
    /// </summary>
    public enum AppTheme
    {
        Light,
        Dark,
        System
    }

    /// <summary>
    /// Grid size options for wallpaper display
    /// </summary>
    public enum GridSize
    {
        Small,
        Medium,
        Large
    }

    /// <summary>
    /// Extension methods for AppSettings
    /// </summary>
    public static class AppSettingsExtensions
    {
        public static int GetGridSizeWidth(this GridSize gridSize)
        {
            return gridSize switch
            {
                GridSize.Small => 200,
                GridSize.Medium => 300,
                GridSize.Large => 400,
                _ => 300
            };
        }

        public static int GetGridSizeHeight(this GridSize gridSize)
        {
            return gridSize switch
            {
                GridSize.Small => 150,
                GridSize.Medium => 200,
                GridSize.Large => 300,
                _ => 200
            };
        }
    }
}
