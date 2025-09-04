using Microsoft.Data.Sqlite;
using System;
using CodeCrakers.Models;

namespace CodeCrakers.Data
{
    public class UserRepository
    {
        // ✅ Check if username or email already exists
        public bool UsernameOrEmailExists(string username, string email)
        {
            using var con = AppDb.GetConnection();
            con.Open();

            const string sql = @"SELECT 1 FROM Users WHERE Username=@u OR Email=@e LIMIT 1;";
            using var cmd = new SqliteCommand(sql, con);
            cmd.Parameters.AddWithValue("@u", username);
            cmd.Parameters.AddWithValue("@e", email);

            var result = cmd.ExecuteScalar();
            return result != null;
        }

        // ✅ Create a new user
        public int CreateUser(string username, string email, string rawPassword)
        {
            var hash = PasswordHasher.Hash(rawPassword);

            using var con = AppDb.GetConnection();
            con.Open();

            const string sql = @"INSERT INTO Users(Username, Email, PasswordHash)
                                 VALUES(@u, @e, @p);
                                 SELECT last_insert_rowid();";

            using var cmd = new SqliteCommand(sql, con);
            cmd.Parameters.AddWithValue("@u", username);
            cmd.Parameters.AddWithValue("@e", email);
            cmd.Parameters.AddWithValue("@p", hash);

            var newId = Convert.ToInt32(cmd.ExecuteScalar());

            // Also create an empty profile row for the new user
            const string profileSql = @"INSERT OR IGNORE INTO UserProfiles(UserId) VALUES(@id);";
            using var cmd2 = new SqliteCommand(profileSql, con);
            cmd2.Parameters.AddWithValue("@id", newId);
            cmd2.ExecuteNonQuery();

            return newId;
        }

        // ✅ Validate login (return user Id if correct, else null)
        public int? ValidateLogin(string usernameOrEmail, string rawPassword)
        {
            var hash = PasswordHasher.Hash(rawPassword);

            using var con = AppDb.GetConnection();
            con.Open();

            const string sql = @"SELECT Id FROM Users 
                                 WHERE (Username=@u OR Email=@u) AND PasswordHash=@p
                                 LIMIT 1;";
            using var cmd = new SqliteCommand(sql, con);
            cmd.Parameters.AddWithValue("@u", usernameOrEmail);
            cmd.Parameters.AddWithValue("@p", hash);

            var result = cmd.ExecuteScalar();
            return result == null ? (int?)null : Convert.ToInt32(result);
        }

        // ✅ Get full user info by Id
        public User GetById(int id)
        {
            using var con = AppDb.GetConnection();
            con.Open();

            const string sql = @"SELECT Id, Username, Email, PasswordHash FROM Users WHERE Id=@id;";
            using var cmd = new SqliteCommand(sql, con);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                Email = reader.GetString(2),
                PasswordHash = reader.GetString(3)
            };
        }
    }
}
