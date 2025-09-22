using Microsoft.Data.Sqlite;
using CodeCrakers.Models;
using System;
using System.Collections.Generic;

namespace CodeCrakers.Data
{
    public class NotificationRepository
    {
        public void Add(Notification notification)
        {
            using var connection = AppDb.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Notifications (UserId, ContestId, Title, Message, Type, Priority, CreatedAt, ScheduledFor, IsRead, IsActive, ActionUrl, IconClass)
                VALUES (@userId, @contestId, @title, @message, @type, @priority, @createdAt, @scheduledFor, @isRead, @isActive, @actionUrl, @iconClass)";

            command.Parameters.AddWithValue("@userId", notification.UserId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@contestId", notification.ContestId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@title", notification.Title);
            command.Parameters.AddWithValue("@message", notification.Message);
            command.Parameters.AddWithValue("@type", (int)notification.Type);
            command.Parameters.AddWithValue("@priority", (int)notification.Priority);
            command.Parameters.AddWithValue("@createdAt", notification.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@scheduledFor", notification.ScheduledFor?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@isRead", notification.IsRead ? 1 : 0);
            command.Parameters.AddWithValue("@isActive", notification.IsActive ? 1 : 0);
            command.Parameters.AddWithValue("@actionUrl", notification.ActionUrl ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@iconClass", notification.IconClass ?? (object)DBNull.Value);

            command.ExecuteNonQuery();
        }

        public List<Notification> GetActiveNotifications(int? userId = null, int limit = 10)
        {
            var notifications = new List<Notification>();

            using var connection = AppDb.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            
            var userFilter = userId.HasValue ? "AND (UserId IS NULL OR UserId = @userId)" : "AND UserId IS NULL";
            
            command.CommandText = $@"
                SELECT Id, UserId, ContestId, Title, Message, Type, Priority, CreatedAt, ScheduledFor, IsRead, IsActive, ActionUrl, IconClass
                FROM Notifications
                WHERE IsActive = 1 
                  AND IsRead = 0 
                  AND (ScheduledFor IS NULL OR datetime(ScheduledFor) <= datetime('now'))
                  {userFilter}
                ORDER BY Priority DESC, datetime(CreatedAt) DESC
                LIMIT @limit";

            if (userId.HasValue)
                command.Parameters.AddWithValue("@userId", userId.Value);
            command.Parameters.AddWithValue("@limit", limit);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                notifications.Add(CreateNotificationFromReader(reader));
            }

            return notifications;
        }

        public List<Notification> GetNotifications(int? userId = null, int page = 1, int pageSize = 20)
        {
            var notifications = new List<Notification>();

            using var connection = AppDb.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            
            var userFilter = userId.HasValue ? "AND (UserId IS NULL OR UserId = @userId)" : "AND UserId IS NULL";
            var offset = (page - 1) * pageSize;
            
            command.CommandText = $@"
                SELECT Id, UserId, ContestId, Title, Message, Type, Priority, CreatedAt, ScheduledFor, IsRead, IsActive, ActionUrl, IconClass
                FROM Notifications
                WHERE IsActive = 1 {userFilter}
                ORDER BY IsRead ASC, Priority DESC, datetime(CreatedAt) DESC
                LIMIT @pageSize OFFSET @offset";

            if (userId.HasValue)
                command.Parameters.AddWithValue("@userId", userId.Value);
            command.Parameters.AddWithValue("@pageSize", pageSize);
            command.Parameters.AddWithValue("@offset", offset);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                notifications.Add(CreateNotificationFromReader(reader));
            }

            return notifications;
        }

        public bool NotificationExists(int contestId, NotificationType type)
        {
            using var connection = AppDb.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM Notifications 
                WHERE ContestId = @contestId AND Type = @type AND IsActive = 1";

            command.Parameters.AddWithValue("@contestId", contestId);
            command.Parameters.AddWithValue("@type", (int)type);

            var result = command.ExecuteScalar();
            return Convert.ToInt32(result) > 0;
        }

        public void MarkAsRead(int notificationId)
        {
            using var connection = AppDb.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE Notifications SET IsRead = 1 WHERE Id = @id";
            command.Parameters.AddWithValue("@id", notificationId);

            command.ExecuteNonQuery();
        }

        public void MarkAllAsRead(int? userId = null)
        {
            using var connection = AppDb.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            
            if (userId.HasValue)
            {
                command.CommandText = "UPDATE Notifications SET IsRead = 1 WHERE (UserId IS NULL OR UserId = @userId) AND IsRead = 0";
                command.Parameters.AddWithValue("@userId", userId.Value);
            }
            else
            {
                command.CommandText = "UPDATE Notifications SET IsRead = 1 WHERE UserId IS NULL AND IsRead = 0";
            }

            command.ExecuteNonQuery();
        }

        public void Delete(int notificationId)
        {
            using var connection = AppDb.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE Notifications SET IsActive = 0 WHERE Id = @id";
            command.Parameters.AddWithValue("@id", notificationId);

            command.ExecuteNonQuery();
        }

        public int GetUnreadCount(int? userId = null)
        {
            using var connection = AppDb.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            
            var userFilter = userId.HasValue ? "AND (UserId IS NULL OR UserId = @userId)" : "AND UserId IS NULL";
            
            command.CommandText = $@"
                SELECT COUNT(*) 
                FROM Notifications 
                WHERE IsActive = 1 
                  AND IsRead = 0 
                  AND (ScheduledFor IS NULL OR datetime(ScheduledFor) <= datetime('now'))
                  {userFilter}";

            if (userId.HasValue)
                command.Parameters.AddWithValue("@userId", userId.Value);

            var result = command.ExecuteScalar();
            return Convert.ToInt32(result);
        }

        public void CleanupOldNotifications(int daysToKeep = 30)
        {
            using var connection = AppDb.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Notifications 
                SET IsActive = 0 
                WHERE datetime(CreatedAt) < datetime('now', '-' || @daysToKeep || ' days')
                  AND IsRead = 1";

            command.Parameters.AddWithValue("@daysToKeep", daysToKeep);

            command.ExecuteNonQuery();
        }

        private Notification CreateNotificationFromReader(SqliteDataReader reader)
        {
            return new Notification
            {
                Id = reader.GetInt32(0),  // Id
                UserId = reader.IsDBNull(1) ? null : reader.GetInt32(1),  // UserId
                ContestId = reader.IsDBNull(2) ? null : reader.GetInt32(2),  // ContestId
                Title = reader.GetString(3),  // Title
                Message = reader.GetString(4),  // Message
                Type = (NotificationType)reader.GetInt32(5),  // Type
                Priority = (NotificationPriority)reader.GetInt32(6),  // Priority
                CreatedAt = DateTime.Parse(reader.GetString(7)),  // CreatedAt
                ScheduledFor = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8)),  // ScheduledFor
                IsRead = reader.GetInt32(9) == 1,  // IsRead
                IsActive = reader.GetInt32(10) == 1,  // IsActive
                ActionUrl = reader.IsDBNull(11) ? null : reader.GetString(11),  // ActionUrl
                IconClass = reader.IsDBNull(12) ? null : reader.GetString(12)  // IconClass
            };
        }
    }
}