using System;
using System.IO;
using System.Threading.Tasks;

namespace Screens.WinUI.Services
{
    public class LiveWallpaperService : ILiveWallpaperService
    {
        private readonly ILoggingService _loggingService;
        private NativeLiveWallpaperPlayer? _wallpaperPlayer;
        private bool _isRunning;
        private bool _isPaused;

        public event EventHandler<bool>? IsRunningChanged;

        public bool IsRunning  => _isRunning;
        public bool IsPaused   => _isPaused;

        public LiveWallpaperService(ILoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        public async Task SetLiveWallpaperAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Live wallpaper file not found: {filePath}");

            await StopLiveWallpaperAsync();

            _isPaused = false;
            var tcs = new TaskCompletionSource<bool>();
            var ext = Path.GetExtension(filePath).ToLower();

            var thread = new System.Threading.Thread(() =>
            {
                try
                {
                    _wallpaperPlayer = new NativeLiveWallpaperPlayer();
                    _wallpaperPlayer.Show();

                    System.Threading.Thread.Sleep(500);

                    if (ext == ".gif")
                        _wallpaperPlayer.PlayGif(filePath);
                    else if (ext is ".mp4" or ".webm" or ".mkv" or ".avi" or ".mov" or ".m4v" or ".wmv")
                        _wallpaperPlayer.PlayVideo(filePath);
                    else
                        throw new NotSupportedException($"File format not supported: {ext}");

                    _isRunning = true;
                    IsRunningChanged?.Invoke(this, true);
                    tcs.TrySetResult(true);

                    System.Windows.Threading.Dispatcher.Run();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });

            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();

            await Task.Delay(1000);
            _loggingService.LogInformation($"Live wallpaper set: {filePath}");
        }

        public Task StopLiveWallpaperAsync()
        {
            try
            {
                if (_wallpaperPlayer != null)
                {
                    _wallpaperPlayer.Dispatcher.Invoke(() =>
                    {
                        _wallpaperPlayer?.StopWallpaper();
                        System.Windows.Threading.Dispatcher.ExitAllFrames();
                    });
                    _wallpaperPlayer = null;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error stopping live wallpaper", ex);
            }

            _isRunning = false;
            _isPaused  = false;
            IsRunningChanged?.Invoke(this, false);
            return Task.CompletedTask;
        }

        public void PauseLiveWallpaper()
        {
            if (!_isRunning || _isPaused || _wallpaperPlayer == null) return;
            try
            {
                _wallpaperPlayer.Dispatcher.Invoke(() => _wallpaperPlayer.PauseWallpaper());
                _isPaused = true;
                _loggingService.LogInformation("Live wallpaper paused");
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error pausing live wallpaper", ex);
            }
        }

        public void ResumeLiveWallpaper()
        {
            if (!_isRunning || !_isPaused || _wallpaperPlayer == null) return;
            try
            {
                _wallpaperPlayer.Dispatcher.Invoke(() => _wallpaperPlayer.ResumeWallpaper());
                _isPaused = false;
                _loggingService.LogInformation("Live wallpaper resumed");
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error resuming live wallpaper", ex);
            }
        }
    }
}
