using Microsoft.Data.Sqlite;
using CodeCrakers.Models;
using System;
using System.Collections.Generic;

namespace CodeCrakers.Data
{
    public class ContestRepository
    {
        public void Add(Contest contest)
        {
            using var connection = AppDb.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Contests (Name, Platform, StartTime, DurationSeconds, Url, PlatformContestId, Description, Type, CreatedAt, IsActive)
                VALUES (@name, @platform, @startTime, @durationSeconds, @url, @platformContestId, @description, @type, @createdAt, @isActive)";

            command.Parameters.AddWithValue("@name", contest.Name);
            command.Parameters.AddWithValue("@platform", contest.Platform);
            command.Parameters.AddWithValue("@startTime", contest.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@durationSeconds", contest.DurationSeconds);
            command.Parameters.AddWithValue("@url", contest.Url ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@platformContestId", contest.PlatformContestId);
            command.Parameters.AddWithValue("@description", contest.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@type", (int)contest.Type);
            command.Parameters.AddWithValue("@createdAt", contest.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@isActive", contest.IsActive ? 1 : 0);

            command.ExecuteNonQuery();
        }

        public List<Contest> GetUpcomingContests(int count = 20)
        {
            var contests = new List<Contest>();

            using var connection = AppDb.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Name, Platform, StartTime, DurationSeconds, Url, PlatformContestId, Description, Type, CreatedAt, IsActive
                FROM Contests
                WHERE IsActive = 1 AND datetime(StartTime) > datetime('now')
                ORDER BY datetime(StartTime) ASC
                LIMIT @count";

            command.Parameters.AddWithValue("@count", count);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                contests.Add(CreateContestFromReader(reader));
            }

            return contests;
        }

        public List<Contest> GetOngoingContests()
        {
            var contests = new List<Contest>();

            using var connection = AppDb.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Name, Platform, StartTime, DurationSeconds, Url, PlatformContestId, Description, Type, CreatedAt, IsActive
                FROM Contests
                WHERE IsActive = 1 
                  AND datetime(StartTime) <= datetime('now')
                  AND datetime(StartTime, '+' || DurationSeconds || ' seconds') > datetime('now')
                ORDER BY datetime(StartTime) ASC";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                contests.Add(CreateContestFromReader(reader));
            }

            return contests;
        }

        public Contest? GetById(int id)
        {
            using var connection = AppDb.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Name, Platform, StartTime, DurationSeconds, Url, PlatformContestId, Description, Type, CreatedAt, IsActive
                FROM Contests
                WHERE Id = @id";

            command.Parameters.AddWithValue("@id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return CreateContestFromReader(reader);
            }

            return null;
        }

        public Contest? GetByPlatformAndId(string platform, int platformContestId)
        {
            using var connection = AppDb.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Name, Platform, StartTime, DurationSeconds, Url, PlatformContestId, Description, Type, CreatedAt, IsActive
                FROM Contests
                WHERE Platform = @platform AND PlatformContestId = @platformContestId";

            command.Parameters.AddWithValue("@platform", platform);
            command.Parameters.AddWithValue("@platformContestId", platformContestId);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return CreateContestFromReader(reader);
            }

            return null;
        }

        public void Update(Contest contest)
        {
            using var connection = AppDb.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Contests 
                SET Name = @name,
                    StartTime = @startTime,
                    DurationSeconds = @durationSeconds,
                    Url = @url,
                    Description = @description,
                    Type = @type,
                    IsActive = @isActive
                WHERE Id = @id";

            command.Parameters.AddWithValue("@id", contest.Id);
            command.Parameters.AddWithValue("@name", contest.Name);
            command.Parameters.AddWithValue("@startTime", contest.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@durationSeconds", contest.DurationSeconds);
            command.Parameters.AddWithValue("@url", contest.Url ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@description", contest.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@type", (int)contest.Type);
            command.Parameters.AddWithValue("@isActive", contest.IsActive ? 1 : 0);

            command.ExecuteNonQuery();
        }

        public void Delete(int id)
        {
            using var connection = AppDb.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Contests WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id);

            command.ExecuteNonQuery();
        }

        public void CleanupOldContests(int daysToKeep = 7)
        {
            using var connection = AppDb.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Contests 
                SET IsActive = 0 
                WHERE datetime(StartTime, '+' || DurationSeconds || ' seconds') < datetime('now', '-' || @daysToKeep || ' days')";

            command.Parameters.AddWithValue("@daysToKeep", daysToKeep);

            command.ExecuteNonQuery();
        }

        private Contest CreateContestFromReader(SqliteDataReader reader)
        {
            return new Contest
            {
                Id = reader.GetInt32(0),  // Id
                Name = reader.GetString(1),  // Name
                Platform = reader.GetString(2),  // Platform
                StartTime = DateTime.Parse(reader.GetString(3)),  // StartTime
                DurationSeconds = reader.GetInt32(4),  // DurationSeconds
                Url = reader.IsDBNull(5) ? null : reader.GetString(5),  // Url
                PlatformContestId = reader.GetInt32(6),  // PlatformContestId
                Description = reader.IsDBNull(7) ? null : reader.GetString(7),  // Description
                Type = (ContestType)reader.GetInt32(8),  // Type
                CreatedAt = DateTime.Parse(reader.GetString(9)),  // CreatedAt
                IsActive = reader.GetInt32(10) == 1  // IsActive
            };
        }
    }
}