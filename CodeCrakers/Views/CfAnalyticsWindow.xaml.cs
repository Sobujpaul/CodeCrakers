using System;
using System.Windows;
using CodeCrakers.Services;

namespace CodeCrakers.Views
{
    public partial class CfAnalyticsWindow : Window
    {
        private readonly string _username;
        public CfAnalyticsWindow(string codeforcesUsername, PlatformApiManager apiManager)
        {
            InitializeComponent();
            _username = codeforcesUsername;
            txtTitle.Text = $"CF Analytics — {_username}";
            LoadAsync(apiManager);
        }

        private async void LoadAsync(PlatformApiManager apiManager)
        {
            try
            {
                var stats = await apiManager.GetPlatformStatsAsync("codeforces", _username);
                if (stats != null && stats.IsConnected)
                {
                    txtCurrentRating.Text = stats.Rating.ToString();
                    txtMaxRating.Text = stats.MaxRating.ToString();
                    txtContests.Text = stats.ContestsParticipated.ToString();
                    txtProblems.Text = stats.ProblemsSolved.ToString();
                }
                else
                {
                    txtCurrentRating.Text = "-";
                    txtMaxRating.Text = "-";
                    txtContests.Text = "-";
                    txtProblems.Text = "-";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load Codeforces analytics: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnOpenProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = $"https://codeforces.com/profile/{_username}",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open Codeforces profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}


