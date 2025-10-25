using Microsoft.Data.Sqlite;
using CodeCrakers.Models;
using System;
using System.Collections.Generic;

namespace CodeCrakers.Data
{
    public class UserProfileRepository
    {
        // ✅ Get profile info for a given user
        public UserProfile GetByUserId(int userId)
        {
            using var con = AppDb.GetConnection();
            con.Open();

            const string sql = @"SELECT UserId, Codeforces, LeetCode, Codechef, Atcoder, Country, University, IsHidden, DisplayName, TotalRating, TotalSolved 
                                 FROM UserProfiles WHERE UserId=@id;";
            using var cmd = new SqliteCommand(sql, con);
            cmd.Parameters.AddWithValue("@id", userId);

            using var r = cmd.ExecuteReader();
            if (!r.Read()) return new UserProfile { UserId = userId };

            return new UserProfile
            {
                UserId = r.GetInt32(0),
                Codeforces = r.IsDBNull(1) ? null : r.GetString(1),
                LeetCode = r.IsDBNull(2) ? null : r.GetString(2),
                Codechef = r.IsDBNull(3) ? null : r.GetString(3),
                Atcoder = r.IsDBNull(4) ? null : r.GetString(4),
                Country = r.IsDBNull(5) ? null : r.GetString(5),
                University = r.IsDBNull(6) ? null : r.GetString(6),
                IsHidden = r.IsDBNull(7) ? 0 : r.GetInt32(7),
                DisplayName = r.IsDBNull(8) ? null : r.GetString(8),
                TotalRating = r.IsDBNull(9) ? 0 : r.GetInt32(9),
                TotalSolved = r.IsDBNull(10) ? 0 : r.GetInt32(10)
            };
        }

        // ✅ Insert or update platform usernames
    public void Upsert(int userId, string? codeforces, string? leetcode, string? codechef, string? atcoder)
        {
            using var con = AppDb.GetConnection();
            con.Open();

            const string sql = @"
INSERT INTO UserProfiles(UserId, Codeforces, LeetCode, Codechef, Atcoder)
VALUES(@id, @cf, @lc, @cc, @ac)
ON CONFLICT(UserId) DO UPDATE SET
    Codeforces = excluded.Codeforces,
    LeetCode   = excluded.LeetCode,
    Codechef   = excluded.Codechef,
    Atcoder    = excluded.Atcoder;";

            using var cmd = new SqliteCommand(sql, con);
            cmd.Parameters.AddWithValue("@id", userId);
            cmd.Parameters.AddWithValue("@cf", (object?)codeforces ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@lc", (object?)leetcode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cc", (object?)codechef ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ac", (object?)atcoder ?? DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        // ✅ Insert or update profile with all fields
    public void Upsert(int userId, string? codeforces, string? leetcode, string? codechef, string? atcoder, 
              string? country, string? university, int isHidden)
        {
            using var con = AppDb.GetConnection();
            con.Open();

            const string sql = @"
INSERT INTO UserProfiles(UserId, Codeforces, LeetCode, Codechef, Atcoder, Country, University, IsHidden, DisplayName, TotalRating, TotalSolved)
VALUES(@id, @cf, @lc, @cc, @ac, @country, @university, @isHidden, @displayName, @totalRating, @totalSolved)
ON CONFLICT(UserId) DO UPDATE SET
    Codeforces = excluded.Codeforces,
    LeetCode   = excluded.LeetCode,
    Codechef   = excluded.Codechef,
    Atcoder    = excluded.Atcoder,
    Country    = excluded.Country,
    University = excluded.University,
    IsHidden   = excluded.IsHidden,
    DisplayName = excluded.DisplayName,
    TotalRating = excluded.TotalRating,
    TotalSolved = excluded.TotalSolved;";

            using var cmd = new SqliteCommand(sql, con);
            cmd.Parameters.AddWithValue("@id", userId);
            cmd.Parameters.AddWithValue("@cf", (object?)codeforces ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@lc", (object?)leetcode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cc", (object?)codechef ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ac", (object?)atcoder ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@country", (object?)country ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@university", (object?)university ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@isHidden", isHidden);
            cmd.Parameters.AddWithValue("@displayName", DBNull.Value); // Will be updated by API calls
            cmd.Parameters.AddWithValue("@totalRating", 0); // Will be updated by API calls
            cmd.Parameters.AddWithValue("@totalSolved", 0); // Will be updated by API calls

            cmd.ExecuteNonQuery();
        }

        // ✅ Get multiple users for leaderboard (with optional search, filter, sorting, and paging)
        public List<UserProfile> GetLeaderboard(string? codeforcesSearch = null,
                                                string? country = null,
                                                string? university = null,
                                                string sortBy = "rating",
                                                int page = 1, int pageSize = 10)
        {
            List<UserProfile> users = new List<UserProfile>();
            using var con = AppDb.GetConnection();
            con.Open();

            // Basic SQL
            string sql = @"SELECT u.UserId, u.Codeforces, u.LeetCode, u.Codechef, u.Atcoder, 
                                  u.DisplayName, u.Country, u.University, u.TotalRating, u.TotalSolved
                           FROM UserProfiles u
                           WHERE 1=1 ";

            if (!string.IsNullOrEmpty(codeforcesSearch))
                sql += " AND u.Codeforces LIKE @cfSearch ";

            if (!string.IsNullOrEmpty(country))
                sql += " AND u.Country = @country ";

            if (!string.IsNullOrEmpty(university))
                sql += " AND u.University = @university ";

            // Sorting
            sql += sortBy.ToLower() switch
            {
                "solved" => " ORDER BY u.TotalSolved DESC ",
                _ => " ORDER BY u.TotalRating DESC "
            };

            // Paging
            sql += " LIMIT @limit OFFSET @offset;";

            using var cmd = new SqliteCommand(sql, con);
            cmd.Parameters.AddWithValue("@cfSearch", $"%{codeforcesSearch}%");
            cmd.Parameters.AddWithValue("@country", (object?)country ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@university", (object?)university ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@limit", pageSize);
            cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                users.Add(new UserProfile
                {
                    UserId = r.GetInt32(0),
                    Codeforces = r.IsDBNull(1) ? null : r.GetString(1),
                    LeetCode = r.IsDBNull(2) ? null : r.GetString(2),
                    Codechef = r.IsDBNull(3) ? null : r.GetString(3),
                    Atcoder = r.IsDBNull(4) ? null : r.GetString(4),
                    DisplayName = r.IsDBNull(5) ? $"User {r.GetInt32(0)}" : r.GetString(5),
                    Country = r.IsDBNull(6) ? null : r.GetString(6),
                    University = r.IsDBNull(7) ? null : r.GetString(7),
                    TotalRating = r.IsDBNull(8) ? 0 : r.GetInt32(8),
                    TotalSolved = r.IsDBNull(9) ? 0 : r.GetInt32(9)
                });
            }

            return users;
        }

        // Fetch ALL matching users without pagination so higher layers can perform correct cross-source sorting.
        public List<UserProfile> GetLeaderboardAll(string? codeforcesSearch = null,
                                                   string? country = null,
                                                   string? university = null,
                                                   string sortBy = "rating")
        {
            var users = new List<UserProfile>();
            using var con = AppDb.GetConnection();
            con.Open();

            string sql = @"SELECT u.UserId, u.Codeforces, u.LeetCode, u.Codechef, u.Atcoder, 
                                  u.DisplayName, u.Country, u.University, u.TotalRating, u.TotalSolved
                           FROM UserProfiles u
                           WHERE 1=1 ";

            if (!string.IsNullOrEmpty(codeforcesSearch))
                sql += " AND u.Codeforces LIKE @cfSearch ";
            if (!string.IsNullOrEmpty(country))
                sql += " AND u.Country = @country ";
            if (!string.IsNullOrEmpty(university))
                sql += " AND u.University = @university ";

            sql += sortBy.ToLower() switch
            {
                "solved" => " ORDER BY u.TotalSolved DESC ",
                _ => " ORDER BY u.TotalRating DESC "
            };

            using var cmd = new SqliteCommand(sql, con);
            cmd.Parameters.AddWithValue("@cfSearch", $"%{codeforcesSearch}%");
            cmd.Parameters.AddWithValue("@country", (object?)country ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@university", (object?)university ?? DBNull.Value);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                users.Add(new UserProfile
                {
                    UserId = r.GetInt32(0),
                    Codeforces = r.IsDBNull(1) ? null : r.GetString(1),
                    LeetCode = r.IsDBNull(2) ? null : r.GetString(2),
                    Codechef = r.IsDBNull(3) ? null : r.GetString(3),
                    Atcoder = r.IsDBNull(4) ? null : r.GetString(4),
                    DisplayName = r.IsDBNull(5) ? $"User {r.GetInt32(0)}" : r.GetString(5),
                    Country = r.IsDBNull(6) ? null : r.GetString(6),
                    University = r.IsDBNull(7) ? null : r.GetString(7),
                    TotalRating = r.IsDBNull(8) ? 0 : r.GetInt32(8),
                    TotalSolved = r.IsDBNull(9) ? 0 : r.GetInt32(9)
                });
            }

            return users;
        }
    }
}
