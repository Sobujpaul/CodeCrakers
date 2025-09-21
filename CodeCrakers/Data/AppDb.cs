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
        }

        public static SqliteConnection GetConnection()
        {
            return new SqliteConnection(ConnectionString);
        }

        private static void EnsureColumn(SqliteConnection connection, string table, string column, string type, string defaultValue)
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
                var defaultClause = defaultValue == null ? string.Empty : $" DEFAULT {defaultValue}";
                using var alterCmd = connection.CreateCommand();
                alterCmd.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {type}{defaultClause};";
                alterCmd.ExecuteNonQuery();
            }
        }
    }
}
