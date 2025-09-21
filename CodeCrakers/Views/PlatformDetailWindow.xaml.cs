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

            PlatformStats stats = platform.ToLower() switch
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

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
