using System;

namespace CodeCrakers.Models
{
    public class Notification
    {
        public int Id { get; set; }                      // Primary Key
        public int? UserId { get; set; }                 // User ID (nullable for system-wide notifications)
        public int? ContestId { get; set; }              // Associated contest ID
        public string Title { get; set; } = string.Empty;               // Notification title
        public string Message { get; set; } = string.Empty;             // Notification message
        public NotificationType Type { get; set; }      // Type of notification
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
        public DateTime CreatedAt { get; set; }         // When notification was created
        public DateTime? ScheduledFor { get; set; }     // When to show the notification
        public bool IsRead { get; set; } = false;       // Whether user has read it
        public bool IsActive { get; set; } = true;      // Whether notification is active
        public string? ActionUrl { get; set; }          // Optional URL for action
        public string? IconClass { get; set; }          // CSS class for icon
        
        // Navigation property
        public Contest? Contest { get; set; }
        
        // Computed properties
        public bool IsScheduled => ScheduledFor.HasValue && ScheduledFor > DateTime.UtcNow;
        public bool ShouldShow => IsActive && !IsRead && (!ScheduledFor.HasValue || ScheduledFor <= DateTime.UtcNow);
        public string TimeAgo => GetTimeAgo(CreatedAt);
        
        private static string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;
            
            if (timeSpan.TotalDays >= 1)
                return $"{(int)timeSpan.TotalDays}d ago";
            if (timeSpan.TotalHours >= 1)
                return $"{(int)timeSpan.TotalHours}h ago";
            if (timeSpan.TotalMinutes >= 1)
                return $"{(int)timeSpan.TotalMinutes}m ago";
            return "Just now";
        }
    }
    
    public enum NotificationType
    {
        ContestReminder,
        ContestStarted,
        ContestEnding,
        ContestFinished,
        SystemUpdate,
        Achievement,
        Welcome,
        General
    }
    
    public enum NotificationPriority
    {
        Low,
        Normal,
        High,
        Critical
    }
}