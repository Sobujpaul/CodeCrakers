using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using CodeCrakers.Services;
using CodeCrakers.Models;

namespace CodeCrakers.Views
{
    public partial class CfAnalyticsWindow : Window
    {
        private readonly string _username;
        private readonly CodeforcesApiService _codeforcesService;
    private DetailedCodeforcesAnalytics? _analytics;
    private List<RatingChange> _ratingHistory = new();

        public CfAnalyticsWindow(string codeforcesUsername, PlatformApiManager apiManager)
        {
            InitializeComponent();
            _username = codeforcesUsername;
            _codeforcesService = new CodeforcesApiService();
            txtTitle.Text = $"CF Analytics — {_username}";
            LoadAnalyticsAsync();
        }

        private async void LoadAnalyticsAsync()
        {
            try
            {
                loadingOverlay.Visibility = Visibility.Visible;

                // Load detailed analytics
                _analytics = await _codeforcesService.GetDetailedAnalyticsAsync(_username);
                _ratingHistory = await _codeforcesService.GetUserRatingHistoryAsync(_username);

                if (_analytics != null)
                {
                    PopulateUserInfo();
                    PopulateStatistics();
                    PopulateCharts();
                    PopulateRecentActivity();
                    PopulateProblemTags();
                    PopulateLanguageStats();
                }
                else
                {
                    ShowErrorState();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load Codeforces analytics: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ShowErrorState();
            }
            finally
            {
                loadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void PopulateUserInfo()
        {
            if (_analytics?.UserInfo != null)
            {
                var user = _analytics.UserInfo;
                txtTitle.Text = $"CF Analytics — {user.Handle}";
                txtUserInfo.Text = $"{user.FirstName} {user.LastName} • {user.Country} • {user.Organization}";
                txtRank.Text = $"Rank: {user.Rank} (Max: {user.MaxRank})";
            }
        }

        private void PopulateStatistics()
        {
            if (_analytics != null)
            {
                txtCurrentRating.Text = _analytics.UserInfo?.Rating.ToString() ?? "0";
                txtMaxRating.Text = _analytics.UserInfo?.MaxRating.ToString() ?? "0";
                txtProblems.Text = _analytics.SolvedProblems.ToString();
                
                // Use calculated contests participated from analytics (more reliable than API field)
                var contestCount = _analytics.ContestsParticipated;
                txtContests.Text = contestCount.ToString();
                
                txtAccuracy.Text = $"{_analytics.SuccessRate:F1}%";

                // Populate submission analysis
                txtAccepted.Text = _analytics.VerdictStats.GetValueOrDefault("OK", 0).ToString();
                txtWrongAnswer.Text = _analytics.VerdictStats.GetValueOrDefault("WRONG_ANSWER", 0).ToString();
                txtTimeLimit.Text = _analytics.VerdictStats.GetValueOrDefault("TIME_LIMIT_EXCEEDED", 0).ToString();
            }
        }

        private void PopulateCharts()
        {
            DrawDifficultyDistribution();
            DrawProblemTagsPieChart();
        }

        private void DrawProblemTagsPieChart()
        {
            problemTagsPieChart.Children.Clear();

            if (_analytics?.TopProblemTags == null || !_analytics.TopProblemTags.Any())
                return;

            var chartWidth = problemTagsPieChart.ActualWidth > 0 ? problemTagsPieChart.ActualWidth : 300;
            var chartHeight = problemTagsPieChart.ActualHeight > 0 ? problemTagsPieChart.ActualHeight : 180;
            var centerX = chartWidth / 2;
            var centerY = chartHeight / 2;
            var radius = Math.Min(centerX, centerY) - 20;

            var totalProblems = _analytics.TopProblemTags.Values.Sum();
            var colors = new SolidColorBrush[]
            {
                new SolidColorBrush(Color.FromRgb(52, 152, 219)),   // Bright Blue
                new SolidColorBrush(Color.FromRgb(46, 204, 113)),   // Emerald Green
                new SolidColorBrush(Color.FromRgb(155, 89, 182)),   // Amethyst Purple
                new SolidColorBrush(Color.FromRgb(241, 196, 15)),   // Sun Yellow
                new SolidColorBrush(Color.FromRgb(230, 126, 34)),   // Carrot Orange
                new SolidColorBrush(Color.FromRgb(231, 76, 60)),    // Alizarin Red
                new SolidColorBrush(Color.FromRgb(52, 73, 94)),     // Wet Asphalt
                new SolidColorBrush(Color.FromRgb(142, 68, 173)),   // Wisteria
                new SolidColorBrush(Color.FromRgb(26, 188, 156)),   // Turquoise
                new SolidColorBrush(Color.FromRgb(243, 156, 18)),   // Bright Orange
                new SolidColorBrush(Color.FromRgb(192, 57, 43)),    // Pomegranate
                new SolidColorBrush(Color.FromRgb(44, 62, 80)),     // Midnight Blue
                new SolidColorBrush(Color.FromRgb(39, 174, 96)),    // Nephritis
                new SolidColorBrush(Color.FromRgb(211, 84, 0)),     // Pumpkin
                new SolidColorBrush(Color.FromRgb(125, 60, 152)),   // Royal Purple
            };

            double startAngle = 0;
            int colorIndex = 0;
            int maxTags = Math.Min(_analytics.TopProblemTags.Count, colors.Length);

            foreach (var tag in _analytics.TopProblemTags.Take(maxTags))
            {
                var percentage = (double)tag.Value / totalProblems;
                var sweepAngle = 360 * percentage;

                if (sweepAngle > 0.5) // draw very small slices only if > 0.5 degrees
                {
                    var brush = colors[colorIndex];
                    var slice = CreatePieSlice(centerX, centerY, radius, startAngle, sweepAngle, brush);

                    // Tooltip content (tag name, count, percentage)
                    var percentText = (percentage * 100).ToString("F1");
                    slice.ToolTip = new ToolTip
                    {
                        Content = $"{tag.Key}: {tag.Value} problems ({percentText}%)",
                        Background = new SolidColorBrush(Color.FromArgb(230, 30, 30, 30)),
                        Foreground = Brushes.White,
                        Padding = new Thickness(8,4,8,4),
                        Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse
                    };

                    // Hover highlight (scale + brighter stroke)
                    slice.Cursor = System.Windows.Input.Cursors.Hand;
                    var originalStroke = slice.Stroke;
                    var originalThickness = slice.StrokeThickness;
                    slice.RenderTransformOrigin = new Point(0.5, 0.5);
                    var scale = new ScaleTransform(1.0, 1.0);
                    slice.RenderTransform = scale;

                    slice.MouseEnter += (s, e) =>
                    {
                        scale.ScaleX = 1.07;
                        scale.ScaleY = 1.07;
                        slice.Stroke = new SolidColorBrush(Colors.Gold);
                        slice.StrokeThickness = 2.0;
                    };
                    slice.MouseLeave += (s, e) =>
                    {
                        scale.ScaleX = 1.0;
                        scale.ScaleY = 1.0;
                        slice.Stroke = originalStroke;
                        slice.StrokeThickness = originalThickness;
                    };

                    problemTagsPieChart.Children.Add(slice);
                }

                startAngle += sweepAngle;
                colorIndex++;
            }
        }

        private Path CreatePieSlice(double centerX, double centerY, double radius, double startAngle, double sweepAngle, Brush fill)
        {
            var startAngleRad = startAngle * Math.PI / 180;
            var endAngleRad = (startAngle + sweepAngle) * Math.PI / 180;

            var x1 = centerX + radius * Math.Cos(startAngleRad);
            var y1 = centerY + radius * Math.Sin(startAngleRad);
            var x2 = centerX + radius * Math.Cos(endAngleRad);
            var y2 = centerY + radius * Math.Sin(endAngleRad);

            var largeArcFlag = sweepAngle > 180 ? 1 : 0;

            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure
            {
                StartPoint = new Point(centerX, centerY),
                IsClosed = true
            };

            pathFigure.Segments.Add(new LineSegment(new Point(x1, y1), true));
            pathFigure.Segments.Add(new ArcSegment(
                new Point(x2, y2),
                new Size(radius, radius),
                0,
                largeArcFlag == 1,
                SweepDirection.Clockwise,
                true
            ));
            pathFigure.Segments.Add(new LineSegment(new Point(centerX, centerY), true));

            pathGeometry.Figures.Add(pathFigure);

            return new Path
            {
                Data = pathGeometry,
                Fill = fill,
                Stroke = Brushes.White,
                StrokeThickness = 1
            };
        }

        private void DrawDifficultyDistribution()
        {
            difficultyGrid.Children.Clear();
            difficultyGrid.RowDefinitions.Clear();
            difficultyGrid.ColumnDefinitions.Clear();

            if (_analytics?.DifficultyDistribution == null || !_analytics.DifficultyDistribution.Any())
                return;

            var sortedDifficulties = _analytics.DifficultyDistribution
                .OrderBy(kvp => kvp.Key)
                .ToList();

            var maxCount = sortedDifficulties.Max(kvp => kvp.Value);
            var highestRating = sortedDifficulties.Max(kvp => kvp.Key);

            // Get color based on difficulty rating according to your exact scheme
            SolidColorBrush GetDifficultyColor(int rating)
            {
                return rating switch
                {
                    >= 800 and <= 1100 => new SolidColorBrush(Colors.Gray),
                    >= 1200 and <= 1300 => new SolidColorBrush(Colors.Green),
                    >= 1400 and <= 1500 => new SolidColorBrush(Colors.Cyan),
                    >= 1600 and <= 1800 => new SolidColorBrush(Colors.Blue),
                    >= 1900 and <= 2100 => new SolidColorBrush(Colors.Violet),
                    >= 2200 and <= 2300 => new SolidColorBrush(Colors.Orange),
                    >= 2400 and <= 2800 => new SolidColorBrush(Colors.Red),
                    >= 2900 and <= 3500 => new SolidColorBrush(Colors.Red), // Red with white ray will be handled separately
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }

            // Create responsive difficulty range - only show from 800 to highest solved
            var responsiveDifficulties = new List<int>();
            for (int rating = 800; rating <= highestRating; rating += 100)
            {
                responsiveDifficulties.Add(rating);
            }

            // Filter difficulties to only show solved problems within the responsive range
            var filteredDifficulties = sortedDifficulties
                .Where(kvp => responsiveDifficulties.Contains(kvp.Key))
                .ToList();

            difficultyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Header row
            difficultyGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 8),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var highestLabel = new TextBlock
            {
                Text = $"Highest Rating Solved: {highestRating}",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = GetDifficultyColor(highestRating),
                VerticalAlignment = VerticalAlignment.Center
            };
            if (highestRating >= 2900)
            {
                highestLabel.Effect = new DropShadowEffect
                {
                    Color = Colors.White,
                    BlurRadius = 6,
                    ShadowDepth = 0,
                    Opacity = 0.9
                };
            }
            headerPanel.Children.Add(highestLabel);
            Grid.SetRow(headerPanel, 0);
            difficultyGrid.Children.Add(headerPanel);

            // Bar rows (limit 12)
            int maxBars = Math.Min(filteredDifficulties.Count, 12);
            for (int i = 0; i < maxBars; i++)
            {
                difficultyGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var diffKvp = filteredDifficulties[i];
                var percentage = (double)diffKvp.Value / maxCount;
                var difficultyColor = GetDifficultyColor(diffKvp.Key);

                var stackPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 2, 0, 2)
                };

                var label = new TextBlock
                {
                    Text = diffKvp.Key.ToString(),
                    Width = 50,
                    FontSize = 12,
                    Foreground = difficultyColor,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeights.Bold
                };

                var bar = new Rectangle
                {
                    Height = 14,
                    Width = Math.Max(percentage * 140, 4), // slightly wider scaling
                    Fill = difficultyColor,
                    Margin = new Thickness(5, 0, 5, 0),
                    RadiusX = 7,
                    RadiusY = 7
                };
                if (diffKvp.Key >= 2900)
                {
                    bar.Effect = new DropShadowEffect
                    {
                        Color = Colors.White,
                        BlurRadius = 8,
                        ShadowDepth = 0,
                        Opacity = 0.8
                    };
                }

                var count = new TextBlock
                {
                    Text = diffKvp.Value.ToString(),
                    FontSize = 11,
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeights.Medium
                };

                stackPanel.Children.Add(label);
                stackPanel.Children.Add(bar);
                stackPanel.Children.Add(count);
                Grid.SetRow(stackPanel, i + 1); // shift by 1 for header row
                difficultyGrid.Children.Add(stackPanel);
            }
        }

        private void PopulateRecentActivity()
        {
            recentActivity.Children.Clear();

            if (_analytics?.RecentSubmissions != null)
            {
                foreach (var submission in _analytics.RecentSubmissions.Take(10))
                {
                    var submissionPanel = new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                        CornerRadius = new CornerRadius(5),
                        Padding = new Thickness(8),
                        Margin = new Thickness(0, 2, 0, 2)
                    };

                    var content = new StackPanel();

                    var problemName = new TextBlock
                    {
                        Text = submission.ProblemName,
                        FontWeight = FontWeights.Medium,
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.FromRgb(52, 152, 219)), // Make it look like a link
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        Cursor = System.Windows.Input.Cursors.Hand,
                        TextDecorations = TextDecorations.Underline
                    };

                    // Make the problem name clickable
                    problemName.MouseLeftButtonDown += (s, e) =>
                    {
                        try
                        {
                            var problemUrl = $"https://codeforces.com/contest/{submission.ContestId}/problem/{submission.ProblemIndex}";
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = problemUrl,
                                UseShellExecute = true
                            });
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Unable to open problem: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    };

                    var details = new TextBlock
                    {
                        Text = $"{submission.Verdict} • {submission.Language} • {submission.SubmissionTime:MM/dd HH:mm}",
                        FontSize = 9,
                        Foreground = submission.Verdict == "OK" 
                            ? new SolidColorBrush(Color.FromRgb(46, 204, 113))
                            : new SolidColorBrush(Color.FromRgb(231, 76, 60))
                    };

                    content.Children.Add(problemName);
                    content.Children.Add(details);
                    submissionPanel.Child = content;
                    recentActivity.Children.Add(submissionPanel);
                }
            }
        }

        private void PopulateProblemTags()
        {
            problemTags.Children.Clear();

            if (_analytics?.TopProblemTags != null)
            {
                // Use the same colors as the pie chart for consistency
                var colors = new SolidColorBrush[]
                {
                    new SolidColorBrush(Color.FromRgb(52, 152, 219)),   // Bright Blue
                    new SolidColorBrush(Color.FromRgb(46, 204, 113)),   // Emerald Green
                    new SolidColorBrush(Color.FromRgb(155, 89, 182)),   // Amethyst Purple
                    new SolidColorBrush(Color.FromRgb(241, 196, 15)),   // Sun Yellow
                    new SolidColorBrush(Color.FromRgb(230, 126, 34)),   // Carrot Orange
                    new SolidColorBrush(Color.FromRgb(231, 76, 60)),    // Alizarin Red
                    new SolidColorBrush(Color.FromRgb(52, 73, 94)),     // Wet Asphalt
                    new SolidColorBrush(Color.FromRgb(142, 68, 173)),   // Wisteria
                    new SolidColorBrush(Color.FromRgb(26, 188, 156)),   // Turquoise
                    new SolidColorBrush(Color.FromRgb(243, 156, 18)),   // Bright Orange
                    new SolidColorBrush(Color.FromRgb(192, 57, 43)),    // Pomegranate
                    new SolidColorBrush(Color.FromRgb(44, 62, 80)),     // Midnight Blue
                    new SolidColorBrush(Color.FromRgb(39, 174, 96)),    // Nephritis
                    new SolidColorBrush(Color.FromRgb(211, 84, 0)),     // Pumpkin
                    new SolidColorBrush(Color.FromRgb(125, 60, 152)),   // Royal Purple
                };

                int colorIndex = 0;
                int maxTags = Math.Min(_analytics.TopProblemTags.Count, colors.Length);
                foreach (var tag in _analytics.TopProblemTags.Take(maxTags))
                {
                var tagButton = new Button
                {
                    Content = $"{tag.Key} ({tag.Value})",
                    Background = colors[colorIndex],
                    Foreground = Brushes.White,
                    BorderBrush = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(8, 4, 8, 4),
                    Margin = new Thickness(2),
                    FontSize = 10,
                    FontWeight = FontWeights.Medium,
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                // Apply rounded corners using a simple style instead of template
                var style = new Style(typeof(Button));
                style.Setters.Add(new Setter(Button.TemplateProperty, tagButton.Template));
                
                // Create a simple border for rounded appearance
                var border = new Border
                {
                    Background = colors[colorIndex],
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(8, 4, 8, 4),
                    Margin = new Thickness(2),
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                
                var textBlock = new TextBlock
                {
                    Text = $"{tag.Key} ({tag.Value})",
                    Foreground = Brushes.White,
                    FontSize = 10,
                    FontWeight = FontWeights.Medium,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                
                border.Child = textBlock;
                
                // Add the border instead of the button to avoid template issues
                problemTags.Children.Add(border);
                    colorIndex++;
                }
            }
        }

        private void PopulateLanguageStats()
        {
            languageStats.Children.Clear();

            if (_analytics?.LanguageStats != null)
            {
                var maxCount = _analytics.LanguageStats.Values.Max();
                
                foreach (var lang in _analytics.LanguageStats.Take(5))
                {
                    var langPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(0, 3, 0, 3)
                    };

                    var langName = new TextBlock
                    {
                        Text = lang.Key,
                        Width = 80,
                        FontSize = 11,
                        Foreground = Brushes.White,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    var percentage = (double)lang.Value / maxCount;
                    var langBar = new Rectangle
                    {
                        Height = 10,
                        Width = percentage * 100,
                        Fill = new SolidColorBrush(Color.FromRgb(52, 152, 219)),
                        Margin = new Thickness(5, 0, 5, 0)
                    };

                    var langCount = new TextBlock
                    {
                        Text = lang.Value.ToString(),
                        FontSize = 11,
                        Foreground = Brushes.LightGray,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    langPanel.Children.Add(langName);
                    langPanel.Children.Add(langBar);
                    langPanel.Children.Add(langCount);
                    languageStats.Children.Add(langPanel);
                }
            }
        }

        private void ShowErrorState()
        {
            txtCurrentRating.Text = "-";
            txtMaxRating.Text = "-";
            txtContests.Text = "-";
            txtProblems.Text = "-";
            txtAccuracy.Text = "-";
            txtAccepted.Text = "-";
            txtWrongAnswer.Text = "-";
            txtTimeLimit.Text = "-";
        }

        private string GetProblemIndex(string problemName)
        {
            // Try to extract problem index from problem name
            // This is a simple heuristic - in real implementation, you'd store the problem index
            var parts = problemName.Split(' ', '.');
            foreach (var part in parts)
            {
                if (part.Length == 1 && char.IsLetter(part[0]))
                {
                    return part.ToUpper();
                }
            }
            return "A"; // Default fallback
        }

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadAnalyticsAsync();
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


