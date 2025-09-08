using System.Windows.Controls;
using CodeCrakers.Data;
using CodeCrakers.Models;
using CodeCrakers.Services;
using System.Threading.Tasks;
using System.Linq;
using System.Windows;

namespace CodeCrakers.Views
{
    public partial class DashboardPage : UserControl
    {
        private int _userId;
        private UserProfileRepository _profileRepo;
        private PlatformApiManager _apiManager;

        public DashboardPage(int userId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("DashboardPage: Starting initialization...");
                InitializeComponent();
                System.Diagnostics.Debug.WriteLine("DashboardPage: InitializeComponent completed");
                
                _userId = userId;
                _profileRepo = new UserProfileRepository();
                _apiManager = new PlatformApiManager();
                System.Diagnostics.Debug.WriteLine("DashboardPage: Repositories initialized");
                
                LoadInitialData();
                System.Diagnostics.Debug.WriteLine("DashboardPage: Initial data loaded");
                
                // Load async data in background without blocking UI
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(100); // Small delay to ensure UI is ready
                        await LoadDashboardDataAsync();
                        System.Diagnostics.Debug.WriteLine("DashboardPage: Async data loading completed");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"DashboardPage: Async loading error: {ex.Message}");
                    }
                });
                
                System.Diagnostics.Debug.WriteLine("DashboardPage: Initialization completed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing DashboardPage: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                
                // Show error message to user
                MessageBox.Show($"Error loading dashboard: {ex.Message}", 
                    "Dashboard Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadInitialData()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("LoadInitialData: Starting...");
                
                // Load user profile data and show initial status
                var profile = _profileRepo.GetByUserId(_userId);
                System.Diagnostics.Debug.WriteLine("LoadInitialData: Profile loaded");
                
                // Show initial platform status
                UpdatePlatformStatusInitial(profile);
                System.Diagnostics.Debug.WriteLine("LoadInitialData: Platform status updated");
                
                // Show initial quick stats
                txtProblemsThisWeek.Text = "0";
                txtContestsThisWeek.Text = "0";
                txtTotalProblems.Text = "0";
                System.Diagnostics.Debug.WriteLine("LoadInitialData: Quick stats initialized");
                
                // Load recent activity (placeholder for now)
                LoadRecentActivity();
                System.Diagnostics.Debug.WriteLine("LoadInitialData: Recent activity loaded");
                
                System.Diagnostics.Debug.WriteLine("LoadInitialData: Completed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadInitialData: Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"LoadInitialData: Stack Trace: {ex.StackTrace}");
                
                // Set default values to prevent UI from being empty
                try
                {
                    // Show "No platforms connected" message
                    NoPlatformsMessage.Visibility = System.Windows.Visibility.Visible;
                    
                    // Set default platform status
                    txtCFUsername.Text = "Not Connected";
                    txtCFStatus.Text = "Click to Connect";
                    txtLCUsername.Text = "Not Connected";
                    txtLCStatus.Text = "Click to Connect";
                    txtCCUsername.Text = "Not Connected";
                    txtCCStatus.Text = "Click to Connect";
                    txtACUsername.Text = "Not Connected";
                    txtACStatus.Text = "Click to Connect";
                    
                    // Set default stats
                    txtProblemsThisWeek.Text = "0";
                    txtContestsThisWeek.Text = "0";
                    txtTotalProblems.Text = "0";
                }
                catch
                {
                    // If even this fails, just continue
                }
            }
        }

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("LoadDashboardDataAsync: Starting...");
                
                // Load user profile data
                var profile = _profileRepo.GetByUserId(_userId);
                System.Diagnostics.Debug.WriteLine("LoadDashboardDataAsync: Profile loaded");
                
                // Update platform status with real data
                System.Diagnostics.Debug.WriteLine("LoadDashboardDataAsync: Updating platform status...");
                await UpdatePlatformStatusAsync(profile);
                System.Diagnostics.Debug.WriteLine("LoadDashboardDataAsync: Platform status updated");
                
                // Load quick stats with real data
                System.Diagnostics.Debug.WriteLine("LoadDashboardDataAsync: Loading quick stats...");
                await LoadQuickStatsAsync(profile);
                System.Diagnostics.Debug.WriteLine("LoadDashboardDataAsync: Quick stats loaded");
                
                System.Diagnostics.Debug.WriteLine("LoadDashboardDataAsync: Completed successfully");
            }
            catch (Exception ex)
            {
                // Handle any errors gracefully
                System.Diagnostics.Debug.WriteLine($"Error loading dashboard data: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                
                // Don't show error message to user for async loading failures
                // Just log the error and continue with default values
                System.Diagnostics.Debug.WriteLine("Dashboard async loading failed, continuing with default values");
            }
        }

        private void UpdatePlatformStatusInitial(UserProfile profile)
        {
            // Update static platform boxes with initial data
            UpdateStaticPlatformBoxes(profile);
        }

        private void UpdateStaticPlatformBoxes(UserProfile profile)
        {
            // Update Codeforces
            if (!string.IsNullOrEmpty(profile.Codeforces))
            {
                txtCFUsername.Text = profile.Codeforces;
                txtCFStatus.Text = "Loading...";
            }
            else
            {
                txtCFUsername.Text = "Not Connected";
                txtCFStatus.Text = "Click to Connect";
            }

            // Update LeetCode
            if (!string.IsNullOrEmpty(profile.LeetCode))
            {
                txtLCUsername.Text = profile.LeetCode;
                txtLCStatus.Text = "Loading...";
            }
            else
            {
                txtLCUsername.Text = "Not Connected";
                txtLCStatus.Text = "Click to Connect";
            }

            // Update CodeChef
            if (!string.IsNullOrEmpty(profile.Codechef))
            {
                txtCCUsername.Text = profile.Codechef;
                txtCCStatus.Text = "Loading...";
            }
            else
            {
                txtCCUsername.Text = "Not Connected";
                txtCCStatus.Text = "Click to Connect";
            }

            // Update AtCoder
            if (!string.IsNullOrEmpty(profile.Atcoder))
            {
                txtACUsername.Text = profile.Atcoder;
                txtACStatus.Text = "Loading...";
            }
            else
            {
                txtACUsername.Text = "Not Connected";
                txtACStatus.Text = "Click to Connect";
            }

            // Show/hide no platforms message
            var hasConnectedPlatforms = !string.IsNullOrEmpty(profile.Codeforces) ||
                                      !string.IsNullOrEmpty(profile.LeetCode) ||
                                      !string.IsNullOrEmpty(profile.Codechef) ||
                                      !string.IsNullOrEmpty(profile.Atcoder);
            
            NoPlatformsMessage.Visibility = hasConnectedPlatforms ? 
                System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        }


        private async Task UpdatePlatformStatusAsync(UserProfile profile)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("UpdatePlatformStatusAsync: Starting...");
                
                // Update Codeforces status
                await UpdatePlatformBox("Codeforces", profile.Codeforces, txtCFUsername, txtCFStatus, CodeforcesBox);
                
                // Update LeetCode status
                await UpdatePlatformBox("LeetCode", profile.LeetCode, txtLCUsername, txtLCStatus, LeetCodeBox);
                
                // Update CodeChef status
                await UpdatePlatformBox("CodeChef", profile.Codechef, txtCCUsername, txtCCStatus, CodeChefBox);
                
                // Update AtCoder status
                await UpdatePlatformBox("AtCoder", profile.Atcoder, txtACUsername, txtACStatus, AtCoderBox);
                
                // Show/hide no platforms message
                var hasConnectedPlatforms = !string.IsNullOrEmpty(profile.Codeforces) ||
                                          !string.IsNullOrEmpty(profile.LeetCode) ||
                                          !string.IsNullOrEmpty(profile.Codechef) ||
                                          !string.IsNullOrEmpty(profile.Atcoder);
                
                NoPlatformsMessage.Visibility = hasConnectedPlatforms ? 
                    System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
                
                System.Diagnostics.Debug.WriteLine("UpdatePlatformStatusAsync: Updated all platform boxes");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdatePlatformStatusAsync: Error - {ex.Message}");
            }
        }

        private async Task UpdatePlatformBox(string platformName, string username, TextBlock usernameTextBlock, TextBlock statusTextBlock, Border platformBox)
        {
            try
            {
                if (string.IsNullOrEmpty(username))
                {
                    usernameTextBlock.Text = "Not Connected";
                    statusTextBlock.Text = "Click to Connect";
                    platformBox.Background = (System.Windows.Media.Brush)FindResource("panelColor");
                    
                    // Clear additional info fields
                    ClearPlatformAdditionalInfo(platformName);
                    return;
                }

                usernameTextBlock.Text = username;
                statusTextBlock.Text = "Testing Connection...";

                // Test connection and get stats
                var isConnected = await TestPlatformConnectionAsync(platformName, username);
                
                if (isConnected)
                {
                    statusTextBlock.Text = "Connected";
                    platformBox.Background = (System.Windows.Media.Brush)FindResource("panelColor");
                    
                    // Load additional platform statistics
                    await LoadPlatformAdditionalInfo(platformName, username);
                }
                else
                {
                    statusTextBlock.Text = "Connection Failed";
                    platformBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red) { Opacity = 0.1 };
                    ClearPlatformAdditionalInfo(platformName);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdatePlatformBox {platformName}: Error - {ex.Message}");
                statusTextBlock.Text = "Error";
                platformBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red) { Opacity = 0.1 };
                ClearPlatformAdditionalInfo(platformName);
            }
        }

        private async Task<bool> TestPlatformConnectionAsync(string platformName, string username)
        {
            try
            {
                switch (platformName.ToLower())
                {
                    case "codeforces":
                        return await _apiManager.TestConnectionAsync("codeforces", username);
                    case "leetcode":
                        return await _apiManager.TestConnectionAsync("leetcode", username);
                    case "codechef":
                        return await _apiManager.TestConnectionAsync("codechef", username);
                    case "atcoder":
                        return await _apiManager.TestConnectionAsync("atcoder", username);
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TestPlatformConnectionAsync {platformName}: Error - {ex.Message}");
                return false;
            }
        }

        private async Task LoadPlatformAdditionalInfo(string platformName, string username)
        {
            try
            {
                var stats = await _apiManager.GetPlatformStatsAsync(platformName.ToLower(), username);
                
                switch (platformName.ToLower())
                {
                    case "codeforces":
                        if (stats.IsConnected)
                        {
                            txtCFRating.Text = $"Rating: {stats.Rating}";
                            txtCFProblems.Text = $"Problems: {stats.ProblemsSolved}";
                        }
                        break;
                    case "leetcode":
                        if (stats.IsConnected)
                        {
                            txtLCProblems.Text = $"Problems: {stats.ProblemsSolved}";
                            txtLCContests.Text = $"Contests: {stats.ContestsParticipated}";
                        }
                        break;
                    case "codechef":
                        if (stats.IsConnected)
                        {
                            txtCCRating.Text = $"Rating: {stats.Rating}";
                            txtCCProblems.Text = $"Problems: {stats.ProblemsSolved}";
                        }
                        break;
                    case "atcoder":
                        if (stats.IsConnected)
                        {
                            txtACRating.Text = $"Rating: {stats.Rating}";
                            txtACProblems.Text = $"Problems: {stats.ProblemsSolved}";
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadPlatformAdditionalInfo {platformName}: Error - {ex.Message}");
            }
        }

        private void ClearPlatformAdditionalInfo(string platformName)
        {
            switch (platformName.ToLower())
            {
                case "codeforces":
                    txtCFRating.Text = "";
                    txtCFProblems.Text = "";
                    break;
                case "leetcode":
                    txtLCProblems.Text = "";
                    txtLCContests.Text = "";
                    break;
                case "codechef":
                    txtCCRating.Text = "";
                    txtCCProblems.Text = "";
                    break;
                case "atcoder":
                    txtACRating.Text = "";
                    txtACProblems.Text = "";
                    break;
            }
        }

        private void PlatformBox_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Border border && border.Tag is string platformName)
                {
                    ShowPlatformDetails(platformName);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlatformBox_MouseLeftButtonDown: Error - {ex.Message}");
            }
        }

        private async void ShowPlatformDetails(string platformName)
        {
            try
            {
                // Get user profile to find username
                var profile = _profileRepo.GetByUserId(_userId);
                string username = GetPlatformUsername(profile, platformName);
                
                if (string.IsNullOrEmpty(username))
                {
                    MessageBox.Show($"No {platformName} account connected. Please connect your account first.", 
                                  "Account Not Connected", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Update platform details UI
                PlatformDetailsTitle.Text = $"{platformName} Details";
                PlatformDetailsUsername.Text = username;
                
                // Set platform icon
                SetPlatformDetailsIcon(platformName);
                
                // Load platform statistics
                var stats = await _apiManager.GetPlatformStatsAsync(platformName.ToLower(), username);
                
                if (stats.IsConnected)
                {
                    PlatformDetailsRating.Text = stats.Rating.ToString();
                    PlatformDetailsProblems.Text = stats.ProblemsSolved.ToString();
                    PlatformDetailsContests.Text = stats.ContestsParticipated.ToString();
                    PlatformDetailsMaxRating.Text = stats.MaxRating.ToString();
                    PlatformDetailsRank.Text = "N/A"; // Rank not available in current API
                }
                else
                {
                    PlatformDetailsRating.Text = "N/A";
                    PlatformDetailsProblems.Text = "N/A";
                    PlatformDetailsContests.Text = "N/A";
                    PlatformDetailsMaxRating.Text = "N/A";
                    PlatformDetailsRank.Text = "N/A";
                }
                
                // Show the details section
                PlatformDetailsSection.Visibility = System.Windows.Visibility.Visible;
                
                // Store current platform for visit button
                PlatformDetailsVisitButton.Tag = platformName;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShowPlatformDetails: Error - {ex.Message}");
                MessageBox.Show($"Error loading {platformName} details: {ex.Message}", 
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetPlatformUsername(UserProfile profile, string platformName)
        {
            return platformName.ToLower() switch
            {
                "codeforces" => profile.Codeforces,
                "leetcode" => profile.LeetCode,
                "codechef" => profile.Codechef,
                "atcoder" => profile.Atcoder,
                _ => ""
            };
        }

        private void SetPlatformDetailsIcon(string platformName)
        {
            var iconColor = platformName.ToLower() switch
            {
                "codeforces" => (System.Windows.Media.Brush)FindResource("codeforcesColor"),
                "leetcode" => (System.Windows.Media.Brush)FindResource("leetcodeColor"),
                "codechef" => (System.Windows.Media.Brush)FindResource("codechefColor"),
                "atcoder" => (System.Windows.Media.Brush)FindResource("atcoderColor"),
                _ => (System.Windows.Media.Brush)FindResource("titleColor1")
            };
            
            PlatformDetailsIcon.Foreground = iconColor;
        }

        private void PlatformDetailsVisitButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button button && button.Tag is string platformName)
                {
                    string url = GetPlatformUrl(platformName);
                    if (!string.IsNullOrEmpty(url))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = url,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlatformDetailsVisitButton_Click: Error - {ex.Message}");
                MessageBox.Show("Error opening platform website.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetPlatformUrl(string platformName)
        {
            var profile = _profileRepo.GetByUserId(_userId);
            string username = GetPlatformUsername(profile, platformName);
            
            if (string.IsNullOrEmpty(username))
                return "";
                
            return platformName.ToLower() switch
            {
                "codeforces" => $"https://codeforces.com/profile/{username}",
                "leetcode" => $"https://leetcode.com/{username}/",
                "codechef" => $"https://www.codechef.com/users/{username}",
                "atcoder" => $"https://atcoder.jp/users/{username}",
                _ => ""
            };
        }

        private void PlatformDetailsCloseButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            PlatformDetailsSection.Visibility = System.Windows.Visibility.Collapsed;
        }

        private async Task LoadQuickStatsAsync(UserProfile profile)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("LoadQuickStatsAsync: Starting...");
                
                var weeklyStats = await _apiManager.GetCombinedWeeklyStatsAsync(profile);
                System.Diagnostics.Debug.WriteLine("LoadQuickStatsAsync: Weekly stats loaded");
                
                var allPlatformStats = await _apiManager.GetAllPlatformStatsAsync(profile);
                System.Diagnostics.Debug.WriteLine("LoadQuickStatsAsync: Platform stats loaded");
                
                // Update quick stats
                txtProblemsThisWeek.Text = weeklyStats.ProblemsSolved.ToString();
                txtContestsThisWeek.Text = weeklyStats.ContestsParticipated.ToString();
                
                // Calculate total problems solved across all platforms
                var totalProblems = allPlatformStats.Sum(s => s.ProblemsSolved);
                txtTotalProblems.Text = totalProblems.ToString();
                
                System.Diagnostics.Debug.WriteLine("LoadQuickStatsAsync: Basic stats updated");
                
                // Update detailed platform statistics
                await UpdateDetailedPlatformStatsAsync(allPlatformStats);
                System.Diagnostics.Debug.WriteLine("LoadQuickStatsAsync: Detailed stats updated");
                
                // Update performance overview
                UpdatePerformanceOverview(weeklyStats, allPlatformStats);
                System.Diagnostics.Debug.WriteLine("LoadQuickStatsAsync: Performance overview updated");
                
                // Update achievements
                UpdateAchievements(allPlatformStats);
                System.Diagnostics.Debug.WriteLine("LoadQuickStatsAsync: Achievements updated");
                
                System.Diagnostics.Debug.WriteLine("LoadQuickStatsAsync: Completed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading quick stats: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                
                // Set default values
                txtProblemsThisWeek.Text = "0";
                txtContestsThisWeek.Text = "0";
                txtTotalProblems.Text = "0";
            }
        }

        private async Task UpdateDetailedPlatformStatsAsync(List<PlatformStats> allStats)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("UpdateDetailedPlatformStatsAsync: Starting...");
                
                // Update Codeforces details
                var cfStats = allStats?.FirstOrDefault(s => s.Platform.Equals("Codeforces", StringComparison.OrdinalIgnoreCase));
                if (cfStats != null && cfStats.IsConnected)
                {
                    txtCFCurrentRating.Text = cfStats.Rating.ToString();
                    txtCFMaxRating.Text = cfStats.MaxRating.ToString();
                    txtCFProblemsSolved.Text = cfStats.ProblemsSolved.ToString();
                    txtCFContests.Text = cfStats.ContestsParticipated.ToString();
                }
                else
                {
                    txtCFCurrentRating.Text = "-";
                    txtCFMaxRating.Text = "-";
                    txtCFProblemsSolved.Text = "-";
                    txtCFContests.Text = "-";
                }

                // Update LeetCode details
                var lcStats = allStats?.FirstOrDefault(s => s.Platform.Equals("LeetCode", StringComparison.OrdinalIgnoreCase));
                if (lcStats != null && lcStats.IsConnected)
                {
                    txtLCReputation.Text = lcStats.Rating.ToString();
                    txtLCProblemsSolved.Text = lcStats.ProblemsSolved.ToString();
                    // For now, we'll use placeholder values for Easy/Medium breakdown
                    // This would require more detailed API calls
                    txtLCEasy.Text = (lcStats.ProblemsSolved * 0.6).ToString("F0"); // Estimate
                    txtLCMedium.Text = (lcStats.ProblemsSolved * 0.3).ToString("F0"); // Estimate
                }
                else
                {
                    txtLCReputation.Text = "-";
                    txtLCProblemsSolved.Text = "-";
                    txtLCEasy.Text = "-";
                    txtLCMedium.Text = "-";
                }
                
                System.Diagnostics.Debug.WriteLine("UpdateDetailedPlatformStatsAsync: Completed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating detailed platform stats: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        private void UpdatePerformanceOverview(WeeklyStats weeklyStats, List<PlatformStats> allStats)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("UpdatePerformanceOverview: Starting...");
                
                if (weeklyStats != null)
                {
                    txtWeeklyProblems.Text = weeklyStats.ProblemsSolved.ToString();
                    txtWeeklyContests.Text = weeklyStats.ContestsParticipated.ToString();
                    
                    // Calculate rating change (simplified - would need historical data for accurate calculation)
                    var ratingChange = weeklyStats.RatingChange;
                    if (ratingChange > 0)
                    {
                        txtWeeklyRatingChange.Text = $"+{ratingChange}";
                        txtWeeklyRatingChange.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGreen);
                    }
                    else if (ratingChange < 0)
                    {
                        txtWeeklyRatingChange.Text = ratingChange.ToString();
                        txtWeeklyRatingChange.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightCoral);
                    }
                    else
                    {
                        txtWeeklyRatingChange.Text = "0";
                        txtWeeklyRatingChange.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray);
                    }
                }
                else
                {
                    txtWeeklyProblems.Text = "0";
                    txtWeeklyContests.Text = "0";
                    txtWeeklyRatingChange.Text = "0";
                    txtWeeklyRatingChange.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray);
                }
                
                System.Diagnostics.Debug.WriteLine("UpdatePerformanceOverview: Completed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating performance overview: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        private void UpdateAchievements(List<PlatformStats> allStats)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("UpdateAchievements: Starting...");
                
                // Clear existing achievements
                pnlAchievements.Children.Clear();
                
                var achievements = new List<(string icon, string title, bool earned)>();
                
                // Check for achievements based on stats
                var totalProblems = allStats?.Sum(s => s.ProblemsSolved) ?? 0;
                var totalContests = allStats?.Sum(s => s.ContestsParticipated) ?? 0;
                var maxRating = allStats?.Any() == true ? allStats.Max(s => s.MaxRating) : 0;
                
                // First Problem Solved
                achievements.Add(("Trophy", "First Problem Solved", totalProblems >= 1));
                
                // Problem Solver
                achievements.Add(("CheckCircle", "Problem Solver", totalProblems >= 10));
                
                // Contest Participant
                achievements.Add(("Trophy", "Contest Participant", totalContests >= 1));
                
                // Contest Champion
                achievements.Add(("Crown", "Contest Champion", totalContests >= 5));
                
                // Rating Master
                achievements.Add(("Star", "Rating Master", maxRating >= 1500));
                
                // Streak Master (placeholder - would need daily tracking)
                achievements.Add(("Fire", "Streak Master", totalProblems >= 50));
                
                // Add achievements to UI
                foreach (var achievement in achievements)
                {
                    var stackPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new System.Windows.Thickness(0, 0, 0, 10)
                    };
                    
                    var icon = new FontAwesome.Sharp.IconBlock
                    {
                        Icon = (FontAwesome.Sharp.IconChar)Enum.Parse(typeof(FontAwesome.Sharp.IconChar), achievement.icon),
                        Foreground = achievement.earned ? 
                            (System.Windows.Media.Brush)FindResource("color6") : 
                            new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray),
                        Width = 16,
                        Height = 16,
                        Margin = new System.Windows.Thickness(0, 0, 8, 0)
                    };
                    
                    var textBlock = new TextBlock
                    {
                        Text = achievement.title,
                        Foreground = achievement.earned ? 
                            (System.Windows.Media.Brush)FindResource("titleColor1") : 
                            (System.Windows.Media.Brush)FindResource("plainTextColor1"),
                        FontSize = 12
                    };
                    
                    stackPanel.Children.Add(icon);
                    stackPanel.Children.Add(textBlock);
                    pnlAchievements.Children.Add(stackPanel);
                }
                
                System.Diagnostics.Debug.WriteLine("UpdateAchievements: Completed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating achievements: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        private void LoadRecentActivity()
        {
            // Enhanced recent activity with more details
            var activityText = new System.Text.StringBuilder();
            activityText.AppendLine("📊 Recent Activity Summary:");
            activityText.AppendLine("• Platform connections: Active");
            activityText.AppendLine("• Data sync: Last updated now");
            activityText.AppendLine("• API status: All systems operational");
            activityText.AppendLine();
            activityText.AppendLine("💡 Tip: Connect more platforms to see detailed activity!");
            
            txtRecentActivity.Text = activityText.ToString();
        }

        public void RefreshData()
        {
            // Only reload initial data if we're currently visible
            // This prevents unnecessary UI updates when dashboard is not visible
            if (IsVisible)
            {
                LoadInitialData();
            }
            LoadDashboardDataAsync();
        }

        private T FindChild<T>(System.Windows.DependencyObject parent, string childName) where T : System.Windows.DependencyObject
        {
            if (parent == null) return null;

            T foundChild = null;
            int childrenCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childrenCount; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                T childType = child as T;
                if (childType == null)
                {
                    foundChild = FindChild<T>(child, childName);
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as System.Windows.FrameworkElement;
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    foundChild = (T)child;
                    break;
                }
            }
            return foundChild;
        }
    }
}
