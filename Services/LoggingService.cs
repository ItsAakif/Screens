using Windows.Storage;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Screens.WinUI.Services
{
    public class LoggingService : ILoggingService
    {
        private string? _logPath;

        public LoggingService()
        {
        }

        public async Task InitializeAsync()
        {
            var localFolder = ApplicationData.Current.LocalFolder.Path;
            _logPath = Path.Combine(localFolder, "Screens", "Logs");

            if (!Directory.Exists(_logPath))
            {
                Directory.CreateDirectory(_logPath);
            }

            await Task.CompletedTask;
        }

        private string LogPath
        {
            get
            {
                if (_logPath == null)
                {
                    // If not initialized, initialize on demand
                    var localFolder = ApplicationData.Current.LocalFolder.Path;
                    _logPath = Path.Combine(localFolder, "Screens", "Logs");

                    if (!Directory.Exists(_logPath))
                    {
                        Directory.CreateDirectory(_logPath);
                    }
                }
                return _logPath;
            }
        }

        public void LogInformation(string message)
        {
            WriteLog("INFO", message);
        }

        public void LogWarning(string message)
        {
            WriteLog("WARN", message);
        }

        public void LogError(string message, Exception? exception = null)
        {
            var error = exception != null ? $"{message}\n{exception}" : message;
            WriteLog("ERROR", error);
        }

        private void WriteLog(string level, string message)
        {
            try
            {
                var logFile = Path.Combine(LogPath, $"app_{DateTime.Now:yyyyMMdd}.log");
                var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}{Environment.NewLine}";
                File.AppendAllText(logFile, logMessage);
            }
            catch
            {
                // Ignore logging errors
            }
        }
    }
}
