using Microsoft.Data.Sqlite;
using CodeCrakers.Models;

namespace CodeCrakers.Data
{
    public class ExternalUserRepository
    {
        public void Add(ExternalUser externalUser)
        {
            using var connection = AppDb.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO ExternalUsers (DisplayName, Codeforces, LeetCode, Codechef, Atcoder, Country, University, AddedAt, AddedBy, MaxRating, TotalSolved)
                VALUES (@displayName, @codeforces, @leetCode, @codechef, @atcoder, @country, @university, @addedAt, @addedBy, @maxRating, @totalSolved)";

            command.Parameters.AddWithValue("@displayName", externalUser.DisplayName);
            command.Parameters.AddWithValue("@codeforces", externalUser.Codeforces ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@leetCode", externalUser.LeetCode ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@codechef", externalUser.Codechef ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@atcoder", externalUser.Atcoder ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@country", externalUser.Country ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@university", externalUser.University ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@addedAt", externalUser.AddedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@addedBy", externalUser.AddedBy);
            command.Parameters.AddWithValue("@maxRating", externalUser.MaxRating);
            command.Parameters.AddWithValue("@totalSolved", externalUser.TotalSolved);

            command.ExecuteNonQuery();
        }

        public List<ExternalUser> GetAll()
        {
            var externalUsers = new List<ExternalUser>();

            using var connection = AppDb.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, DisplayName, Codeforces, LeetCode, Codechef, Atcoder, Country, University, AddedAt, AddedBy, MaxRating, TotalSolved
                FROM ExternalUsers
                ORDER BY MaxRating DESC, TotalSolved DESC";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                externalUsers.Add(new ExternalUser
                {
                    Id = reader.GetInt32(0),  // Id
                    DisplayName = reader.GetString(1),  // DisplayName
                    Codeforces = reader.IsDBNull(2) ? null : reader.GetString(2),  // Codeforces
                    LeetCode = reader.IsDBNull(3) ? null : reader.GetString(3),  // LeetCode
                    Codechef = reader.IsDBNull(4) ? null : reader.GetString(4),  // Codechef
                    Atcoder = reader.IsDBNull(5) ? null : reader.GetString(5),  // Atcoder
                    Country = reader.IsDBNull(6) ? null : reader.GetString(6),  // Country
                    University = reader.IsDBNull(7) ? null : reader.GetString(7),  // University
                    AddedAt = DateTime.Parse(reader.GetString(8)),  // AddedAt
                    AddedBy = reader.GetString(9),  // AddedBy
                    MaxRating = reader.GetInt32(10),  // MaxRating
                    TotalSolved = reader.GetInt32(11),  // TotalSolved
                    IsExternal = true
                });
            }

            return externalUsers;
        }

        public ExternalUser? GetById(int id)
        {
            using var connection = AppDb.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, DisplayName, Codeforces, LeetCode, Codechef, Atcoder, Country, University, AddedAt, AddedBy, MaxRating, TotalSolved
                FROM ExternalUsers
                WHERE Id = @id";

            command.Parameters.AddWithValue("@id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new ExternalUser
                {
                    Id = reader.GetInt32(0),  // Id
                    DisplayName = reader.GetString(1),  // DisplayName
                    Codeforces = reader.IsDBNull(2) ? null : reader.GetString(2),  // Codeforces
                    LeetCode = reader.IsDBNull(3) ? null : reader.GetString(3),  // LeetCode
                    Codechef = reader.IsDBNull(4) ? null : reader.GetString(4),  // Codechef
                    Atcoder = reader.IsDBNull(5) ? null : reader.GetString(5),  // Atcoder
                    Country = reader.IsDBNull(6) ? null : reader.GetString(6),  // Country
                    University = reader.IsDBNull(7) ? null : reader.GetString(7),  // University
                    AddedAt = DateTime.Parse(reader.GetString(8)),  // AddedAt
                    AddedBy = reader.GetString(9),  // AddedBy
                    MaxRating = reader.GetInt32(10),  // MaxRating
                    TotalSolved = reader.GetInt32(11),  // TotalSolved
                    IsExternal = true
                };
            }

            return null;
        }

        public void Update(ExternalUser externalUser)
        {
            using var connection = AppDb.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE ExternalUsers 
                SET DisplayName = @displayName,
                    Codeforces = @codeforces,
                    LeetCode = @leetCode,
                    Codechef = @codechef,
                    Atcoder = @atcoder,
                    Country = @country,
                    University = @university,
                    MaxRating = @maxRating,
                    TotalSolved = @totalSolved
                WHERE Id = @id";

            command.Parameters.AddWithValue("@id", externalUser.Id);
            command.Parameters.AddWithValue("@displayName", externalUser.DisplayName);
            command.Parameters.AddWithValue("@codeforces", externalUser.Codeforces ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@leetCode", externalUser.LeetCode ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@codechef", externalUser.Codechef ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@atcoder", externalUser.Atcoder ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@country", externalUser.Country ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@university", externalUser.University ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@maxRating", externalUser.MaxRating);
            command.Parameters.AddWithValue("@totalSolved", externalUser.TotalSolved);

            command.ExecuteNonQuery();
        }

        public void Delete(int id)
        {
            using var connection = AppDb.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM ExternalUsers WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id);

            command.ExecuteNonQuery();
        }

        public bool ExistsWithSamePlatformUsernames(ExternalUser externalUser)
        {
            using var connection = AppDb.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*) FROM ExternalUsers 
                WHERE (
                    (Codeforces IS NOT NULL AND Codeforces = @codeforces) OR
                    (LeetCode IS NOT NULL AND LeetCode = @leetCode) OR
                    (Codechef IS NOT NULL AND Codechef = @codechef) OR
                    (Atcoder IS NOT NULL AND Atcoder = @atcoder)
                ) AND Id != @id";

            command.Parameters.AddWithValue("@codeforces", externalUser.Codeforces ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@leetCode", externalUser.LeetCode ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@codechef", externalUser.Codechef ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@atcoder", externalUser.Atcoder ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@id", externalUser.Id);

            var result = command.ExecuteScalar();
            return Convert.ToInt32(result) > 0;
        }

        public void UpdateStats(int id, int maxRating, int totalSolved)
        {
            using var connection = AppDb.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE ExternalUsers 
                SET MaxRating = @maxRating, TotalSolved = @totalSolved 
                WHERE Id = @id";

            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@maxRating", maxRating);
            command.Parameters.AddWithValue("@totalSolved", totalSolved);

            command.ExecuteNonQuery();
        }
    }
}