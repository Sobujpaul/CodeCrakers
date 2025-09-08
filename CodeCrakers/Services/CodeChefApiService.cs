using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeCrakers.Models;
using System.Text.RegularExpressions;

namespace CodeCrakers.Services
{
    public class CodeChefApiService : BaseApiService
    {
        public CodeChefApiService() : base("https://www.codechef.com/") { }

        public async Task<CodeChefUserInfo> GetUserInfoAsync(string username)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}users/{username}");
                response.EnsureSuccessStatusCode();
                
                var html = await response.Content.ReadAsStringAsync();
                
                // Parse HTML to extract user information
                var userInfo = new CodeChefUserInfo
                {
                    Username = username
                };

                // Extract rating using regex
                var ratingMatch = Regex.Match(html, @"Rating:\s*(\d+)");
                if (ratingMatch.Success && int.TryParse(ratingMatch.Groups[1].Value, out var rating))
                {
                    userInfo.Rating = rating;
                }

                // Extract max rating
                var maxRatingMatch = Regex.Match(html, @"Highest Rating:\s*(\d+)");
                if (maxRatingMatch.Success && int.TryParse(maxRatingMatch.Groups[1].Value, out var maxRating))
                {
                    userInfo.MaxRating = maxRating;
                }

                // Extract problems solved
                var problemsMatch = Regex.Match(html, @"Problems Solved:\s*(\d+)");
                if (problemsMatch.Success && int.TryParse(problemsMatch.Groups[1].Value, out var problems))
                {
                    userInfo.ProblemsSolved = problems;
                }

                // Extract partially solved
                var partialMatch = Regex.Match(html, @"Problems Partially Solved:\s*(\d+)");
                if (partialMatch.Success && int.TryParse(partialMatch.Groups[1].Value, out var partial))
                {
                    userInfo.ProblemsPartiallySolved = partial;
                }

                // Extract global rank
                var globalRankMatch = Regex.Match(html, @"Global Rank:\s*([\d,]+)");
                if (globalRankMatch.Success)
                {
                    userInfo.GlobalRank = globalRankMatch.Groups[1].Value;
                }

                // Extract country rank
                var countryRankMatch = Regex.Match(html, @"Country Rank:\s*([\d,]+)");
                if (countryRankMatch.Success)
                {
                    userInfo.CountryRank = countryRankMatch.Groups[1].Value;
                }

                return userInfo;
            }
            catch (Exception ex)
            {
                throw new ApiException($"Failed to fetch CodeChef user info: {ex.Message}", ex);
            }
        }

        public async Task<PlatformStats> GetUserStatsAsync(string username)
        {
            try
            {
                var userInfo = await GetUserInfoAsync(username);

                return new PlatformStats
                {
                    Platform = "CodeChef",
                    Username = username,
                    Rating = userInfo.Rating,
                    MaxRating = userInfo.MaxRating,
                    ProblemsSolved = userInfo.ProblemsSolved,
                    ContestsParticipated = 0, // Would need additional scraping
                    LastActivity = DateTime.UtcNow, // Would need additional scraping
                    IsConnected = true
                };
            }
            catch (ApiException)
            {
                return new PlatformStats
                {
                    Platform = "CodeChef",
                    Username = username,
                    IsConnected = false
                };
            }
        }

        public async Task<WeeklyStats> GetWeeklyStatsAsync(string username)
        {
            try
            {
                // CodeChef weekly stats would require more complex scraping
                // For now, return empty stats
                return new WeeklyStats
                {
                    ProblemsSolved = 0,
                    ContestsParticipated = 0,
                    RatingChange = 0,
                    WeekStart = DateTime.UtcNow.AddDays(-7),
                    WeekEnd = DateTime.UtcNow
                };
            }
            catch (ApiException)
            {
                return new WeeklyStats
                {
                    ProblemsSolved = 0,
                    ContestsParticipated = 0,
                    RatingChange = 0,
                    WeekStart = DateTime.UtcNow.AddDays(-7),
                    WeekEnd = DateTime.UtcNow
                };
            }
        }

        public new async Task<bool> TestConnectionAsync(string username)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}users/{username}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
