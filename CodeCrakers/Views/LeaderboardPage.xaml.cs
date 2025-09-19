using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CodeCrakers.Data;
using CodeCrakers.Models;
using CodeCrakers.Services;

namespace CodeCrakers.Views
{
    public partial class LeaderboardPage : UserControl
    {
        private readonly PlatformApiManager _apiManager;
        private readonly UserRepository _userRepo;
        private readonly UserProfileRepository _profileRepo;

        private List<AggregatedUserStats> _allUsers = new List<AggregatedUserStats>();
        private List<AggregatedUserStats> _filtered = new List<AggregatedUserStats>();

        private int _page = 1;
        private int _pageSize = 14;
        private string _search = string.Empty;
        private string _sort = "rank"; // rank | problems | rating
        private bool _asc = false;
        private string _country = null;
        private string _university = null;

        public LeaderboardPage()
        {
            try
            {
                InitializeComponent();
                _apiManager = new PlatformApiManager();
                _userRepo = new UserRepository();
                _profileRepo = new UserProfileRepository();

                // Initialize with empty data first
                _allUsers = new List<AggregatedUserStats>();
                ApplyFiltersAndBind();

                // Load data asynchronously without blocking the UI
                Task.Run(async () =>
                {
                    try
                    {
                        await LoadDataAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"LeaderboardPage: Error loading data: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LeaderboardPage: Constructor error: {ex.Message}");
                // Initialize with minimal setup
                _allUsers = new List<AggregatedUserStats>();
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // Fetch all user IDs from DB
                var userIds = GetAllUserIds();

                if (userIds.Count == 0)
                {
                    // Create sample data for demonstration
                    _allUsers = CreateSampleData();
                    await Dispatcher.InvokeAsync(async () =>
                    {
                        await PopulateFiltersAsync();
                        ApplyFiltersAndBind();
                    });
                    return;
                }

                // Build aggregated stats in parallel
                var tasks = userIds.Select(id => _apiManager.BuildAggregatedStatsAsync(id));
                var aggregated = await Task.WhenAll(tasks);

                // Respect privacy
                _allUsers = aggregated.Where(a => a != null && !a.IsHidden).ToList();

                await Dispatcher.InvokeAsync(async () =>
                {
                    await PopulateFiltersAsync();
                    ApplyFiltersAndBind();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LeaderboardPage: Error in LoadDataAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"LeaderboardPage: Stack Trace: {ex.StackTrace}");
                
                // Initialize with empty data instead of showing error dialog
                _allUsers = new List<AggregatedUserStats>();
                await Dispatcher.InvokeAsync(async () =>
                {
                    await PopulateFiltersAsync();
                    ApplyFiltersAndBind();
                });
            }
        }

        private List<int> GetAllUserIds()
        {
            try
            {
                var ids = new List<int>();
                using var con = AppDb.GetConnection();
                con.Open();
                using var cmd = con.CreateCommand();
                cmd.CommandText = "SELECT Id FROM Users";
                using var r = cmd.ExecuteReader();
                while (r.Read()) ids.Add(r.GetInt32(0));
                return ids;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LeaderboardPage: Error getting user IDs: {ex.Message}");
                return new List<int>();
            }
        }

        private async Task PopulateFiltersAsync()
        {
            var countries = _allUsers.Select(u => u.Country).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s).ToList();
            countries.Insert(0, "All Countries");
            cmbCountry.ItemsSource = countries;
            cmbCountry.SelectedIndex = 0;

            var universities = _allUsers.Select(u => u.University).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s).ToList();
            universities.Insert(0, "All Universities");
            cmbUniversity.ItemsSource = universities;
            cmbUniversity.SelectedIndex = 0;
        }

        private void ApplyFiltersAndBind()
        {
            IEnumerable<AggregatedUserStats> q = _allUsers;

            if (!string.IsNullOrWhiteSpace(_search))
            {
                var s = _search.Trim().ToLower();
                q = q.Where(u => 
                    (u.DisplayName ?? string.Empty).ToLower().Contains(s) ||
                    (u.Codeforces ?? string.Empty).ToLower().Contains(s) ||
                    (u.LeetCode ?? string.Empty).ToLower().Contains(s) ||
                    (u.Codechef ?? string.Empty).ToLower().Contains(s) ||
                    (u.Atcoder ?? string.Empty).ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(_country))
            {
                q = q.Where(u => string.Equals(u.Country, _country, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(_university))
            {
                q = q.Where(u => string.Equals(u.University, _university, StringComparison.OrdinalIgnoreCase));
            }

            // Apply sorting
            switch (_sort)
            {
                case "rank":
                    q = _asc ? q.OrderBy(u => u.Rank) : q.OrderByDescending(u => u.Rank);
                    break;
                case "rating":
                    q = _asc ? q.OrderBy(u => u.HighestRating) : q.OrderByDescending(u => u.HighestRating);
                    break;
                case "problems":
                default:
                    q = _asc ? q.OrderBy(u => u.TotalProblemsSolved) : q.OrderByDescending(u => u.TotalProblemsSolved);
                    break;
            }

            _filtered = q.ToList();
            
            // Assign ranks based on current sorting
            for (int i = 0; i < _filtered.Count; i++)
            {
                _filtered[i].Rank = i + 1;
            }

            // Pagination
            var totalPages = Math.Max(1, (int)Math.Ceiling(_filtered.Count / (double)_pageSize));
            _page = Math.Min(_page, totalPages);
            var pageItems = _filtered.Skip((_page - 1) * _pageSize).Take(_pageSize).ToList();

            gridLeaderboard.ItemsSource = pageItems;
            txtPageInfo.Text = $"Page {_page} / {totalPages} (Total: {_filtered.Count})";
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _search = txtSearch.Text;
            _page = 1;
            ApplyFiltersAndBind();
        }

        private void cmbSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbSort.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                _sort = tag;
                _page = 1;
                ApplyFiltersAndBind();
            }
        }

        private void btnSortDir_Checked(object sender, RoutedEventArgs e)
        {
            _asc = true;
            ApplyFiltersAndBind();
        }

        private void btnSortDir_Unchecked(object sender, RoutedEventArgs e)
        {
            _asc = false;
            ApplyFiltersAndBind();
        }

        private void cmbFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            _country = cmbCountry.SelectedIndex <= 0 ? null : cmbCountry.SelectedItem as string;
            _university = cmbUniversity.SelectedIndex <= 0 ? null : cmbUniversity.SelectedItem as string;
            _page = 1;
            ApplyFiltersAndBind();
        }

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (_page > 1) { _page--; ApplyFiltersAndBind(); }
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            var totalPages = Math.Max(1, (int)Math.Ceiling(_filtered.Count / (double)_pageSize));
            if (_page < totalPages) { _page++; ApplyFiltersAndBind(); }
        }

        private void gridLeaderboard_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (gridLeaderboard.SelectedItem is AggregatedUserStats user)
            {
                var win = new UserDetailWindow(user);
                win.Owner = Window.GetWindow(this);
                win.ShowDialog();
            }
        }

        private void PlatformCell_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (gridLeaderboard.SelectedItem is AggregatedUserStats user && sender is Border b && b.Tag is string platform)
            {
                var win = new UserDetailWindow(user, platform);
                win.Owner = Window.GetWindow(this);
                win.ShowDialog();
            }
        }

        private void PlatformUsername_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (gridLeaderboard.SelectedItem is AggregatedUserStats user && sender is Border b && b.Tag is string platform)
            {
                // Create a user statistics window similar to dashboard functionality
                var win = new UserDetailWindow(user, platform);
                win.Owner = Window.GetWindow(this);
                win.ShowDialog();
            }
        }

        // Public method to refresh data when leaderboard tab is clicked
        public void RefreshData()
        {
            if (_allUsers.Count == 0)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await LoadDataAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"LeaderboardPage: Error refreshing data: {ex.Message}");
                    }
                });
            }
        }

        private List<AggregatedUserStats> CreateSampleData()
        {
            return new List<AggregatedUserStats>
            {
                new AggregatedUserStats
                {
                    UserId = 1,
                    DisplayName = "tourist",
                    Country = "Belarus",
                    University = "ITMO University",
                    HighestRating = 4000,
                    TotalProblemsSolved = 2500,
                    Codeforces = "tourist",
                    LeetCode = "tourist",
                    Codechef = "tourist",
                    Atcoder = "tourist",
                    IsHidden = false
                },
                new AggregatedUserStats
                {
                    UserId = 2,
                    DisplayName = "Petr",
                    Country = "Russia",
                    University = "ITMO University",
                    HighestRating = 3800,
                    TotalProblemsSolved = 2200,
                    Codeforces = "Petr",
                    LeetCode = "Petr",
                    Codechef = "",
                    Atcoder = "Petr",
                    IsHidden = false
                },
                new AggregatedUserStats
                {
                    UserId = 3,
                    DisplayName = "Egor",
                    Country = "Russia",
                    University = "Moscow State University",
                    HighestRating = 3600,
                    TotalProblemsSolved = 2000,
                    Codeforces = "Egor",
                    LeetCode = "",
                    Codechef = "Egor",
                    Atcoder = "",
                    IsHidden = false
                },
                new AggregatedUserStats
                {
                    UserId = 4,
                    DisplayName = "Benq",
                    Country = "USA",
                    University = "MIT",
                    HighestRating = 3500,
                    TotalProblemsSolved = 1800,
                    Codeforces = "Benq",
                    LeetCode = "Benq",
                    Codechef = "Benq",
                    Atcoder = "Benq",
                    IsHidden = false
                },
                new AggregatedUserStats
                {
                    UserId = 5,
                    DisplayName = "Um_nik",
                    Country = "Russia",
                    University = "ITMO University",
                    HighestRating = 3400,
                    TotalProblemsSolved = 1700,
                    Codeforces = "Um_nik",
                    LeetCode = "",
                    Codechef = "",
                    Atcoder = "Um_nik",
                    IsHidden = false
                },
                new AggregatedUserStats
                {
                    UserId = 6,
                    DisplayName = "jiangly",
                    Country = "China",
                    University = "Tsinghua University",
                    HighestRating = 3300,
                    TotalProblemsSolved = 1600,
                    Codeforces = "jiangly",
                    LeetCode = "jiangly",
                    Codechef = "jiangly",
                    Atcoder = "",
                    IsHidden = false
                },
                new AggregatedUserStats
                {
                    UserId = 7,
                    DisplayName = "maroonrk",
                    Country = "Japan",
                    University = "University of Tokyo",
                    HighestRating = 3200,
                    TotalProblemsSolved = 1500,
                    Codeforces = "maroonrk",
                    LeetCode = "",
                    Codechef = "",
                    Atcoder = "maroonrk",
                    IsHidden = false
                },
                new AggregatedUserStats
                {
                    UserId = 8,
                    DisplayName = "ecnerwala",
                    Country = "USA",
                    University = "Stanford University",
                    HighestRating = 3100,
                    TotalProblemsSolved = 1400,
                    Codeforces = "ecnerwala",
                    LeetCode = "ecnerwala",
                    Codechef = "",
                    Atcoder = "ecnerwala",
                    IsHidden = false
                },
                new AggregatedUserStats
                {
                    UserId = 9,
                    DisplayName = "ksun48",
                    Country = "USA",
                    University = "MIT",
                    HighestRating = 3000,
                    TotalProblemsSolved = 1300,
                    Codeforces = "ksun48",
                    LeetCode = "",
                    Codechef = "ksun48",
                    Atcoder = "",
                    IsHidden = false
                },
                new AggregatedUserStats
                {
                    UserId = 10,
                    DisplayName = "Radewoosh",
                    Country = "Poland",
                    University = "University of Warsaw",
                    HighestRating = 2900,
                    TotalProblemsSolved = 1200,
                    Codeforces = "Radewoosh",
                    LeetCode = "Radewoosh",
                    Codechef = "Radewoosh",
                    Atcoder = "Radewoosh",
                    IsHidden = false
                },
                new AggregatedUserStats
                {
                    UserId = 11,
                    DisplayName = "Errichto",
                    Country = "Poland",
                    University = "University of Warsaw",
                    HighestRating = 2800,
                    TotalProblemsSolved = 1100,
                    Codeforces = "Errichto",
                    LeetCode = "",
                    Codechef = "",
                    Atcoder = "",
                    IsHidden = false
                },
                new AggregatedUserStats
                {
                    UserId = 12,
                    DisplayName = "SecondThread",
                    Country = "USA",
                    University = "University of Illinois",
                    HighestRating = 2700,
                    TotalProblemsSolved = 1000,
                    Codeforces = "SecondThread",
                    LeetCode = "SecondThread",
                    Codechef = "SecondThread",
                    Atcoder = "",
                    IsHidden = false
                },
                new AggregatedUserStats
                {
                    UserId = 13,
                    DisplayName = "neal",
                    Country = "USA",
                    University = "Carnegie Mellon University",
                    HighestRating = 2600,
                    TotalProblemsSolved = 900,
                    Codeforces = "neal",
                    LeetCode = "neal",
                    Codechef = "",
                    Atcoder = "neal",
                    IsHidden = false
                },
                new AggregatedUserStats
                {
                    UserId = 14,
                    DisplayName = "Geothermal",
                    Country = "USA",
                    University = "MIT",
                    HighestRating = 2500,
                    TotalProblemsSolved = 800,
                    Codeforces = "Geothermal",
                    LeetCode = "",
                    Codechef = "Geothermal",
                    Atcoder = "Geothermal",
                    IsHidden = false
                },
                new AggregatedUserStats
                {
                    UserId = 15,
                    DisplayName = "Monogon",
                    Country = "USA",
                    University = "Stanford University",
                    HighestRating = 2400,
                    TotalProblemsSolved = 700,
                    Codeforces = "Monogon",
                    LeetCode = "Monogon",
                    Codechef = "",
                    Atcoder = "",
                    IsHidden = false
                }
            };
        }
    }
}


