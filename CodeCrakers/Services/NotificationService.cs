using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeCrakers.Models;
using CodeCrakers.Data;

namespace CodeCrakers.Services
{
    public class NotificationService
    {
        private readonly ContestApiService _contestApiService;
        private readonly NotificationRepository _notificationRepo;
        private readonly ContestRepository _contestRepo;

        public NotificationService()
        {
            _contestApiService = new ContestApiService();
            _notificationRepo = new NotificationRepository();
            _contestRepo = new ContestRepository();
        }

        public Task<List<Notification>> GetActiveNotificationsAsync(int? userId = null)
        {
            // No I/O here; return synchronously without changing behavior
            return Task.FromResult(_notificationRepo.GetActiveNotifications(userId));
        }

        public Task<List<Notification>> GetAllNotificationsAsync(int? userId = null, int page = 1, int pageSize = 20)
        {
            // No I/O here; return synchronously without changing behavior
            return Task.FromResult(_notificationRepo.GetNotifications(userId, page, pageSize));
        }

        public async Task RefreshContestsAndGenerateNotificationsAsync()
        {
            try
            {
                // Fetch latest contests from APIs
                var upcomingContests = await _contestApiService.GetUpcomingContestsAsync();
                
                // Update contests in database
                await UpdateContestsInDatabaseAsync(upcomingContests);
                
                // Generate notifications for upcoming contests
                await GenerateContestNotificationsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing contests and generating notifications: {ex.Message}");
            }
        }

        private Task UpdateContestsInDatabaseAsync(List<Contest> contests)
        {
            foreach (var contest in contests)
            {
                // Check if contest already exists
                var existingContest = _contestRepo.GetByPlatformAndId(contest.Platform, contest.PlatformContestId);
                
                if (existingContest == null)
                {
                    // Add new contest
                    _contestRepo.Add(contest);
                }
                else if (existingContest.StartTime != contest.StartTime || existingContest.Name != contest.Name)
                {
                    // Update existing contest if details changed
                    existingContest.Name = contest.Name;
                    existingContest.StartTime = contest.StartTime;
                    existingContest.DurationSeconds = contest.DurationSeconds;
                    existingContest.Url = contest.Url;
                    existingContest.Type = contest.Type;
                    _contestRepo.Update(existingContest);
                }
            }
            return Task.CompletedTask;
        }

        private async Task GenerateContestNotificationsAsync()
        {
            var upcomingContests = _contestRepo.GetUpcomingContests();
            var now = DateTime.UtcNow;

            foreach (var contest in upcomingContests)
            {
                await GenerateNotificationsForContestAsync(contest, now);
            }
        }

        private async Task GenerateNotificationsForContestAsync(Contest contest, DateTime now)
        {
            var timeUntilStart = contest.StartTime - now;

            // Generate different types of notifications based on timing
            await GenerateReminderNotifications(contest, timeUntilStart);
            await GenerateStartNotification(contest, timeUntilStart);
            await GenerateEndingNotification(contest, now);
        }

        private async Task GenerateReminderNotifications(Contest contest, TimeSpan timeUntilStart)
        {
            // 1 day reminder
            if (timeUntilStart.TotalDays <= 1.1 && timeUntilStart.TotalDays >= 0.9)
            {
                await CreateNotificationIfNotExists(
                    contestId: contest.Id,
                    type: NotificationType.ContestReminder,
                    title: "Contest Tomorrow!",
                    message: $"{contest.Name} starts in {FormatTimeSpan(timeUntilStart)} on {contest.Platform}",
                    priority: NotificationPriority.High,
                    actionUrl: contest.Url,
                    iconClass: GetPlatformIcon(contest.Platform)
                );
            }

            // 1 hour reminder
            if (timeUntilStart.TotalHours <= 1.1 && timeUntilStart.TotalHours >= 0.9)
            {
                await CreateNotificationIfNotExists(
                    contestId: contest.Id,
                    type: NotificationType.ContestReminder,
                    title: "Contest Starting Soon!",
                    message: $"{contest.Name} starts in {FormatTimeSpan(timeUntilStart)}",
                    priority: NotificationPriority.Critical,
                    actionUrl: contest.Url,
                    iconClass: GetPlatformIcon(contest.Platform)
                );
            }

            // 15 minutes reminder
            if (timeUntilStart.TotalMinutes <= 20 && timeUntilStart.TotalMinutes >= 10)
            {
                await CreateNotificationIfNotExists(
                    contestId: contest.Id,
                    type: NotificationType.ContestReminder,
                    title: "Final Reminder!",
                    message: $"{contest.Name} starts in {Math.Round(timeUntilStart.TotalMinutes)} minutes. Get ready!",
                    priority: NotificationPriority.Critical,
                    actionUrl: contest.Url,
                    iconClass: GetPlatformIcon(contest.Platform)
                );
            }
        }

        private async Task GenerateStartNotification(Contest contest, TimeSpan timeUntilStart)
        {
            // Contest just started (within 5 minutes)
            if (timeUntilStart.TotalMinutes <= 5 && timeUntilStart.TotalMinutes >= -5)
            {
                await CreateNotificationIfNotExists(
                    contestId: contest.Id,
                    type: NotificationType.ContestStarted,
                    title: "Contest Started!",
                    message: $"{contest.Name} has begun! Join now on {contest.Platform}",
                    priority: NotificationPriority.Critical,
                    actionUrl: contest.Url,
                    iconClass: GetPlatformIcon(contest.Platform)
                );
            }
        }

        private async Task GenerateEndingNotification(Contest contest, DateTime now)
        {
            var timeUntilEnd = contest.EndTime - now;
            
            // Contest ending in 30 minutes
            if (contest.IsOngoing && timeUntilEnd.TotalMinutes <= 35 && timeUntilEnd.TotalMinutes >= 25)
            {
                await CreateNotificationIfNotExists(
                    contestId: contest.Id,
                    type: NotificationType.ContestEnding,
                    title: "Contest Ending Soon!",
                    message: $"{contest.Name} ends in {Math.Round(timeUntilEnd.TotalMinutes)} minutes",
                    priority: NotificationPriority.High,
                    actionUrl: contest.Url,
                    iconClass: GetPlatformIcon(contest.Platform)
                );
            }
        }

        private Task CreateNotificationIfNotExists(int contestId, NotificationType type, string title, 
            string message, NotificationPriority priority, string? actionUrl = null, string? iconClass = null)
        {
            // Check if similar notification already exists
            if (!_notificationRepo.NotificationExists(contestId, type))
            {
                var notification = new Notification
                {
                    ContestId = contestId,
                    Title = title,
                    Message = message,
                    Type = type,
                    Priority = priority,
                    CreatedAt = DateTime.UtcNow,
                    ActionUrl = actionUrl,
                    IconClass = iconClass
                };

                _notificationRepo.Add(notification);
            }
            return Task.CompletedTask;
        }

        public void MarkAsRead(int notificationId)
        {
            _notificationRepo.MarkAsRead(notificationId);
        }

        public void MarkAllAsRead(int? userId = null)
        {
            _notificationRepo.MarkAllAsRead(userId);
        }

        public void DeleteNotification(int notificationId)
        {
            _notificationRepo.Delete(notificationId);
        }

        public int GetUnreadCount(int? userId = null)
        {
            return _notificationRepo.GetUnreadCount(userId);
        }

        public async Task<List<Contest>> GetUpcomingContestsAsync(int count = 10)
        {
            // First try to get from database
            var contests = _contestRepo.GetUpcomingContests(count);
            
            // If we don't have enough or they're old, refresh from APIs
            if (contests.Count < count / 2 || contests.Any(c => (DateTime.UtcNow - c.CreatedAt).TotalHours > 6))
            {
                await RefreshContestsAndGenerateNotificationsAsync();
                contests = _contestRepo.GetUpcomingContests(count);
            }

            return contests;
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 1)
                return $"{Math.Round(timeSpan.TotalDays)} day{(Math.Round(timeSpan.TotalDays) != 1 ? "s" : "")}";
            if (timeSpan.TotalHours >= 1)
                return $"{Math.Round(timeSpan.TotalHours)} hour{(Math.Round(timeSpan.TotalHours) != 1 ? "s" : "")}";
            return $"{Math.Round(timeSpan.TotalMinutes)} minute{(Math.Round(timeSpan.TotalMinutes) != 1 ? "s" : "")}";
        }

        private string GetPlatformIcon(string platform)
        {
            return platform.ToLower() switch
            {
                "codeforces" => "fas fa-code",
                "atcoder" => "fas fa-trophy",
                "codechef" => "fas fa-utensils",
                "leetcode" => "fas fa-laptop-code",
                _ => "fas fa-calendar"
            };
        }

        public Task CreateWelcomeNotification(int userId, string username)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = "Welcome to CodeCrakers!",
                Message = $"Hi {username}! Get ready to track contests and improve your competitive programming skills.",
                Type = NotificationType.Welcome,
                Priority = NotificationPriority.Normal,
                CreatedAt = DateTime.UtcNow,
                IconClass = "fas fa-star"
            };

            _notificationRepo.Add(notification);
            return Task.CompletedTask;
        }

        public Task CreateCustomNotification(int? userId, string title, string message, 
            NotificationType type = NotificationType.General, NotificationPriority priority = NotificationPriority.Normal)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                Priority = priority,
                CreatedAt = DateTime.UtcNow,
                IconClass = "fas fa-bell"
            };

            _notificationRepo.Add(notification);
            return Task.CompletedTask;
        }
    }
}