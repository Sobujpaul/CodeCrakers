using Microsoft.Data.Sqlite;
using CodeCrakers.Models;
using System;

namespace CodeCrakers.Data
{
    public class UserProfileRepository
    {
        // ✅ Get profile info for a given user
        public UserProfile GetByUserId(int userId)
        {
            using var con = AppDb.GetConnection();
            con.Open();

            const string sql = @"SELECT UserId, Codeforces, LeetCode, Codechef, Atcoder 
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
            };
        }

        // ✅ Insert or update platform usernames
        public void Upsert(int userId, string codeforces, string leetcode, string codechef, string atcoder)
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
    }
}
