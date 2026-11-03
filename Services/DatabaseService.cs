using Microsoft.Data.Sqlite;
using Windows.Storage;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Screens.WinUI.Services
{
    public class DatabaseService : IDatabaseService
    {
        private string? _dbPath;
        private SqliteConnection? _connection;
        private bool _initialized = false;

        public async Task InitializeAsync()
        {
            if (_initialized) return;
            _initialized = true;

            var localFolder = ApplicationData.Current.LocalFolder.Path;
            _dbPath = Path.Combine(localFolder, "Screens", "screens.db");

            var directory = Path.GetDirectoryName(_dbPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            _connection = new SqliteConnection($"Data Source={_dbPath}");
            await _connection.OpenAsync();

            await ExecuteAsync("PRAGMA journal_mode=WAL");
            await ExecuteAsync("PRAGMA foreign_keys = ON");
            await CreateTablesAsync();
        }

        private async Task CreateTablesAsync()
        {
            await ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS Favorites (
                    Id          TEXT    PRIMARY KEY,
                    Title       TEXT    NOT NULL DEFAULT '',
                    Category    TEXT    NOT NULL DEFAULT '',
                    ImageUrl    TEXT    NOT NULL DEFAULT '',
                    ThumbnailUrl TEXT,
                    Dimensions  TEXT,
                    LocalPath   TEXT,
                    SourceProvider TEXT NOT NULL DEFAULT 'Local',
                    Description TEXT,
                    Author      TEXT,
                    AuthorUrl   TEXT,
                    SourceUrl   TEXT,
                    FileSize    INTEGER DEFAULT 0,
                    DateAdded   INTEGER NOT NULL,
                    IsLiveWallpaper INTEGER NOT NULL DEFAULT 0
                )");

            await ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS DownloadHistory (
                    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    WallpaperId TEXT    NOT NULL,
                    LocalPath   TEXT    NOT NULL DEFAULT '',
                    Title       TEXT    NOT NULL DEFAULT '',
                    Category    TEXT    NOT NULL DEFAULT '',
                    IsLiveWallpaper INTEGER NOT NULL DEFAULT 0,
                    DownloadDate INTEGER NOT NULL
                )");

            await ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS Settings (
                    Key   TEXT PRIMARY KEY,
                    Value TEXT NOT NULL DEFAULT ''
                )");

            // Add new DownloadHistory columns gracefully if upgrading from old schema
            try { await ExecuteAsync("ALTER TABLE DownloadHistory ADD COLUMN Title TEXT NOT NULL DEFAULT ''"); } catch { }
            try { await ExecuteAsync("ALTER TABLE DownloadHistory ADD COLUMN Category TEXT NOT NULL DEFAULT ''"); } catch { }
            try { await ExecuteAsync("ALTER TABLE DownloadHistory ADD COLUMN IsLiveWallpaper INTEGER NOT NULL DEFAULT 0"); } catch { }
        }

        public async Task<List<T>> QueryAsync<T>(string sql, object? parameters = null)
        {
            var results = new List<T>();
            if (_connection == null) return results;

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameters(command, parameters);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var expando = new System.Dynamic.ExpandoObject() as IDictionary<string, object?>;
                for (int i = 0; i < reader.FieldCount; i++)
                    expando[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                results.Add((T)(object)expando);
            }

            return results;
        }

        public async Task ExecuteAsync(string sql, object? parameters = null)
        {
            if (_connection == null) return;

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameters(command, parameters);
            await command.ExecuteNonQueryAsync();
        }

        private static void AddParameters(SqliteCommand command, object? parameters)
        {
            if (parameters == null) return;
            foreach (var prop in parameters.GetType().GetProperties())
                command.Parameters.AddWithValue($"@{prop.Name}", prop.GetValue(parameters) ?? DBNull.Value);
        }

        public void Dispose() => _connection?.Dispose();
    }
}
