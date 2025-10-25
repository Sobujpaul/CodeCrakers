using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace CodeCrakers.Data
{
    public static class AppDb
    {
        // Database file location in LocalAppData
        private static readonly string DbDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodeCrakers");

        private static readonly string DbFilePath =
            Path.Combine(DbDirectory, "users.db");

        private static readonly string ConnectionString =
            $"Data Source={DbFilePath};Cache=Shared;Foreign Keys=True;Mode=ReadWriteCreate";

        // Expose database path for diagnostics/logging
        public static string GetDatabasePath() => DbFilePath;

        // Called from App.xaml.cs at startup
        public static void Initialize()
        {
            if (!Directory.Exists(DbDirectory))
                Directory.CreateDirectory(DbDirectory);

            using var con = GetConnection();
            con.Open();

            using (var cmd = con.CreateCommand())
            {
                cmd.CommandText = @"
PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Users(
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    Username     TEXT NOT NULL UNIQUE,
    Email        TEXT NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    CreatedAt    TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS UserProfiles(
    UserId     INTEGER PRIMARY KEY,
    Codeforces TEXT,
    LeetCode   TEXT,
    Codechef   TEXT,
    Atcoder    TEXT,
    -- Optional metadata columns (added via migration below if missing)
    Country    TEXT,
    University TEXT,
    IsHidden   INTEGER DEFAULT 0,
    -- Leaderboard columns
    DisplayName TEXT,
    TotalRating INTEGER DEFAULT 0,
    TotalSolved INTEGER DEFAULT 0,
    FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS ExternalUsers(
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    DisplayName TEXT NOT NULL,
    Codeforces  TEXT,
    LeetCode    TEXT,
    Codechef    TEXT,
    Atcoder     TEXT,
    Country     TEXT,
    University  TEXT,
    AddedAt     TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    AddedBy     TEXT NOT NULL,
    MaxRating   INTEGER DEFAULT 0,
    TotalSolved INTEGER DEFAULT 0
);

CREATE TABLE IF NOT EXISTS Contests(
    Id                  INTEGER PRIMARY KEY AUTOINCREMENT,
    Name                TEXT NOT NULL,
    Platform            TEXT NOT NULL,
    StartTime           TEXT NOT NULL,
    DurationSeconds     INTEGER NOT NULL,
    Url                 TEXT,
    PlatformContestId   INTEGER NOT NULL,
    Description         TEXT,
    Type                INTEGER DEFAULT 0,
    CreatedAt           TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsActive            INTEGER DEFAULT 1,
    UNIQUE(Platform, PlatformContestId)
);

CREATE TABLE IF NOT EXISTS Notifications(
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId          INTEGER,
    ContestId       INTEGER,
    Title           TEXT NOT NULL,
    Message         TEXT NOT NULL,
    Type            INTEGER NOT NULL,
    Priority        INTEGER DEFAULT 1,
    CreatedAt       TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ScheduledFor    TEXT,
    IsRead          INTEGER DEFAULT 0,
    IsActive        INTEGER DEFAULT 1,
    ActionUrl       TEXT,
    IconClass       TEXT,
    FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY(ContestId) REFERENCES Contests(Id) ON DELETE CASCADE
);";
                cmd.ExecuteNonQuery();
            }

            // New table for password reset tokens (one-time codes)
            using (var cmd = con.CreateCommand())
            {
                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS PasswordResets(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL,
    Token TEXT NOT NULL,
    ExpiresAt TEXT NOT NULL,
    Used INTEGER DEFAULT 0,
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE
);";
                cmd.ExecuteNonQuery();
            }

            // Lightweight migration: add columns if they don't exist yet
            EnsureColumn(con, "UserProfiles", "Country", "TEXT", null);
            EnsureColumn(con, "UserProfiles", "University", "TEXT", null);
            EnsureColumn(con, "UserProfiles", "IsHidden", "INTEGER", "0");
            EnsureColumn(con, "UserProfiles", "DisplayName", "TEXT", null);
            EnsureColumn(con, "UserProfiles", "TotalRating", "INTEGER", "0");
            EnsureColumn(con, "UserProfiles", "TotalSolved", "INTEGER", "0");
            
            // Ensure ExternalUsers table columns (for future migrations)
            EnsureColumn(con, "ExternalUsers", "MaxRating", "INTEGER", "0");
            EnsureColumn(con, "ExternalUsers", "TotalSolved", "INTEGER", "0");
            
            // Ensure Contest and Notification table columns (for future migrations)
            EnsureColumn(con, "Contests", "Type", "INTEGER", "0");
            EnsureColumn(con, "Contests", "IsActive", "INTEGER", "1");
            EnsureColumn(con, "Notifications", "Priority", "INTEGER", "1");
            EnsureColumn(con, "Notifications", "ScheduledFor", "TEXT", null);
            EnsureColumn(con, "Notifications", "ActionUrl", "TEXT", null);
            EnsureColumn(con, "Notifications", "IconClass", "TEXT", null);
        }

        // Optional: lightweight database health check (integrity + foreign keys)
        public static void HealthCheck()
        {
            try
            {
                using var con = GetConnection();
                con.Open();

                using (var pragmaFk = con.CreateCommand())
                {
                    pragmaFk.CommandText = "PRAGMA foreign_keys";
                    var fk = pragmaFk.ExecuteScalar()?.ToString();
                    Utils.Logger.Log($"[DB] foreign_keys pragma: {fk}");
                }

                using (var integrity = con.CreateCommand())
                {
                    integrity.CommandText = "PRAGMA integrity_check";
                    var result = integrity.ExecuteScalar()?.ToString();
                    if (string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
                    {
                        Utils.Logger.Log("[DB] integrity_check: ok");
                    }
                    else
                    {
                        Utils.Logger.Log($"[DB] integrity_check: {result}");
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.Logger.Log($"[DB] HealthCheck exception: {ex}");
            }
        }

        public static SqliteConnection GetConnection()
        {
            return new SqliteConnection(ConnectionString);
        }

        private static void EnsureColumn(SqliteConnection connection, string table, string column, string type, string? defaultValue)
        {
            using var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = $"PRAGMA table_info({table});";
            using var reader = checkCmd.ExecuteReader();
            bool exists = false;
            while (reader.Read())
            {
                var name = reader.GetString(1);
                if (string.Equals(name, column, StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                var defaultClause = string.IsNullOrEmpty(defaultValue) ? string.Empty : $" DEFAULT {defaultValue}";
                using var alterCmd = connection.CreateCommand();
                alterCmd.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {type}{defaultClause};";
                alterCmd.ExecuteNonQuery();
            }
        }
    }
}
