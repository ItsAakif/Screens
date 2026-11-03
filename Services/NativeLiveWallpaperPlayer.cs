using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Screens.WinUI.Services
{
    public class NativeLiveWallpaperPlayer : Window
    {
        private MediaElement? _mediaPlayer;
        private Image? _gifImage;

        #region P/Invoke

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        private const uint WM_COMMAND  = 0x0111;
        private const uint SWP_NOZORDER   = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        #endregion

        public NativeLiveWallpaperPlayer()
        {
            WindowStyle         = WindowStyle.None;
            AllowsTransparency  = true;
            Background          = new SolidColorBrush(Colors.Black);
            WindowState         = WindowState.Maximized;
            ShowInTaskbar       = false;
            Topmost             = false;
            ResizeMode          = ResizeMode.NoResize;
            Loaded             += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            ParentToDesktop(hwnd);
        }

        private void ParentToDesktop(IntPtr hwnd)
        {
            try
            {
                var progman = FindWindow("Progman", null);
                SendMessage(progman, WM_COMMAND, new IntPtr(0x052C), IntPtr.Zero);

                IntPtr workerw = IntPtr.Zero;
                EnumWindows((tophandle, _) =>
                {
                    var p = FindWindowEx(tophandle, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (p != IntPtr.Zero)
                        workerw = FindWindowEx(IntPtr.Zero, tophandle, "WorkerW", null);
                    return true;
                }, IntPtr.Zero);

                if (workerw != IntPtr.Zero)
                {
                    SetParent(hwnd, workerw);
                    SetWindowPos(hwnd, IntPtr.Zero, 0, 0,
                        (int)SystemParameters.PrimaryScreenWidth,
                        (int)SystemParameters.PrimaryScreenHeight,
                        SWP_NOZORDER | SWP_NOACTIVATE | SWP_SHOWWINDOW);
                }
            }
            catch { /* Desktop parenting failure is non-fatal */ }
        }

        public void PlayVideo(string filePath)
        {
            Content = null;
            _gifImage = null;

            _mediaPlayer = new MediaElement
            {
                Source           = new Uri(filePath, UriKind.Absolute),
                LoadedBehavior   = MediaState.Manual,
                UnloadedBehavior = MediaState.Manual,
                Stretch          = Stretch.UniformToFill,
                Volume           = 0
            };

            _mediaPlayer.MediaEnded += (s, e) =>
            {
                _mediaPlayer!.Position = TimeSpan.Zero;
                _mediaPlayer.Play();
            };
            _mediaPlayer.MediaOpened += (s, e) => _mediaPlayer?.Play();

            Content = _mediaPlayer;
        }

        public void PlayGif(string filePath)
        {
            Content = null;
            _mediaPlayer = null;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource      = new Uri(filePath, UriKind.Absolute);
            bitmap.CacheOption    = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions  = BitmapCreateOptions.PreservePixelFormat;
            bitmap.EndInit();

            _gifImage = new Image { Source = bitmap, Stretch = Stretch.UniformToFill };
            Content = _gifImage;
        }

        public void PauseWallpaper()
        {
            try { _mediaPlayer?.Pause(); }
            catch { /* Pause failure is non-fatal */ }
        }

        public void ResumeWallpaper()
        {
            try { _mediaPlayer?.Play(); }
            catch { /* Resume failure is non-fatal */ }
        }

        public void StopWallpaper()
        {
            try
            {
                if (_mediaPlayer != null)
                {
                    _mediaPlayer.Stop();
                    _mediaPlayer.Close();
                    _mediaPlayer = null;
                }
                _gifImage = null;
                Close();
            }
            catch { /* Stop failure is non-fatal */ }
        }

        protected override void OnClosed(EventArgs e)
        {
            _mediaPlayer?.Stop();
            _mediaPlayer = null;
            _gifImage    = null;
            base.OnClosed(e);
        }
    }
}
