using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Diagnostics;
using System.Net.Http;
using Newtonsoft.Json;
using FontAwesome.Sharp;

namespace CodeCrakers.Views
{
    public partial class NotificationPage : UserControl
    {
        private List<ContestNotification> notifications = new List<ContestNotification>();
        private List<Contest> upcomingContests = new List<Contest>();
        private HttpClient httpClient;

        public NotificationPage()
        {
            InitializeComponent();
            httpClient = new HttpClient();
            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                await LoadUpcomingContests();
                LoadNotifications();
                UpdateNotificationCount();
            }
            catch (Exception ex)
            {
                ShowError($"Error loading data: {ex.Message}");
            }
        }

        private async Task LoadUpcomingContests()
        {
            try
            {
                txtNoContests.Text = "Loading upcoming contests...";
                contestsPanel.Children.Clear();
                contestsPanel.Children.Add(txtNoContests);

                // Fetch upcoming contests from Codeforces API
                var response = await httpClient.GetStringAsync("https://codeforces.com/api/contest.list");
                var apiResponse = JsonConvert.DeserializeObject<CodeforcesApiResponse<List<Contest>>>(response);

                if (apiResponse.Status == "OK")
                {
                    upcomingContests.Clear();
                    var currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();

                    foreach (var contest in apiResponse.Result)
                    {
                        if (contest.Phase == "BEFORE" && contest.StartTimeSeconds > currentTime)
                        {
                            upcomingContests.Add(contest);
                        }
                    }

                    // Sort by start time
                    upcomingContests.Sort((a, b) => a.StartTimeSeconds.CompareTo(b.StartTimeSeconds));

                    DisplayUpcomingContests();
                }
                else
                {
                    txtNoContests.Text = "Failed to load contests.";
                }
            }
            catch (Exception ex)
            {
                txtNoContests.Text = "Error loading contests.";
                ShowError($"Error loading contests: {ex.Message}");
            }
        }

        private void DisplayUpcomingContests()
        {
            contestsPanel.Children.Clear();

            if (upcomingContests.Count == 0)
            {
                txtNoContests.Text = "No upcoming contests found.";
                contestsPanel.Children.Add(txtNoContests);
                return;
            }

            foreach (var contest in upcomingContests)
            {
                var contestItem = CreateContestItem(contest);
                contestsPanel.Children.Add(contestItem);
            }
        }

        private Border CreateContestItem(Contest contest)
        {
            var border = new Border
            {
                Style = (Style)Resources["ContestItemStyle"]
            };

            var mainGrid = new Grid();
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Contest info
            var infoStack = new StackPanel();
            
            var titleText = new TextBlock
            {
                Text = contest.Name,
                FontFamily = new FontFamily("Montserrat"),
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Foreground = (Brush)FindResource("titleColor1"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 5)
            };

            var startTime = DateTimeOffset.FromUnixTimeSeconds(contest.StartTimeSeconds);
            var timeText = new TextBlock
            {
                Text = $"Starts: {startTime:MMM dd, yyyy HH:mm} UTC",
                FontFamily = new FontFamily("Montserrat"),
                FontSize = 12,
                Foreground = (Brush)FindResource("plainTextColor1"),
                Margin = new Thickness(0, 0, 0, 2)
            };

            var durationText = new TextBlock
            {
                Text = $"Duration: {TimeSpan.FromSeconds(contest.DurationSeconds):h\\:mm}",
                FontFamily = new FontFamily("Montserrat"),
                FontSize = 12,
                Foreground = (Brush)FindResource("plainTextColor1")
            };

            infoStack.Children.Add(titleText);
            infoStack.Children.Add(timeText);
            infoStack.Children.Add(durationText);

            // Time remaining
            var timeRemaining = startTime - DateTimeOffset.Now;
            var countdownText = new TextBlock
            {
                Text = timeRemaining.TotalDays > 1 ? 
                    $"In {(int)timeRemaining.TotalDays} days" : 
                    $"In {timeRemaining:h\\:mm}",
                FontFamily = new FontFamily("Montserrat"),
                FontSize = 12,
                FontWeight = FontWeights.Medium,
                Foreground = (Brush)FindResource("color2"),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            Grid.SetColumn(infoStack, 0);
            Grid.SetColumn(countdownText, 1);

            mainGrid.Children.Add(infoStack);
            mainGrid.Children.Add(countdownText);

            border.Child = mainGrid;

            // Add click event to open contest URL
            border.MouseLeftButtonUp += (s, e) => 
            {
                try
                {
                    var url = $"https://codeforces.com/contest/{contest.Id}";
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    ShowError($"Error opening contest URL: {ex.Message}");
                }
            };

            return border;
        }

        private void LoadNotifications()
        {
            // For now, create sample notifications
            // In a real application, this would load from a database or API
            notifications.Clear();
            
            // Add notifications for upcoming contests
            foreach (var contest in upcomingContests.Take(3))
            {
                var startTime = DateTimeOffset.FromUnixTimeSeconds(contest.StartTimeSeconds);
                var timeUntilStart = startTime - DateTimeOffset.Now;
                
                if (timeUntilStart.TotalHours <= 24 && timeUntilStart.TotalHours > 0)
                {
                    notifications.Add(new ContestNotification
                    {
                        Id = Guid.NewGuid().ToString(),
                        Title = "Contest Starting Soon",
                        Message = $"{contest.Name} starts in {(timeUntilStart.TotalHours < 1 ? $"{(int)timeUntilStart.TotalMinutes} minutes" : $"{(int)timeUntilStart.TotalHours} hours")}",
                        Timestamp = DateTimeOffset.Now,
                        IsRead = false,
                        Type = NotificationType.ContestReminder,
                        ContestId = contest.Id
                    });
                }
            }

            // Add some general notifications
            if (notifications.Count == 0)
            {
                notifications.Add(new ContestNotification
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Welcome to CodeCrakers",
                    Message = "Stay updated with upcoming coding contests and track your progress!",
                    Timestamp = DateTimeOffset.Now.AddHours(-1),
                    IsRead = false,
                    Type = NotificationType.General
                });
            }

            DisplayNotifications();
        }

        private void DisplayNotifications()
        {
            notificationsPanel.Children.Clear();

            if (notifications.Count == 0)
            {
                txtNoNotifications.Text = "No notifications yet.";
                notificationsPanel.Children.Add(txtNoNotifications);
                return;
            }

            foreach (var notification in notifications.OrderByDescending(n => n.Timestamp))
            {
                var notificationItem = CreateNotificationItem(notification);
                notificationsPanel.Children.Add(notificationItem);
            }
        }

        private Border CreateNotificationItem(ContestNotification notification)
        {
            var border = new Border
            {
                Style = (Style)Resources["NotificationItemStyle"],
                Opacity = notification.IsRead ? 0.7 : 1.0
            };

            var mainGrid = new Grid();
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Icon
            var icon = new IconImage
            {
                Icon = notification.Type == NotificationType.ContestReminder ? IconChar.Calendar : IconChar.Info,
                Width = 20,
                Height = 20,
                Foreground = notification.IsRead ? (Brush)FindResource("plainTextColor1") : (Brush)FindResource("color1"),
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 5, 15, 0)
            };

            // Content
            var contentStack = new StackPanel();
            
            var titleText = new TextBlock
            {
                Text = notification.Title,
                FontFamily = new FontFamily("Montserrat"),
                FontSize = 14,
                FontWeight = notification.IsRead ? FontWeights.Normal : FontWeights.Medium,
                Foreground = (Brush)FindResource("titleColor1"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 5)
            };

            var messageText = new TextBlock
            {
                Text = notification.Message,
                FontFamily = new FontFamily("Montserrat"),
                FontSize = 12,
                Foreground = (Brush)FindResource("plainTextColor1"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 5)
            };

            var timeText = new TextBlock
            {
                Text = FormatRelativeTime(notification.Timestamp),
                FontFamily = new FontFamily("Montserrat"),
                FontSize = 11,
                Foreground = (Brush)FindResource("plainTextColor1"),
                Opacity = 0.8
            };

            contentStack.Children.Add(titleText);
            contentStack.Children.Add(messageText);
            contentStack.Children.Add(timeText);

            // Unread indicator
            var unreadIndicator = new Border
            {
                Width = 8,
                Height = 8,
                CornerRadius = new CornerRadius(4),
                Background = (Brush)FindResource("color2"),
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 8, 0, 0),
                Visibility = notification.IsRead ? Visibility.Collapsed : Visibility.Visible
            };

            Grid.SetColumn(icon, 0);
            Grid.SetColumn(contentStack, 1);
            Grid.SetColumn(unreadIndicator, 2);

            mainGrid.Children.Add(icon);
            mainGrid.Children.Add(contentStack);
            mainGrid.Children.Add(unreadIndicator);

            border.Child = mainGrid;

            // Add click event to mark as read
            border.MouseLeftButtonUp += (s, e) => 
            {
                if (!notification.IsRead)
                {
                    notification.IsRead = true;
                    DisplayNotifications();
                    UpdateNotificationCount();
                }

                // If it's a contest notification, open the contest
                if (notification.Type == NotificationType.ContestReminder && notification.ContestId.HasValue)
                {
                    try
                    {
                        var url = $"https://codeforces.com/contest/{notification.ContestId}";
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        ShowError($"Error opening contest URL: {ex.Message}");
                    }
                }
            };

            return border;
        }

        private string FormatRelativeTime(DateTimeOffset timestamp)
        {
            var diff = DateTimeOffset.Now - timestamp;
            
            if (diff.TotalMinutes < 1)
                return "Just now";
            else if (diff.TotalMinutes < 60)
                return $"{(int)diff.TotalMinutes} minutes ago";
            else if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours} hours ago";
            else if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays} days ago";
            else
                return timestamp.ToString("MMM dd, yyyy");
        }

        private void UpdateNotificationCount()
        {
            var unreadCount = notifications.Count(n => !n.IsRead);
            
            if (unreadCount == 0)
            {
                txtNotificationCount.Text = "No unread notifications";
            }
            else
            {
                txtNotificationCount.Text = $"{unreadCount} unread notification{(unreadCount > 1 ? "s" : "")}";
            }
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadUpcomingContests();
            LoadNotifications();
            UpdateNotificationCount();
        }

        private void btnMarkAllRead_Click(object sender, RoutedEventArgs e)
        {
            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }
            DisplayNotifications();
            UpdateNotificationCount();
        }
    }

    // Data models
    public class ContestNotification
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public bool IsRead { get; set; }
        public NotificationType Type { get; set; }
        public int? ContestId { get; set; }
    }

    public enum NotificationType
    {
        General,
        ContestReminder,
        Achievement,
        System
    }

    public class Contest
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("phase")]
        public string Phase { get; set; }

        [JsonProperty("frozen")]
        public bool Frozen { get; set; }

        [JsonProperty("durationSeconds")]
        public int DurationSeconds { get; set; }

        [JsonProperty("startTimeSeconds")]
        public long StartTimeSeconds { get; set; }

        [JsonProperty("relativeTimeSeconds")]
        public long RelativeTimeSeconds { get; set; }
    }

    public class CodeforcesApiResponse<T>
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("result")]
        public T Result { get; set; }
    }
}