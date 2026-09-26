using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace ProjectVoiceLink
{
    public record DatabaseStats(long TotalVoices, long TotalUsers, long TotalLogs, long DatabaseSizeBytes);

    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string? connectionString = null)
        {
            _connectionString = connectionString ?? Configuration.DatabasePath;
        }

        public SqliteConnection CreateConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            var builder = new SqliteConnectionStringBuilder(_connectionString);
            if (!string.IsNullOrEmpty(builder.DataSource) && builder.DataSource != ":memory:")
            {
                var dir = Path.GetDirectoryName(builder.DataSource);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }

            await using var conn = CreateConnection();
            await conn.OpenAsync(cancellationToken);

            // Configure SQLite for high concurrency and performance
            const string pragmaSql = @"
                PRAGMA journal_mode = WAL;
                PRAGMA synchronous = NORMAL;
                PRAGMA busy_timeout = 5000;
                PRAGMA cache_size = -64000;
                PRAGMA foreign_keys = ON;
            ";
            using (var pragmaCmd = new SqliteCommand(pragmaSql, conn))
            {
                await pragmaCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            const string schemaSql = @"
                CREATE TABLE IF NOT EXISTS Voices (
                    KEYID INTEGER PRIMARY KEY AUTOINCREMENT,
                    first_name TEXT,
                    last_name TEXT,
                    username TEXT,
                    id TEXT,
                    language_code TEXT,
                    date TEXT,
                    is_bot TEXT,
                    message_id TEXT,
                    audio_file TEXT,
                    filehash TEXT
                );

                CREATE TABLE IF NOT EXISTS Users (
                    KEYID INTEGER PRIMARY KEY AUTOINCREMENT,
                    first_name TEXT,
                    last_name TEXT,
                    username TEXT,
                    id TEXT,
                    language_code TEXT,
                    preferred_language TEXT,
                    date TEXT,
                    is_bot TEXT,
                    message_id TEXT,
                    text TEXT
                );

                CREATE TABLE IF NOT EXISTS Logs (
                    KEYID INTEGER PRIMARY KEY AUTOINCREMENT,
                    first_name TEXT,
                    last_name TEXT,
                    username TEXT,
                    id TEXT,
                    language_code TEXT,
                    date TEXT,
                    is_bot TEXT,
                    message_id TEXT,
                    text TEXT
                );

                CREATE INDEX IF NOT EXISTS idx_voices_filehash ON Voices(filehash);
                CREATE INDEX IF NOT EXISTS idx_voices_id ON Voices(id);
                CREATE INDEX IF NOT EXISTS idx_voices_date ON Voices(date);
                CREATE INDEX IF NOT EXISTS idx_users_id ON Users(id);
                CREATE INDEX IF NOT EXISTS idx_logs_date ON Logs(date);
            ";
            using (var schemaCmd = new SqliteCommand(schemaSql, conn))
            {
                await schemaCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            // Ensure directory for audio bottles exists
            Directory.CreateDirectory(Configuration.AudioStoragePath);
        }

        public async Task<bool> HasDuplicateHashAsync(string filehash, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filehash) || filehash == "error")
            {
                return false;
            }

            await using var conn = CreateConnection();
            await conn.OpenAsync(cancellationToken);

            const string sql = "SELECT 1 FROM Voices WHERE filehash = @hash LIMIT 1;";
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@hash", filehash);
            var res = await cmd.ExecuteScalarAsync(cancellationToken);
            return res != null;
        }

        public async Task InsertVoiceAsync(UserClass user, string audioFilePath, string filehash, CancellationToken cancellationToken = default)
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync(cancellationToken);

            const string sql = @"
                INSERT INTO Voices (first_name, last_name, username, id, language_code, date, is_bot, message_id, audio_file, filehash)
                VALUES (@first_name, @last_name, @username, @id, @language_code, @date, @is_bot, @message_id, @audio_file, @filehash);";
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@first_name", user.first_name);
            cmd.Parameters.AddWithValue("@last_name", user.last_name);
            cmd.Parameters.AddWithValue("@username", user.username);
            cmd.Parameters.AddWithValue("@id", user.id);
            cmd.Parameters.AddWithValue("@language_code", user.languagecode);
            cmd.Parameters.AddWithValue("@date", user.date);
            cmd.Parameters.AddWithValue("@is_bot", user.isbot);
            cmd.Parameters.AddWithValue("@message_id", user.messageid);
            cmd.Parameters.AddWithValue("@audio_file", audioFilePath);
            cmd.Parameters.AddWithValue("@filehash", filehash);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<string?> GetRandomVoiceAsync(string currentHash, string userId, CancellationToken cancellationToken = default)
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync(cancellationToken);

            // 1. Try finding a voice message from someone else
            const string sqlFromOthers = @"
                SELECT audio_file FROM Voices 
                WHERE filehash != @currentHash AND id != @userId 
                ORDER BY RANDOM() LIMIT 10;";

            using (var cmd = new SqliteCommand(sqlFromOthers, conn))
            {
                cmd.Parameters.AddWithValue("@currentHash", currentHash);
                cmd.Parameters.AddWithValue("@userId", userId);

                using var rdr = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await rdr.ReadAsync(cancellationToken))
                {
                    var path = rdr.GetString(0);
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }
            }

            // 2. Fallback: if only user's messages or same hash messages exist
            const string sqlAny = @"
                SELECT audio_file FROM Voices 
                ORDER BY RANDOM() LIMIT 5;";

            using (var cmd = new SqliteCommand(sqlAny, conn))
            {
                using var rdr = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await rdr.ReadAsync(cancellationToken))
                {
                    var path = rdr.GetString(0);
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }
            }

            return null;
        }

        public async Task<string?> GetLastVoiceAsync(CancellationToken cancellationToken = default)
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync(cancellationToken);

            const string sql = "SELECT audio_file FROM Voices ORDER BY KEYID DESC LIMIT 10;";
            using var cmd = new SqliteCommand(sql, conn);
            using var rdr = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await rdr.ReadAsync(cancellationToken))
            {
                var path = rdr.GetString(0);
                if (File.Exists(path))
                {
                    return path;
                }
            }
            return null;
        }

        public async Task LogUserStartAsync(UserClass user, CancellationToken cancellationToken = default)
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync(cancellationToken);

            const string sql = @"
                INSERT INTO Users (first_name, last_name, username, id, language_code, preferred_language, date, is_bot, message_id, text)
                VALUES (@first_name, @last_name, @username, @id, @language_code, '', @date, @is_bot, @message_id, @text);";
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@first_name", user.first_name);
            cmd.Parameters.AddWithValue("@last_name", user.last_name);
            cmd.Parameters.AddWithValue("@username", user.username);
            cmd.Parameters.AddWithValue("@id", user.id);
            cmd.Parameters.AddWithValue("@language_code", user.languagecode);
            cmd.Parameters.AddWithValue("@date", user.date);
            cmd.Parameters.AddWithValue("@is_bot", user.isbot);
            cmd.Parameters.AddWithValue("@message_id", user.messageid);
            cmd.Parameters.AddWithValue("@text", user.text);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task LogMessageAsync(UserClass user, CancellationToken cancellationToken = default)
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync(cancellationToken);

            const string sql = @"
                INSERT INTO Logs (first_name, last_name, username, id, language_code, date, is_bot, message_id, text)
                VALUES (@first_name, @last_name, @username, @id, @language_code, @date, @is_bot, @message_id, @text);";
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@first_name", user.first_name);
            cmd.Parameters.AddWithValue("@last_name", user.last_name);
            cmd.Parameters.AddWithValue("@username", user.username);
            cmd.Parameters.AddWithValue("@id", user.id);
            cmd.Parameters.AddWithValue("@language_code", user.languagecode);
            cmd.Parameters.AddWithValue("@date", user.date);
            cmd.Parameters.AddWithValue("@is_bot", user.isbot);
            cmd.Parameters.AddWithValue("@message_id", user.messageid);
            cmd.Parameters.AddWithValue("@text", user.text);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<string?> GetUserPreferredLanguageAsync(string userId, CancellationToken cancellationToken = default)
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync(cancellationToken);

            const string sql = "SELECT preferred_language FROM Users WHERE id = @id AND preferred_language != '' ORDER BY KEYID DESC LIMIT 1;";
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", userId);
            var res = await cmd.ExecuteScalarAsync(cancellationToken);
            return res?.ToString();
        }

        public async Task SetUserPreferredLanguageAsync(string userId, string langCode, CancellationToken cancellationToken = default)
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync(cancellationToken);

            const string sql = "UPDATE Users SET preferred_language = @lang WHERE id = @id;";
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@lang", langCode);
            cmd.Parameters.AddWithValue("@id", userId);
            var updated = await cmd.ExecuteNonQueryAsync(cancellationToken);

            if (updated == 0)
            {
                // If user didn't have a row yet, insert a placeholder
                const string insertSql = "INSERT INTO Users (id, preferred_language, date) VALUES (@id, @lang, @date);";
                using var insertCmd = new SqliteCommand(insertSql, conn);
                insertCmd.Parameters.AddWithValue("@id", userId);
                insertCmd.Parameters.AddWithValue("@lang", langCode);
                insertCmd.Parameters.AddWithValue("@date", DateTime.UtcNow.ToString("o"));
                await insertCmd.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        public async Task<List<string>> GetVoicePathsOlderThanAsync(DateTime cutoff, CancellationToken cancellationToken = default)
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync(cancellationToken);

            var paths = new List<string>();
            const string sql = "SELECT audio_file FROM Voices WHERE date < @cutoff;";
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@cutoff", cutoff.ToString("o"));

            using var rdr = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await rdr.ReadAsync(cancellationToken))
            {
                if (!rdr.IsDBNull(0))
                {
                    paths.Add(rdr.GetString(0));
                }
            }

            return paths;
        }

        public async Task<int> DeleteRecordsOlderThanAsync(DateTime cutoff, CancellationToken cancellationToken = default)
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync(cancellationToken);

            var cutoffStr = cutoff.ToString("o");
            int total = 0;

            const string delVoices = "DELETE FROM Voices WHERE date < @cutoff;";
            using (var cmd = new SqliteCommand(delVoices, conn))
            {
                cmd.Parameters.AddWithValue("@cutoff", cutoffStr);
                total += await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            const string delLogs = "DELETE FROM Logs WHERE date < @cutoff;";
            using (var cmd = new SqliteCommand(delLogs, conn))
            {
                cmd.Parameters.AddWithValue("@cutoff", cutoffStr);
                total += await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            return total;
        }

        public async Task<HashSet<string>> GetAllKnownAudioPathsAsync(CancellationToken cancellationToken = default)
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync(cancellationToken);

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            const string sql = "SELECT audio_file FROM Voices;";
            using var cmd = new SqliteCommand(sql, conn);
            using var rdr = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await rdr.ReadAsync(cancellationToken))
            {
                if (!rdr.IsDBNull(0))
                {
                    set.Add(Path.GetFullPath(rdr.GetString(0)));
                }
            }
            return set;
        }

        public async Task VacuumAsync(CancellationToken cancellationToken = default)
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync(cancellationToken);

            using var cmd = new SqliteCommand("VACUUM;", conn);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<DatabaseStats> GetStatsAsync(CancellationToken cancellationToken = default)
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync(cancellationToken);

            long voices = 0, users = 0, logs = 0;

            using (var cmd = new SqliteCommand("SELECT COUNT(1) FROM Voices;", conn))
            {
                voices = Convert.ToInt64(await cmd.ExecuteScalarAsync(cancellationToken) ?? 0);
            }

            using (var cmd = new SqliteCommand("SELECT COUNT(1) FROM Users;", conn))
            {
                users = Convert.ToInt64(await cmd.ExecuteScalarAsync(cancellationToken) ?? 0);
            }

            using (var cmd = new SqliteCommand("SELECT COUNT(1) FROM Logs;", conn))
            {
                logs = Convert.ToInt64(await cmd.ExecuteScalarAsync(cancellationToken) ?? 0);
            }

            long dbSize = 0;
            var builder = new SqliteConnectionStringBuilder(_connectionString);
            if (!string.IsNullOrEmpty(builder.DataSource) && File.Exists(builder.DataSource))
            {
                dbSize = new FileInfo(builder.DataSource).Length;
            }

            return new DatabaseStats(voices, users, logs, dbSize);
        }
    }
}