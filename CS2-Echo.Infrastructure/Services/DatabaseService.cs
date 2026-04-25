using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using Microsoft.Data.Sqlite;

namespace CS2_Echo.Infrastructure.Services;

public class DatabaseService : IDisposable
{
    private readonly string _connectionString;
    private readonly string _dbFilePath;
    private readonly ConfigurationService _configService;

    private readonly SqliteConnection _connection;
    private readonly SemaphoreSlim _dbLock = new SemaphoreSlim(1, 1);

    public int MinMessageSize => _configService.Current.MinMessageSize;
    public bool EnablePlayerStats => _configService.Current.EnablePlayerStats;

    public DatabaseService(ConfigurationService configService)
    {

        _configService = configService;

        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string folder = Path.Combine(appData, "CS2_Echo");
        Directory.CreateDirectory(folder);

        _dbFilePath = Path.Combine(folder, "cs2_echo.db");
        _connectionString = $"Data Source={_dbFilePath}";

        _connection = new SqliteConnection(_connectionString);
        
    }

    public async Task InitializeAsync()
    {
        await _dbLock.WaitAsync();

        try
        {
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }

            var cmd = _connection.CreateCommand();
            cmd.CommandText =
            @"
                    CREATE TABLE IF NOT EXISTS Translations (
                        OriginalText TEXT NOT NULL,
                        TranslatedText TEXT NOT NULL,
                        SourceLang TEXT NOT NULL,
                        TargetLang TEXT NOT NULL,
                        LastAccessed DATETIME NOT NULL,
                        PRIMARY KEY (OriginalText, TargetLang)
                    );

                    CREATE TABLE IF NOT EXISTS PlayerStats (
                        PlayerName TEXT NOT NULL,
                        Language TEXT NOT NULL,
                        MessageCount INTEGER DEFAULT 1,
                        LastActive DATETIME NOT NULL,
                        PRIMARY KEY (PlayerName, Language)
                    );
                    CREATE TABLE IF NOT EXISTS IgnoredPlayers (
                        PlayerName TEXT PRIMARY KEY
                    );
                ";
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task<string?> GetCachedTranslationAsync(string originalText, string targetLang)
    {
        if (string.IsNullOrEmpty(originalText) || originalText.Length < MinMessageSize)
            return null;

        await _dbLock.WaitAsync();
        try
        {
            var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT TranslatedText FROM Translations WHERE OriginalText = $originalText AND TargetLang = $targetLang";
            cmd.Parameters.AddWithValue("$originalText", originalText);
            cmd.Parameters.AddWithValue("$targetLang", targetLang);

            var result = await cmd.ExecuteScalarAsync() as string;

            if (result != null)
            {
                await UpdateLastAccessedAsync(originalText, targetLang);
            }

            return result;
            //return originalText; // debug translation skip
        }
        finally
        {
            _dbLock.Release();
        }
        
    }

    public async Task SaveTranslationAsync(string originalText, string translatedText, string sourceLang, string targetLang)
    {
        if (string.IsNullOrWhiteSpace(originalText) || originalText.Length < MinMessageSize)
            return;

        await _dbLock.WaitAsync();
        try
        {
            var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                INSERT OR REPLACE INTO Translations (OriginalText, TranslatedText, SourceLang, TargetLang, LastAccessed)
                VALUES ($originalText, $translatedText, $sourceLang, $targetLang, $lastAccessed)";
            cmd.Parameters.AddWithValue("$originalText", originalText);
            cmd.Parameters.AddWithValue("$translatedText", translatedText);
            cmd.Parameters.AddWithValue("$sourceLang", sourceLang);
            cmd.Parameters.AddWithValue("$targetLang", targetLang);
            cmd.Parameters.AddWithValue("$lastAccessed", DateTime.UtcNow);

            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _dbLock.Release();
        }
    }

    /// <summary>
    /// Updates the last accessed time for a cached translation.
    /// Note: The caller MUST hold the _dbLock before calling this method.
    /// </summary>
    private async Task UpdateLastAccessedAsync(string originalText, string targetLang)
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            UPDATE Translations 
            SET LastAccessed = $lastAccessed 
            WHERE OriginalText = $originalText AND TargetLang = $targetLang";
        cmd.Parameters.AddWithValue("$lastAccessed", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("$originalText", originalText);
        cmd.Parameters.AddWithValue("$targetLang", targetLang);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task LogPlayerLanguageAsync(string playerName, string detectedLanguage)
    {
        if (!EnablePlayerStats || string.IsNullOrWhiteSpace(playerName) || detectedLanguage == "unknown")
            return;

        await _dbLock.WaitAsync();
        try
        {
            var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO PlayerStats (PlayerName, Language, MessageCount, LastActive)
                VALUES ($playerName, $language, 1, $lastActive)
                ON CONFLICT(PlayerName, Language) DO UPDATE SET 
                    MessageCount = MessageCount + 1,
                    LastActive = excluded.LastActive";

            cmd.Parameters.AddWithValue("$playerName", playerName);
            cmd.Parameters.AddWithValue("$language", detectedLanguage);
            cmd.Parameters.AddWithValue("$lastActive", DateTime.UtcNow);

            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public double GetDatabaseSizeMB()
    {
        var fileInfo = new FileInfo(_dbFilePath);
        if (fileInfo.Exists)
        {
            return fileInfo.Length / (1024.0 * 1024.0);
        }

        return 0;
    }

    public async Task ClearCacheAsync()
    {
        await _dbLock.WaitAsync();
        try
        {
            var cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Translations;";
            await cmd.ExecuteNonQueryAsync();

            var vacuumCmd = _connection.CreateCommand();
            vacuumCmd.CommandText = "VACUUM;";
            await vacuumCmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task ClearPlayerStatsAsync()
    {
        await _dbLock.WaitAsync();
        try
        {
            var cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM PlayerStats;";
            await cmd.ExecuteNonQueryAsync();

            var vacuumCmd = _connection.CreateCommand();
            vacuumCmd.CommandText = "VACUUM;";
            await vacuumCmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task EnforceCacheLimitAsync(int maxRows = 1000)
    {
        await _dbLock.WaitAsync();
        try
        {
            var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                DELETE FROM Translations 
                WHERE rowid NOT IN (
                    SELECT rowid FROM Translations
                    ORDER BY LastAccessed DESC
                    LIMIT $maxRows
                );";
            cmd.Parameters.AddWithValue("$maxRows", maxRows);
            await cmd.ExecuteNonQueryAsync();

            var vacuumCmd = _connection.CreateCommand();
            vacuumCmd.CommandText = "VACUUM;";
            await vacuumCmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task AddIgnoredPlayerAsync(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName)) return;

        await _dbLock.WaitAsync();
        try
        {
            var cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT OR IGNORE INTO IgnoredPlayers (PlayerName) VALUES ($name)";
            cmd.Parameters.AddWithValue("$name", playerName.Trim());
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task RemoveIgnoredPlayerAsync(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName)) return;

        await _dbLock.WaitAsync();
        try
        {
            var cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM IgnoredPlayers WHERE PlayerName = $name";
            cmd.Parameters.AddWithValue("$name", playerName.Trim());
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task<HashSet<string>> LoadIgnoredPlayersAsync()
    {
        var ignored = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await _dbLock.WaitAsync();
        try
        {
            var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT PlayerName FROM IgnoredPlayers";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                ignored.Add(reader.GetString(0));
            }
        }
        finally
        {
            _dbLock.Release();
        }

        return ignored;
    }


    public async Task<List<(string PlayerName, string Language, int MessageCount, DateTime LastActive)>> GetTopPlayerStatsAsync(int limit = 100)
    {
        var results = new List<(string, string, int, DateTime)>();

        await _dbLock.WaitAsync();
        try
        {
            var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT PlayerName, Language, MessageCount, LastActive FROM PlayerStats ORDER BY MessageCount DESC LIMIT $limit";
            cmd.Parameters.AddWithValue("$limit", limit);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add((
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetInt32(2),
                    reader.GetDateTime(3)
                ));
            }
        }
        finally
        {
            _dbLock.Release();
        }

        return results;
    }


    public async Task<string> GetTopLanguageForPlayerAsync(string playerName)
    {
        string topLanguage = "unknown";
        if (string.IsNullOrWhiteSpace(playerName)) return topLanguage;

        await _dbLock.WaitAsync();
        try
        {
            var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                SELECT Language 
                FROM PlayerStats 
                WHERE PlayerName = $playerName 
                ORDER BY MessageCount DESC 
                LIMIT 1";
            cmd.Parameters.AddWithValue("$playerName", playerName.Trim());

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                topLanguage = reader.GetString(0);
            }
        }
        finally
        {
            _dbLock.Release();
        }

        return topLanguage;
    }

    public void Dispose()
    {
        _connection?.Dispose();
        _dbLock?.Dispose();
    }
}