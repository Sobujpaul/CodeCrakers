using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CodeCrakers.Data;
using CodeCrakers.Models;
using CodeCrakers.Services;

namespace CodeCrakers.Views
{
    public partial class SuggestionPage : UserControl
    {
        private readonly int _userId;
        private readonly UserProfileRepository _profileRepo = new();
        private readonly CodeforcesApiService _cfService = new();
        private readonly System.Windows.Threading.DispatcherTimer _autoTimer = new();

        public ICommand OpenProblemCommand { get; }

        public SuggestionPage(int userId)
        {
            InitializeComponent();
            _userId = userId;
            OpenProblemCommand = new RelayCommand<Problem>(OpenProblemInBrowser);
            this.DataContext = this;
            this.Loaded += SuggestionPage_Loaded;

            _autoTimer.Interval = TimeSpan.FromMinutes(2);
            _autoTimer.Tick += async (_, __) =>
            {
                if (chkAutoRefresh.IsChecked == true)
                {
                    await LoadSuggestionsAsync();
                }
            };
            _autoTimer.Start();
        }

        private async void SuggestionPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadSuggestionsAsync();
        }

        private async Task LoadSuggestionsAsync()
        {
            try
            {
                progress.Visibility = Visibility.Visible;
                lstProblems.ItemsSource = null;

                var profile = _profileRepo.GetByUserId(_userId);
                var handle = profile?.Codeforces;
                if (string.IsNullOrWhiteSpace(handle))
                {
                    txtSubtitle.Text = "Connect your Codeforces handle in Platform Settings to see suggestions.";
                    return;
                }

                txtSubtitle.Text = $"(Handle: {handle})";

                var user = await _cfService.GetUserInfoAsync(handle);
                var minThreshold = Math.Max(0, user.Rating - 100);
                var maxThreshold = Math.Max(0, user.Rating) + 200;

                var problems = await _cfService.GetSuggestionsAsync(handle, 20);
                lstProblems.ItemsSource = problems;

                txtSubtitle.Text = $"(Handle: {handle} • Rating: {user.Rating} • Showing {minThreshold}–{maxThreshold})";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Suggestion load error: {ex}");
                MessageBox.Show($"Failed to load suggestions: {ex.Message}", "Suggestions", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                progress.Visibility = Visibility.Collapsed;
            }
        }

        private void OpenProblemInBrowser(Problem? p)
        {
            if (p == null) return;
            var url = $"https://codeforces.com/problemset/problem/{p.ContestId}/{p.Index}";
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(psi);
                // Schedule a gentle refresh a bit later in case the user solves it quickly
                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(30));
                    await Dispatcher.InvokeAsync(async () =>
                    {
                        if (chkAutoRefresh.IsChecked == true)
                        {
                            await LoadSuggestionsAsync();
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open browser: {ex.Message}");
            }
        }

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadSuggestionsAsync();
        }
    }

    // Minimal generic RelayCommand to avoid MVVM dependency
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;
        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute; _canExecute = canExecute;
        }
        public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;
        public void Execute(object? parameter) => _execute((T?)parameter);
        public event EventHandler? CanExecuteChanged { add { CommandManager.RequerySuggested += value; } remove { CommandManager.RequerySuggested -= value; } }
    }
}
