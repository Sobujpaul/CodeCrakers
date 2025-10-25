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
using CodeCrakers.Services;
using System.Net.NetworkInformation;
using System.Windows.Threading;

namespace CodeCrakers.Views
{
    public partial class NotificationPage : UserControl
    {
    private List<ClistContest> upcomingContests = new List<ClistContest>();
        private HttpClient httpClient;
        private ClistScraperService clistScraper;
    private KontestsApiService kontestsApi;
    private DispatcherTimer? autoRefreshTimer;
        private bool isRefreshing;

        public NotificationPage()
        {
            InitializeComponent();
            httpClient = new HttpClient();
            clistScraper = new ClistScraperService();
            kontestsApi = new KontestsApiService();
            this.Loaded += NotificationPage_Loaded;
            this.Unloaded += NotificationPage_Unloaded;
            LoadData();
        }

        private void NotificationPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Start periodic auto-refresh (every 10 minutes)
                autoRefreshTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMinutes(10)
                };
                autoRefreshTimer.Tick += AutoRefreshTimer_Tick;
                autoRefreshTimer.Start();

                // Listen for network availability changes
                NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
            }
            catch { }
        }

        private void NotificationPage_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (autoRefreshTimer != null)
                {
                    autoRefreshTimer.Tick -= AutoRefreshTimer_Tick;
                    autoRefreshTimer.Stop();
                    autoRefreshTimer = null;
                }

                NetworkChange.NetworkAvailabilityChanged -= NetworkChange_NetworkAvailabilityChanged;

                clistScraper?.Dispose();
                httpClient?.Dispose();
            }
            catch { }
        }

        private async void AutoRefreshTimer_Tick(object? sender, EventArgs e)
        {
            await SafeRefreshAsync();
        }

        private void NetworkChange_NetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
        {
            if (e.IsAvailable)
            {
                // Refresh immediately when network comes back
                Dispatcher.Invoke(async () => await SafeRefreshAsync());
            }
        }

        private async void LoadData()
        {
            try
            {
                await LoadUpcomingContests();
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

                // Preferred source: kontests.net aggregator API (broad coverage across platforms)
                upcomingContests.Clear();
                try
                {
                    var all = await kontestsApi.GetAllUpcomingAsync();
                    if (all != null && all.Count > 0)
                    {
                        upcomingContests.AddRange(all);
                    }
                }
                catch { /* ignore and try next source */ }

                // Secondary source: clist.by scraping
                if (upcomingContests.Count == 0)
                {
                    try
                    {
                        var clistContests = await clistScraper.GetUpcomingContestsAsync();
                        upcomingContests.AddRange(clistContests);
                    }
                    catch { }
                }

                // Final fallback: internal contest sources (CF API + generators)
                if (upcomingContests.Count == 0)
                {
                    try
                    {
                        var api = new ContestApiService();
                        var alt = await api.GetUpcomingContestsAsync();
                        foreach (var c in alt)
                        {
                            upcomingContests.Add(new ClistContest
                            {
                                Name = c.Name,
                                Platform = c.Platform.ToLowerInvariant(),
                                StartTimeUtc = c.StartTime.ToUniversalTime(),
                                Duration = TimeSpan.FromSeconds(Math.Max(0, c.DurationSeconds)),
                                Url = string.IsNullOrWhiteSpace(c.Url) ? "https://clist.by/?view=list" : c.Url
                            });
                        }
                    }
                    catch { }
                }

                DisplayUpcomingContests();
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

        private Border CreateContestItem(ClistContest contest)
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

            var startTime = new DateTimeOffset(contest.StartTimeUtc, TimeSpan.Zero);
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
                Text = contest.Duration > TimeSpan.Zero ? $"Duration: {contest.Duration:h\\:mm}" : "",
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
                    var url = string.IsNullOrWhiteSpace(contest.Url) ? "https://clist.by/?view=list" : contest.Url;
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    ShowError($"Error opening contest URL: {ex.Message}");
                }
            };

            return border;
        }

        // Removed notifications list: page focuses on upcoming contests only

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await SafeRefreshAsync();
        }

        private async Task SafeRefreshAsync()
        {
            if (isRefreshing) return;
            try
            {
                isRefreshing = true;
                await LoadUpcomingContests();
            }
            finally
            {
                isRefreshing = false;
            }
        }
    }
    // Removed notifications + local API models; using aggregator services for contests
}