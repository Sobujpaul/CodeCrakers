using CodeCrakers.Data;
using CodeCrakers.Services;
using CodeCrakers.Models;
using System.Windows;
using System.Windows.Controls;

namespace CodeCrakers.Views
{
    public partial class LeaderboardPage : UserControl
    {
        private LeaderboardService _leaderboardService = new LeaderboardService();
        private int currentPage = 1;
        private int pageSize = 10;

        public LeaderboardPage()
        {
            InitializeComponent();
            LoadLeaderboard();
        }

        public void LoadLeaderboard()
        {
            var sortBy = ((ComboBoxItem)cmbSort.SelectedItem)?.Tag?.ToString() ?? "rating";

            var leaderboard = _leaderboardService.GetLeaderboard(
                currentPage,
                pageSize,
                txtSearch.Text,
                txtCountry.Text,
                txtUniversity.Text,
                sortBy
            );

            dgLeaderboard.ItemsSource = leaderboard;
            txtPageInfo.Text = $"Page {currentPage}";
        }

        private void btnNext_Click(object sender, RoutedEventArgs e) { currentPage++; LoadLeaderboard(); }
        private void btnPrevious_Click(object sender, RoutedEventArgs e) { if (currentPage > 1) currentPage--; LoadLeaderboard(); }
        private void btnApplyFilters_Click(object sender, RoutedEventArgs e) { currentPage = 1; LoadLeaderboard(); }

        private void PlatformButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var platform = button.Tag.ToString();
            var user = button.DataContext as UserProfile;

            // For now, show a simple message - this would need proper implementation
            MessageBox.Show($"Platform: {platform}\nUser: {user?.DisplayName}", 
                "Platform Details", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Placeholder text functionality
        private void txtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtSearch.Text == "Search Codeforces")
            {
                txtSearch.Text = "";
                txtSearch.Foreground = System.Windows.Media.Brushes.White;
            }
        }

        private void txtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                txtSearch.Text = "Search Codeforces";
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
