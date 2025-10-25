using System.Windows;
using System.Windows.Controls;
using CodeCrakers.Models;

namespace CodeCrakers.Views
{
    public partial class PlatformDetailWindow : Window
    {
        public PlatformDetailWindow(AggregatedUserStats user, string platform)
        {
            InitializeComponent();
            txtTitle.Text = $"{user.DisplayName} - {platform}";

            PlatformStats? stats = platform.ToLower() switch
            {
                "codeforces" => user.CodeforcesStats,
                "leetcode" => user.LeetCodeStats,
                "codechef" => user.CodechefStats,
                "atcoder" => user.AtcoderStats,
                _ => null
            };

            if (stats == null)
            {
                statsPanel.Children.Add(new TextBlock { Text = "No data available.", FontWeight = FontWeights.Bold });
                return;
            }

            if (platform.ToLower() == "leetcode")
            {
                statsPanel.Children.Add(new TextBlock { Text = $"Rating: {stats.Rating}" });
                statsPanel.Children.Add(new TextBlock { Text = $"Problems Solved: {stats.ProblemsSolved}" });
            }
            else if (platform.ToLower() == "atcoder")
            {
                statsPanel.Children.Add(new TextBlock { Text = $"Rating: {stats.Rating} (Max {stats.MaxRating})" });
                statsPanel.Children.Add(new TextBlock { Text = $"Problems Solved: {stats.ProblemsSolved}" });
                statsPanel.Children.Add(new TextBlock { Text = $"Contests Participated: {stats.ContestsParticipated}" });
            }
            else
            {
                statsPanel.Children.Add(new TextBlock { Text = $"Rating: {stats.Rating} (Max {stats.MaxRating})" });
                statsPanel.Children.Add(new TextBlock { Text = $"Problems Solved: {stats.ProblemsSolved}" });
            }
        }

        // Lightweight constructor for showing quick stats from a single platform
        public PlatformDetailWindow(PlatformStats? stats, string displayName)
        {
            InitializeComponent();
            var platform = string.IsNullOrWhiteSpace(stats?.Platform) ? "Platform" : stats!.Platform;
            txtTitle.Text = $"{displayName} - {platform}";

            if (stats == null || !stats.IsConnected)
            {
                statsPanel.Children.Add(new TextBlock { Text = "No data available.", FontWeight = FontWeights.Bold });
                return;
            }

            // Brief summary across platforms
            statsPanel.Children.Add(new TextBlock { Text = $"Username: {stats.Username}" });
            statsPanel.Children.Add(new TextBlock { Text = $"Rating: {stats.Rating} (Max {stats.MaxRating})" });
            statsPanel.Children.Add(new TextBlock { Text = $"Problems Solved: {stats.ProblemsSolved}" });
            if (stats.ContestsParticipated > 0)
                statsPanel.Children.Add(new TextBlock { Text = $"Contests: {stats.ContestsParticipated}" });
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
