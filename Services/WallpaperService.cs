using Microsoft.Win32;
using Screens.WinUI.Models;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Screens.WinUI.Services
{
    public class WallpaperService : IWallpaperService
    {
        #region P/Invoke

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        private const int SPI_SETDESKWALLPAPER = 0x0014;
        private const int SPIF_UPDATEINIFILE   = 0x01;
        private const int SPIF_SENDCHANGE      = 0x02;

        #endregion

        async Task IWallpaperService.SetDesktopWallpaperAsync(string imagePath)
            => await SetDesktopWallpaperAsync(imagePath, WallpaperStyle.Fill);

        public async Task SetDesktopWallpaperAsync(string imagePath, WallpaperStyle style = WallpaperStyle.Fill)
        {
            await Task.Run(() =>
            {
                if (!File.Exists(imagePath))
                    throw new FileNotFoundException($"Wallpaper file not found: {imagePath}");

                ApplyStyle(style);

                var result = SystemParametersInfo(
                    SPI_SETDESKWALLPAPER, 0,
                    Path.GetFullPath(imagePath),
                    SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);

                if (result == 0)
                    throw new InvalidOperationException("Failed to set desktop wallpaper");
            });
        }

        public async Task SetLockScreenWallpaperAsync(string imagePath)
        {
            await Task.Run(() =>
            {
                if (!File.Exists(imagePath))
                    throw new FileNotFoundException($"Wallpaper file not found: {imagePath}");

                const string keyPath   = @"SOFTWARE\Microsoft\Windows\CurrentVersion\PersonalizationCSP";
                const string valueName = "LockScreenImagePath";

                using var key = Registry.CurrentUser.OpenSubKey(keyPath, true);
                key?.SetValue(valueName, imagePath);
            });
        }

        private static void ApplyStyle(WallpaperStyle style)
        {
            const string keyPath            = @"Control Panel\Desktop";
            const string styleValue         = "WallpaperStyle";
            const string tileValue          = "TileWallpaper";

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(keyPath, true);
                if (key == null) return;

                (string s, string t) reg = style switch
                {
                    WallpaperStyle.Center  => ("0",  "0"),
                    WallpaperStyle.Tile    => ("0",  "1"),
                    WallpaperStyle.Stretch => ("2",  "0"),
                    WallpaperStyle.Fit     => ("6",  "0"),
                    WallpaperStyle.Fill    => ("10", "0"),
                    WallpaperStyle.Span    => ("22", "0"),
                    _                      => ("10", "0")
                };

                key.SetValue(styleValue, reg.s);
                key.SetValue(tileValue,  reg.t);
            }
            catch
            {
                // Style application failure is non-fatal
            }
        }
    }
}
