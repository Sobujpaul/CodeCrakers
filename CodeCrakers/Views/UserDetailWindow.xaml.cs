using System;
using System.Windows;
using CodeCrakers.Models;

namespace CodeCrakers.Views
{
    public partial class UserDetailWindow : Window
    {
        public UserDetailWindow(AggregatedUserStats user, string focusPlatform = null)
        {
            InitializeComponent();

            txtTitle.Text = user.DisplayName;

            cfRating.Text = $"Rating: {user.CodeforcesStats?.Rating ?? 0} (Max {user.CodeforcesStats?.MaxRating ?? 0})";
            cfSolved.Text = $"Solved: {user.CodeforcesStats?.ProblemsSolved ?? 0}";

            lcRating.Text = $"Rating: {user.LeetCodeStats?.Rating ?? 0}";
            lcSolved.Text = $"Solved: {user.LeetCodeStats?.ProblemsSolved ?? 0}";

            ccRating.Text = $"Rating: {user.CodechefStats?.Rating ?? 0} (Max {user.CodechefStats?.MaxRating ?? 0})";
            ccSolved.Text = $"Solved: {user.CodechefStats?.ProblemsSolved ?? 0}";

            acRating.Text = $"Rating: {user.AtcoderStats?.Rating ?? 0} (Max {user.AtcoderStats?.MaxRating ?? 0})";
            acSolved.Text = $"Solved: {user.AtcoderStats?.ProblemsSolved ?? 0}";

            summary.Text = $"Total Solved: {user.TotalProblemsSolved}  |  Highest Rating: {user.HighestRating}";
        }
    }
}



