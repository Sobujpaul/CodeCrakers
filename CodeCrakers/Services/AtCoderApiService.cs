using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeCrakers.Models;
using System.Text.RegularExpressions;

namespace CodeCrakers.Services
{
    public class AtCoderApiService : BaseApiService
    {
        public AtCoderApiService() : base("https://atcoder.jp/") { }

        public async Task<AtCoderUserInfo> GetUserInfoAsync(string username)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}users/{username}");
                response.EnsureSuccessStatusCode();
                
                var html = await response.Content.ReadAsStringAsync();
                
                // Parse HTML to extract user information
                var userInfo = new AtCoderUserInfo
                {
                    UserScreenName = username
                };

                // Extract rating using regex
                var ratingMatch = Regex.Match(html, @"Rating:\s*(\d+)");
                if (ratingMatch.Success)
                {
                    userInfo.Rating = ratingMatch.Groups[1].Value;
                }

                // Extract highest rating
                var highestRatingMatch = Regex.Match(html, @"Highest Rating:\s*(\d+)");
                if (highestRatingMatch.Success)
                {
                    userInfo.HighestRating = highestRatingMatch.Groups[1].Value;
                }

                // Extract rated matches
                var ratedMatchesMatch = Regex.Match(html, @"Rated Matches:\s*(\d+)");
                if (ratedMatchesMatch.Success)
                {
                    userInfo.RatedMatches = ratedMatchesMatch.Groups[1].Value;
                }

                // Extract last competed
                var lastCompetedMatch = Regex.Match(html, @"Last Competed:\s*([^<]+)");
                if (lastCompetedMatch.Success)
                {
                    userInfo.LastCompeted = lastCompetedMatch.Groups[1].Value.Trim();
                }

                // Extract affiliation
                var affiliationMatch = Regex.Match(html, @"Affiliation:\s*([^<]+)");
                if (affiliationMatch.Success)
                {
                    userInfo.Affiliation = affiliationMatch.Groups[1].Value.Trim();
                }

                // Extract country
                var countryMatch = Regex.Match(html, @"Country:\s*([^<]+)");
                if (countryMatch.Success)
                {
                    userInfo.Country = countryMatch.Groups[1].Value.Trim();
                }

                return userInfo;
            }
            catch (Exception ex)
            {
                throw new ApiException($"Failed to fetch AtCoder user info: {ex.Message}", ex);
            }
        }

        public async Task<PlatformStats> GetUserStatsAsync(string username)
        {
            try
            {
                var userInfo = await GetUserInfoAsync(username);

                var rating = 0;
                if (int.TryParse(userInfo.Rating, out var r))
                {
                    rating = r;
                }

                var maxRating = 0;
                if (int.TryParse(userInfo.HighestRating, out var mr))
                {
                    maxRating = mr;
                }

                var contestsParticipated = 0;
                if (int.TryParse(userInfo.RatedMatches, out var rc))
                {
                    contestsParticipated = rc;
                }

                return new PlatformStats
                {
                    Platform = "AtCoder",
                    Username = username,
                    Rating = rating,
                    MaxRating = maxRating,
                    ProblemsSolved = 0, // Would need additional scraping
                    ContestsParticipated = contestsParticipated,
                    LastActivity = DateTime.UtcNow, // Would need to parse LastCompeted
                    IsConnected = true
                };
            }
            catch (ApiException)
            {
                return new PlatformStats
                {
                    Platform = "AtCoder",
                    Username = username,
                    IsConnected = false
                };
            }
        }

        public async Task<WeeklyStats> GetWeeklyStatsAsync(string username)
        {
            try
            {
                // AtCoder weekly stats would require more complex scraping
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
