using Microsoft.Data.Sqlite;
using System;
using System.Security.Cryptography;
using System.Text;

namespace CodeCrakers.Data
{
    public class PasswordResetRepository
    {
        public string CreateResetToken(int userId, TimeSpan? lifetime = null)
        {
            var ttl = lifetime ?? TimeSpan.FromMinutes(15);
            var token = GenerateToken();

            using var con = AppDb.GetConnection();
            con.Open();

            const string sql = @"INSERT INTO PasswordResets(UserId, Token, ExpiresAt) VALUES(@u, @t, @e);";
            using var cmd = new SqliteCommand(sql, con);
            cmd.Parameters.AddWithValue("@u", userId);
            cmd.Parameters.AddWithValue("@t", token);
            cmd.Parameters.AddWithValue("@e", DateTime.UtcNow.Add(ttl).ToString("o"));
            cmd.ExecuteNonQuery();

            return token;
        }

        public int? ValidateToken(string token)
        {
            using var con = AppDb.GetConnection();
            con.Open();

            const string sql = @"SELECT UserId FROM PasswordResets WHERE Token=@t AND Used=0 AND ExpiresAt > @now LIMIT 1;";
            using var cmd = new SqliteCommand(sql, con);
            cmd.Parameters.AddWithValue("@t", token);
            cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"));
            var result = cmd.ExecuteScalar();
            return result == null ? (int?)null : Convert.ToInt32(result);
        }

        public void MarkTokenUsed(string token)
        {
            using var con = AppDb.GetConnection();
            con.Open();

            const string sql = @"UPDATE PasswordResets SET Used=1 WHERE Token=@t;";
            using var cmd = new SqliteCommand(sql, con);
            cmd.Parameters.AddWithValue("@t", token);
            cmd.ExecuteNonQuery();
        }

        private static string GenerateToken()
        {
            // 32 bytes => 256 bits => base64 ~ 43 chars (URL safe replacement)
            var bytes = RandomNumberGenerator.GetBytes(32);
            var token = Convert.ToBase64String(bytes)
                .Replace('+', 'A')
                .Replace('/', 'B')
                .TrimEnd('=');
            return token;
        }
    }
}
