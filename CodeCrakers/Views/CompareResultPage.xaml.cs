using System;
using System.Threading.Tasks;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CodeCrakers.Services;

namespace CodeCrakers.Views
{
    public partial class CompareResultPage : UserControl
    {
        private readonly string _handle1;
        private readonly string _handle2;
        private readonly CodeforcesApiService _cfService = new();

        public event Action? OnBackRequested;

        public CompareResultPage(string handle1, string handle2)
        {
            _handle1 = handle1;
            _handle2 = handle2;
            InitializeComponent();
            txtTitle.Text = $"{_handle1}  vs  {_handle2}";
            Loaded += CompareResultPage_Loaded;
        }

        private async void CompareResultPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                txtStatus.Text = "Loading...";
                var t1 = _cfService.GetDetailedAnalyticsAsync(_handle1);
                var t2 = _cfService.GetDetailedAnalyticsAsync(_handle2);
                await Task.WhenAll(t1, t2);

                var a1 = t1.Result;
                var a2 = t2.Result;

                if (a1 == null || a2 == null)
                {
                    txtStatus.Text = "Error loading data";
                    return;
                }

                // Left side
                txtHandle1.Text = a1.UserInfo.Handle;
                SetRatingText(txtRating1, a1.UserInfo.Rating);
                txtMaxRating1.Text = a1.UserInfo.MaxRating.ToString();
                txtRank1.Text = a1.UserInfo.Rank;
                txtMaxRank1.Text = a1.UserInfo.MaxRank;
                txtSolved1.Text = a1.SolvedProblems.ToString();
                txtContests1.Text = a1.ContestsParticipated.ToString();
                txtSuccess1.Text = a1.SuccessRate.ToString("F1");
                txtSubmissions1.Text = a1.TotalSubmissions.ToString();
                txtFriends1.Text = a1.UserInfo.FriendOfCount.ToString();
                txtLastActivity1.Text = $"Last Activity: {a1.LastActivity:g}";

                // Right side
                txtHandle2.Text = a2.UserInfo.Handle;
                SetRatingText(txtRating2, a2.UserInfo.Rating);
                txtMaxRating2.Text = a2.UserInfo.MaxRating.ToString();
                txtRank2.Text = a2.UserInfo.Rank;
                txtMaxRank2.Text = a2.UserInfo.MaxRank;
                txtSolved2.Text = a2.SolvedProblems.ToString();
                txtContests2.Text = a2.ContestsParticipated.ToString();
                txtSuccess2.Text = a2.SuccessRate.ToString("F1");
                txtSubmissions2.Text = a2.TotalSubmissions.ToString();
                txtFriends2.Text = a2.UserInfo.FriendOfCount.ToString();
                txtLastActivity2.Text = $"Last Activity: {a2.LastActivity:g}";

                // Differences
                SetDiff(diffRating, a1.UserInfo.Rating, a2.UserInfo.Rating);
                SetDiff(diffMaxRating, a1.UserInfo.MaxRating, a2.UserInfo.MaxRating);
                SetDiff(diffSolved, a1.SolvedProblems, a2.SolvedProblems);
                SetDiff(diffContests, a1.ContestsParticipated, a2.ContestsParticipated);
                SetDiff(diffSuccess, a1.SuccessRate, a2.SuccessRate, "F1");
                SetDiff(diffSubmissions, a1.TotalSubmissions, a2.TotalSubmissions);
                SetDiff(diffFriends, a1.UserInfo.FriendOfCount, a2.UserInfo.FriendOfCount);

                txtSummary.Text = BuildSummary(a1, a2);

                // Extra info
                SetExtraInfo(a1, a2);

                // Tags overlap & listing
                txtTagsOverlap.Text = BuildTagsOverlap(a1, a2);

                // Difficulty distribution string
                txtDifficulty.Text = BuildDifficultyDistribution(a1, a2);

                // Removed Problem Solving Efficiency section and related metrics fetch to simplify the layout

                txtStatus.Text = "";
            }
            catch (Exception ex)
            {
                txtStatus.Text = "Failed";
                System.Diagnostics.Debug.WriteLine($"CompareResultPage: LoadData failed - {ex.Message}");
            }
        }
        private void SetDiff(System.Windows.Controls.TextBlock tb, double v1, double v2, string format = "F0", bool higherIsBetter = true)
        {
            double diff = v1 - v2;
            string formatted = diff.ToString(format);
            if (diff > 0) formatted = "+" + formatted;
            tb.Text = formatted;
            bool positiveMeaningBetter = higherIsBetter ? diff > 0 : diff < 0;
            // Keep inherited foreground (white); use FontWeight only
            tb.FontWeight = FontWeights.SemiBold;
        }

        private string BuildSummary(Models.DetailedCodeforcesAnalytics a1, Models.DetailedCodeforcesAnalytics a2)
        {
            string leadRating = a1.UserInfo.Rating == a2.UserInfo.Rating ? "Both have the same rating" : (a1.UserInfo.Rating > a2.UserInfo.Rating ? $"{a1.UserInfo.Handle} leads in rating" : $"{a2.UserInfo.Handle} leads in rating");
            string solvedLead = a1.SolvedProblems == a2.SolvedProblems ? "equal solved count" : (a1.SolvedProblems > a2.SolvedProblems ? $"{a1.UserInfo.Handle} solved more problems" : $"{a2.UserInfo.Handle} solved more problems");
            return $"Summary: {leadRating}; {solvedLead}.";
        }

        private void SetExtraInfo(Models.DetailedCodeforcesAnalytics a1, Models.DetailedCodeforcesAnalytics a2)
        {
            txtCountry1.Text = string.IsNullOrWhiteSpace(a1.UserInfo.Country) ? "-" : a1.UserInfo.Country;
            txtCountry2.Text = string.IsNullOrWhiteSpace(a2.UserInfo.Country) ? "-" : a2.UserInfo.Country;
            txtOrg1.Text = string.IsNullOrWhiteSpace(a1.UserInfo.Organization) ? "-" : a1.UserInfo.Organization;
            txtOrg2.Text = string.IsNullOrWhiteSpace(a2.UserInfo.Organization) ? "-" : a2.UserInfo.Organization;
            txtContribution1.Text = a1.UserInfo.Contribution.ToString();
            txtContribution2.Text = a2.UserInfo.Contribution.ToString();
            txtReg1.Text = DateTimeOffset.FromUnixTimeSeconds(a1.UserInfo.RegistrationTimeSeconds).DateTime.ToString("yyyy-MM-dd");
            txtReg2.Text = DateTimeOffset.FromUnixTimeSeconds(a2.UserInfo.RegistrationTimeSeconds).DateTime.ToString("yyyy-MM-dd");
        }

        private string BuildTagsOverlap(Models.DetailedCodeforcesAnalytics a1, Models.DetailedCodeforcesAnalytics a2)
        {
            var tags1 = a1.TopProblemTags.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var tags2 = a2.TopProblemTags.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var overlap = tags1.Intersect(tags2, StringComparer.OrdinalIgnoreCase).OrderBy(t => t).ToList();
            var only1 = tags1.Except(tags2, StringComparer.OrdinalIgnoreCase).OrderBy(t => t).ToList();
            var only2 = tags2.Except(tags1, StringComparer.OrdinalIgnoreCase).OrderBy(t => t).ToList();

            string FormatList(string title, IEnumerable<string> list) => list.Any() ? $"{title}: {string.Join(", ", list)}" : $"{title}: (none)";

            return string.Join("\n", new[]
            {
                $"Overlap ({overlap.Count}): {(overlap.Count==0 ? "(none)" : string.Join(", ", overlap))}",
                FormatList(a1.UserInfo.Handle + " only", only1),
                FormatList(a2.UserInfo.Handle + " only", only2)
            });
        }

        private string BuildDifficultyDistribution(Models.DetailedCodeforcesAnalytics a1, Models.DetailedCodeforcesAnalytics a2)
        {
            var allBuckets = a1.DifficultyDistribution.Keys.Union(a2.DifficultyDistribution.Keys).OrderBy(k => k).ToList();
            if (allBuckets.Count == 0) return "No difficulty data available.";

            // Header
            var lines = new System.Collections.Generic.List<string> { "Bucket  User1  User2  Δ" };
            foreach (var b in allBuckets)
            {
                a1.DifficultyDistribution.TryGetValue(b, out int c1);
                a2.DifficultyDistribution.TryGetValue(b, out int c2);
                int diff = c1 - c2;
                string diffStr = diff > 0 ? "+" + diff : diff.ToString();
                lines.Add(string.Format("{0,-6} {1,5} {2,6} {3,3}", b, c1, c2, diffStr));
            }
            return string.Join("\n", lines);
        }

        private void SetRatingText(TextBlock tb, int rating)
        {
            tb.Text = rating.ToString();
            // Keep foreground inherited (white) and emphasize weight
            tb.FontWeight = FontWeights.Bold;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            OnBackRequested?.Invoke();
        }
    }
}
