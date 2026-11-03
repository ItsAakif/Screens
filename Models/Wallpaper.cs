using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Screens.WinUI.Models
{
    /// <summary>
    /// Represents a wallpaper with all its metadata
    /// </summary>
    public partial class Wallpaper : ObservableObject
    {
        [ObservableProperty]
        private string id = string.Empty;

        [ObservableProperty]
        private string title = string.Empty;

        [ObservableProperty]
        private string category = string.Empty;

        [ObservableProperty]
        private string imageUrl = string.Empty;

        [ObservableProperty]
        private string thumbnailUrl = string.Empty;

        [ObservableProperty]
        private string dimensions = string.Empty;

        /// <summary>
        /// Local file path where the image is cached
        /// </summary>
        [ObservableProperty]
        private string? localPath;

        /// <summary>
        /// The API provider this wallpaper came from
        /// </summary>
        [ObservableProperty]
        private string sourceProvider = string.Empty;

        /// <summary>
        /// Description or tags for the wallpaper
        /// </summary>
        [ObservableProperty]
        private string? description;

        /// <summary>
        /// Author/photographer name
        /// </summary>
        [ObservableProperty]
        private string? author;

        /// <summary>
        /// URL to the author's profile
        /// </summary>
        [ObservableProperty]
        private string? authorUrl;

        /// <summary>
        /// URL to the original image page
        /// </summary>
        [ObservableProperty]
        private string? sourceUrl;

        /// <summary>
        /// File size in bytes
        /// </summary>
        [ObservableProperty]
        private long fileSize;

        /// <summary>
        /// Whether this wallpaper is a favorite
        /// </summary>
        [ObservableProperty]
        private bool isFavorite;

        /// <summary>
        /// Date when this wallpaper was added to favorites
        /// </summary>
        [ObservableProperty]
        private DateTime? dateAdded;

        /// <summary>
        /// Whether this wallpaper is selected for batch operations
        /// </summary>
        [ObservableProperty]
        private bool isSelected;

        /// <summary>
        /// Whether this is a live/animated wallpaper (GIF)
        /// </summary>
        [ObservableProperty]
        private bool isLiveWallpaper;
    }
}
