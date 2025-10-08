using CodeCrakers.Data;
using CodeCrakers.Services;
using CodeCrakers.Models;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System;

namespace CodeCrakers.Views
{
    public partial class LeaderboardPage : UserControl
    {
        private LeaderboardService _leaderboardService = new LeaderboardService();
        private int currentPage = 1;
        private int pageSize = 10;
        private string currentUsername = "Anonymous"; // You can set this from the logged-in user

        public LeaderboardPage()
        {
            InitializeComponent();
            LoadLeaderboard();
        }

        public void LoadLeaderboard()
        {
            var sortBy = ((ComboBoxItem)cmbSort.SelectedItem)?.Tag?.ToString() ?? "currentRating";

            // Handle placeholder text
            var searchText = (txtSearch.Text == "Search Name") ? "" : txtSearch.Text;
            var countryText = (txtCountry.Text == "Country") ? "" : txtCountry.Text;
            var universityText = (txtUniversity.Text == "University") ? "" : txtUniversity.Text;

            var leaderboard = _leaderboardService.GetLeaderboard(
                currentPage,
                pageSize,
                searchText,
                countryText,
                universityText,
                sortBy
            );

            dgLeaderboard.ItemsSource = leaderboard;
            txtPageInfo.Text = $"Page {currentPage}";
        }

        private void btnNext_Click(object sender, RoutedEventArgs e) { currentPage++; LoadLeaderboard(); }
        private void btnPrevious_Click(object sender, RoutedEventArgs e) { if (currentPage > 1) currentPage--; LoadLeaderboard(); }
        private void btnApplyFilters_Click(object sender, RoutedEventArgs e) { currentPage = 1; LoadLeaderboard(); }
        private void btnRefreshAll_Click(object sender, RoutedEventArgs e)
        {
            // Reset placeholders
            if (string.IsNullOrWhiteSpace(txtSearch.Text) || txtSearch.Text != "Search Name") { txtSearch.Text = "Search Name"; txtSearch.Foreground = System.Windows.Media.Brushes.Gray; }
            if (string.IsNullOrWhiteSpace(txtCountry.Text) || txtCountry.Text != "Country") { txtCountry.Text = "Country"; txtCountry.Foreground = System.Windows.Media.Brushes.Gray; }
            if (string.IsNullOrWhiteSpace(txtUniversity.Text) || txtUniversity.Text != "University") { txtUniversity.Text = "University"; txtUniversity.Foreground = System.Windows.Media.Brushes.Gray; }

            // Reset sorting and paging
            currentPage = 1;
            foreach (var item in cmbSort.Items)
            {
                if (item is ComboBoxItem cbi && (string)cbi.Tag == "currentRating")
                {
                    cbi.IsSelected = true;
                    break;
                }
            }

            // Reload leaderboard
            LoadLeaderboard();
        }

        private void PlatformButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var platform = button.Tag?.ToString();
            var leaderboardEntry = button.DataContext as LeaderboardEntry;

            if (leaderboardEntry == null || platform == null) return;

            string username = platform.ToLower() switch
            {
                "codeforces" => leaderboardEntry.CodeforcesUsername,
                "leetcode" => leaderboardEntry.LeetCodeUsername,
                "codechef" => leaderboardEntry.CodeChefUsername,
                "atcoder" => leaderboardEntry.AtCoderUsername,
                _ => null
            };

            if (!string.IsNullOrEmpty(username))
            {
                if (platform.Equals("codeforces", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var analytics = new CfAnalyticsWindow(username, new PlatformApiManager())
                        {
                            Owner = Window.GetWindow(this)
                        };
                        analytics.ShowDialog();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Unable to open CF Analytics: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    var userType = leaderboardEntry.IsExternal ? "External User" : "Registered User";
                    MessageBox.Show($"Platform: {platform}\nUsername: {username}\nUser: {leaderboardEntry.Name}\nType: {userType}",
                        "Platform Details", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private async void btnAddExternal_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(txtDisplayName.Text))
                {
                    ShowStatus("Please enter a display name.", false);
                    return;
                }

                var codeforces = string.IsNullOrWhiteSpace(txtCodeforcesUsername.Text) ? null : txtCodeforcesUsername.Text.Trim();
                var leetcode = string.IsNullOrWhiteSpace(txtLeetCodeUsername.Text) ? null : txtLeetCodeUsername.Text.Trim();
                var codechef = string.IsNullOrWhiteSpace(txtCodeChefUsername.Text) ? null : txtCodeChefUsername.Text.Trim();
                var atcoder = string.IsNullOrWhiteSpace(txtAtCoderUsername.Text) ? null : txtAtCoderUsername.Text.Trim();
                var country = string.IsNullOrWhiteSpace(txtNewCountry.Text) ? null : txtNewCountry.Text.Trim();
                var university = string.IsNullOrWhiteSpace(txtNewUniversity.Text) ? null : txtNewUniversity.Text.Trim();

                if (codeforces == null && leetcode == null && codechef == null && atcoder == null)
                {
                    ShowStatus("Please enter at least one platform username.", false);
                    return;
                }

                btnAddExternal.IsEnabled = false;
                ShowStatus("Adding user and fetching stats...", true);

                // Add external user
                await _leaderboardService.AddExternalUserAsync(
                    txtDisplayName.Text.Trim(),
                    codeforces,
                    leetcode,
                    codechef,
                    atcoder,
                    country,
                    university,
                    currentUsername
                );

                // Clear form
                ClearAddUserForm();
                
                // Refresh leaderboard
                LoadLeaderboard();
                
                ShowStatus($"Successfully added {txtDisplayName.Text} to leaderboard!", true);
                
                // Hide status after 3 seconds
                await Task.Delay(3000);
                txtAddStatus.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                ShowStatus($"Error adding user: {ex.Message}", false);
            }
            finally
            {
                btnAddExternal.IsEnabled = true;
            }
        }

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var leaderboardEntry = button?.DataContext as LeaderboardEntry;

            if (leaderboardEntry?.IsExternal == true)
            {
                try
                {
                    button.IsEnabled = false;
                    await _leaderboardService.RefreshExternalUserStatsAsync(leaderboardEntry.Id);
                    LoadLeaderboard();
                    MessageBox.Show($"Stats refreshed for {leaderboardEntry.Name}", "Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error refreshing stats: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                finally
                {
                    button.IsEnabled = true;
                }
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var leaderboardEntry = button?.DataContext as LeaderboardEntry;

            if (leaderboardEntry?.IsExternal == true)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to remove {leaderboardEntry.Name} from the leaderboard?",
                    "Confirm Deletion",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _leaderboardService.RemoveExternalUser(leaderboardEntry.Id);
                        LoadLeaderboard();
                        MessageBox.Show($"{leaderboardEntry.Name} has been removed from the leaderboard.", 
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error removing user: {ex.Message}", "Error", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var entry = button?.DataContext as LeaderboardEntry;
            if (entry == null || entry.IsExternal == false)
                return;

            var repo = new ExternalUserRepository();
            var external = repo.GetById(entry.Id);
            if (external == null)
            {
                MessageBox.Show("External user not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dlg = new ExternalUserEditWindow(external)
            {
                Owner = Window.GetWindow(this)
            };
            if (dlg.ShowDialog() == true)
            {
                // Prevent duplicate platform usernames
                if (repo.ExistsWithSamePlatformUsernames(dlg.ExternalUser))
                {
                    MessageBox.Show("Another user already uses one of these platform usernames.", "Conflict", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                repo.Update(dlg.ExternalUser);
                LoadLeaderboard();
                MessageBox.Show("User updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ClearAddUserForm()
        {
            txtDisplayName.Clear();
            txtCodeforcesUsername.Clear();
            txtLeetCodeUsername.Clear();
            txtCodeChefUsername.Clear();
            txtAtCoderUsername.Clear();
            txtNewCountry.Clear();
            txtNewUniversity.Clear();
        }

        private void ShowStatus(string message, bool isSuccess)
        {
            txtAddStatus.Text = message;
            txtAddStatus.Foreground = isSuccess ? 
                System.Windows.Media.Brushes.LightGreen : 
                System.Windows.Media.Brushes.LightCoral;
            txtAddStatus.Visibility = Visibility.Visible;
        }

        // Placeholder text functionality
        private void txtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtSearch.Text == "Search Name")
            {
                txtSearch.Text = "";
                txtSearch.Foreground = System.Windows.Media.Brushes.White;
            }
        }

        private void txtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                txtSearch.Text = "Search Name";
                txtSearch.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void txtCountry_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtCountry.Text == "Country")
            {
                txtCountry.Text = "";
                txtCountry.Foreground = System.Windows.Media.Brushes.White;
            }
        }

        private void txtCountry_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCountry.Text))
            {
                txtCountry.Text = "Country";
                txtCountry.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void txtUniversity_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtUniversity.Text == "University")
            {
                txtUniversity.Text = "";
                txtUniversity.Foreground = System.Windows.Media.Brushes.White;
            }
        }

        private void txtUniversity_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUniversity.Text))
            {
                txtUniversity.Text = "University";
                txtUniversity.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }
    }
}
