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
    FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE
);";
                cmd.ExecuteNonQuery();
            }
        }

        public static SqliteConnection GetConnection()
        {
            return new SqliteConnection(ConnectionString);
        }
    }
}
