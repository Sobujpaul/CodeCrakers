using System.Windows;
using System.Windows.Controls;
using CodeCrakers.Data;
using CodeCrakers.Models;
using CodeCrakers.Services;
using System.Threading.Tasks;

namespace CodeCrakers.Views
{
    public partial class PlatformPage : UserControl
    {
        private int _userId;
        private UserProfileRepository _profileRepo;
        private PlatformApiManager _apiManager;

        public PlatformPage(int userId)
        {
            InitializeComponent();
            _userId = userId;
            _profileRepo = new UserProfileRepository();
            _apiManager = new PlatformApiManager();
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            var profile = _profileRepo.GetByUserId(_userId);
            
            txtCodeforces.Text = profile.Codeforces ?? "";
            txtLeetCode.Text = profile.LeetCode ?? "";
            txtCodeChef.Text = profile.Codechef ?? "";
            txtAtCoder.Text = profile.Atcoder ?? "";
        }

        private async void btnTestCodeforces_Click(object sender, RoutedEventArgs e)
        {
            string username = txtCodeforces.Text.Trim();
            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Please enter a Codeforces username first.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnTestCodeforces.IsEnabled = false;
            btnTestCodeforces.Content = "Testing...";

            try
            {
                bool isConnected = await _apiManager.TestConnectionAsync("codeforces", username);
                
                if (isConnected)
                {
                    var stats = await _apiManager.GetPlatformStatsAsync("codeforces", username);
                    MessageBox.Show($"✅ Connection successful!\n\nUsername: {stats.Username}\nRating: {stats.Rating}\nMax Rating: {stats.MaxRating}\nProblems Solved: {stats.ProblemsSolved}", 
                        "Connection Test", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"❌ Connection failed!\n\nUsername '{username}' not found on Codeforces.", 
                        "Connection Test", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Connection failed!\n\nError: {ex.Message}", 
                    "Connection Test", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnTestCodeforces.IsEnabled = true;
                btnTestCodeforces.Content = "Test";
            }
        }

        private async void btnTestLeetCode_Click(object sender, RoutedEventArgs e)
        {
            string username = txtLeetCode.Text.Trim();
            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Please enter a LeetCode username first.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnTestLeetCode.IsEnabled = false;
            btnTestLeetCode.Content = "Testing...";

            try
            {
                bool isConnected = await _apiManager.TestConnectionAsync("leetcode", username);
                
                if (isConnected)
                {
                    var stats = await _apiManager.GetPlatformStatsAsync("leetcode", username);
                    MessageBox.Show($"✅ Connection successful!\n\nUsername: {stats.Username}\nRating: {stats.Rating}\nProblems Solved: {stats.ProblemsSolved}", 
                        "Connection Test", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"❌ Connection failed!\n\nUsername '{username}' not found on LeetCode.", 
                        "Connection Test", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Connection failed!\n\nError: {ex.Message}", 
                    "Connection Test", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnTestLeetCode.IsEnabled = true;
                btnTestLeetCode.Content = "Test";
            }
        }

        private async void btnTestCodeChef_Click(object sender, RoutedEventArgs e)
        {
            string username = txtCodeChef.Text.Trim();
            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Please enter a CodeChef username first.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnTestCodeChef.IsEnabled = false;
            btnTestCodeChef.Content = "Testing...";

            try
            {
                bool isConnected = await _apiManager.TestConnectionAsync("codechef", username);
                
                if (isConnected)
                {
                    var stats = await _apiManager.GetPlatformStatsAsync("codechef", username);
                    MessageBox.Show($"✅ Connection successful!\n\nUsername: {stats.Username}\nRating: {stats.Rating}\nMax Rating: {stats.MaxRating}\nProblems Solved: {stats.ProblemsSolved}", 
                        "Connection Test", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"❌ Connection failed!\n\nUsername '{username}' not found on CodeChef.", 
                        "Connection Test", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Connection failed!\n\nError: {ex.Message}", 
                    "Connection Test", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnTestCodeChef.IsEnabled = true;
                btnTestCodeChef.Content = "Test";
            }
        }

        private async void btnTestAtCoder_Click(object sender, RoutedEventArgs e)
        {
            string username = txtAtCoder.Text.Trim();
            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Please enter an AtCoder username first.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnTestAtCoder.IsEnabled = false;
            btnTestAtCoder.Content = "Testing...";

            try
            {
                bool isConnected = await _apiManager.TestConnectionAsync("atcoder", username);
                
                if (isConnected)
                {
                    var stats = await _apiManager.GetPlatformStatsAsync("atcoder", username);
                    MessageBox.Show($"✅ Connection successful!\n\nUsername: {stats.Username}\nRating: {stats.Rating}\nMax Rating: {stats.MaxRating}\nContests Participated: {stats.ContestsParticipated}", 
                        "Connection Test", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"❌ Connection failed!\n\nUsername '{username}' not found on AtCoder.", 
                        "Connection Test", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Connection failed!\n\nError: {ex.Message}", 
                    "Connection Test", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnTestAtCoder.IsEnabled = true;
                btnTestAtCoder.Content = "Test";
            }
        }

        private void btnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string codeforces = txtCodeforces.Text.Trim();
                string leetcode = txtLeetCode.Text.Trim();
                string codechef = txtCodeChef.Text.Trim();
                string atcoder = txtAtCoder.Text.Trim();

                // Save to database
                _profileRepo.Upsert(_userId, codeforces, leetcode, codechef, atcoder);

                MessageBox.Show("✅ Platform settings saved successfully!\n\nDashboard will be updated automatically.", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Notify parent to refresh dashboard
                OnSettingsSaved?.Invoke();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"❌ Error saving settings: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Event to notify parent when settings are saved
        public System.Action OnSettingsSaved { get; set; }
    }
}
