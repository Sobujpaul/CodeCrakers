using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeCrakers.Models;
using System.Text;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace CodeCrakers.Services
{
    public class LeetCodeApiService : BaseApiService
    {
        public LeetCodeApiService() : base("https://leetcode.com/") { }

        public async Task<LeetCodeUser> GetUserInfoAsync(string username)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"LeetCode: Attempting to get user info for: {username}");
                
                // Try multiple approaches to get user information
                var userInfo = await TryGetUserInfoFromProfilePage(username);
                if (userInfo != null)
                {
                    return userInfo;
                }

                // Fallback: Try to get basic info from user stats
                userInfo = await TryGetUserInfoFromStats(username);
                if (userInfo != null)
                {
                    return userInfo;
                }

                // If all methods fail, throw an exception
                throw new ApiException($"User '{username}' not found on LeetCode");
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"LeetCode HTTP Error: {ex.Message}");
                throw new ApiException($"HTTP request failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LeetCode Parse Error: {ex.Message}");
                throw new ApiException($"Failed to parse LeetCode user info: {ex.Message}", ex);
            }
        }

        private async Task<LeetCodeUser> TryGetUserInfoFromProfilePage(string username)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"LeetCode: Trying profile page for: {username}");
                
                // Use the public profile page to get user information
                var response = await _httpClient.GetAsync($"{_baseUrl}{username}/");
                
                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"LeetCode: Profile page returned status: {response.StatusCode}");
                    return null;
                }
                
                var html = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"LeetCode: Retrieved HTML content, length: {html.Length}");
                
                // Check if the page contains user data (not a 404 or error page)
                if (html.Contains("User not found") || html.Contains("404") || html.Contains("Page Not Found"))
                {
                    System.Diagnostics.Debug.WriteLine("LeetCode: Page indicates user not found");
                    return null;
                }
                
                // Parse HTML to extract user information
                var userInfo = new LeetCodeUser
                {
                    Profile = new LeetCodeUserProfile
                    {
                        UserName = username
                    },
                    SubmissionStats = new LeetCodeSubmissionStats
                    {
                        AcSubmissionNum = new List<LeetCodeSubmissionStat>(),
                        TotalSubmissionNum = new List<LeetCodeSubmissionStat>()
                    }
                };

                // Try multiple regex patterns to extract reputation
                var reputationPatterns = new[]
                {
                    @"""reputation"":\s*(\d+)",
                    @"reputation[""']?\s*:\s*(\d+)",
                    @"reputation\s*=\s*(\d+)"
                };

                foreach (var pattern in reputationPatterns)
                {
                    var reputationMatch = Regex.Match(html, pattern);
                    if (reputationMatch.Success && int.TryParse(reputationMatch.Groups[1].Value, out var reputation))
                    {
                        userInfo.Profile.Reputation = reputation.ToString();
                        System.Diagnostics.Debug.WriteLine($"LeetCode: Found reputation: {reputation}");
                        break;
                    }
                }

                // Try multiple regex patterns to extract solution count
                var solutionPatterns = new[]
                {
                    @"""solutionCount"":\s*(\d+)",
                    @"solutionCount[""']?\s*:\s*(\d+)",
                    @"solved\s*(\d+)\s*problems"
                };

                foreach (var pattern in solutionPatterns)
                {
                    var solutionMatch = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
                    if (solutionMatch.Success && int.TryParse(solutionMatch.Groups[1].Value, out var solutionCount))
                    {
                        userInfo.Profile.SolutionCount = solutionCount.ToString();
                        System.Diagnostics.Debug.WriteLine($"LeetCode: Found solution count: {solutionCount}");
                        break;
                    }
                }

                // If we found any data, return the user info
                if (!string.IsNullOrEmpty(userInfo.Profile.Reputation) || !string.IsNullOrEmpty(userInfo.Profile.SolutionCount))
                {
                    System.Diagnostics.Debug.WriteLine("LeetCode: Successfully extracted user data from profile page");
                    return userInfo;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LeetCode: Error in profile page method: {ex.Message}");
                return null;
            }
        }

        private async Task<LeetCodeUser> TryGetUserInfoFromStats(string username)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"LeetCode: Trying stats endpoint for: {username}");
                
                // Try multiple API endpoints
                var endpoints = new[]
                {
                    $"{_baseUrl}api/user/{username}/",
                    $"{_baseUrl}graphql/",
                    $"{_baseUrl}api/problemset/all/",
                };

                foreach (var endpoint in endpoints)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"LeetCode: Trying endpoint: {endpoint}");
                        
                        var response = await _httpClient.GetAsync(endpoint);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync();
                            System.Diagnostics.Debug.WriteLine($"LeetCode: Retrieved content from {endpoint}, length: {content.Length}");
                            
                            // For now, just return a basic user info if we can reach the endpoint
                            if (!string.IsNullOrEmpty(content))
                            {
                                return new LeetCodeUser
                                {
                                    Profile = new LeetCodeUserProfile
                                    {
                                        UserName = username,
                                        Reputation = "0",
                                        SolutionCount = "0"
                                    },
                                    SubmissionStats = new LeetCodeSubmissionStats
                                    {
                                        AcSubmissionNum = new List<LeetCodeSubmissionStat>(),
                                        TotalSubmissionNum = new List<LeetCodeSubmissionStat>()
                                    }
                                };
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"LeetCode: Error with endpoint {endpoint}: {ex.Message}");
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LeetCode: Error in stats method: {ex.Message}");
                return null;
            }
        }

        public async Task<PlatformStats> GetUserStatsAsync(string username)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"LeetCode: Getting stats for user: {username}");
                
                var userInfo = await GetUserInfoAsync(username);
                var profile = userInfo.Profile;
                var submissionStats = userInfo.SubmissionStats;

                // Calculate total problems solved
                var totalSolved = submissionStats?.AcSubmissionNum?.Sum(s => s.Count) ?? 0;

                // Get reputation (as a proxy for rating)
                var reputation = 0;
                if (int.TryParse(profile?.Reputation, out var rep))
                {
                    reputation = rep;
                }

                System.Diagnostics.Debug.WriteLine($"LeetCode: User {username} - Problems: {totalSolved}, Reputation: {reputation}");

                return new PlatformStats
                {
                    Platform = "LeetCode",
                    Username = username,
                    Rating = reputation,
                    MaxRating = reputation, // LeetCode doesn't have max rating concept
                    ProblemsSolved = totalSolved,
                    ContestsParticipated = 0, // Would need separate API call for contests
                    LastActivity = DateTime.UtcNow, // Would need to track last submission
                    IsConnected = true
                };
            }
            catch (ApiException ex)
            {
                System.Diagnostics.Debug.WriteLine($"LeetCode API Error for user {username}: {ex.Message}");
                return new PlatformStats
                {
                    Platform = "LeetCode",
                    Username = username,
                    IsConnected = false
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LeetCode Unexpected Error for user {username}: {ex.Message}");
                return new PlatformStats
                {
                    Platform = "LeetCode",
                    Username = username,
                    IsConnected = false
                };
            }
        }

        public async Task<WeeklyStats> GetWeeklyStatsAsync(string username)
        {
            try
            {
                // LeetCode doesn't provide easy access to weekly stats via public API
                // This would require more complex implementation or web scraping
                var userStats = await GetUserStatsAsync(username);
                
                return new WeeklyStats
                {
                    ProblemsSolved = 0, // Would need to track over time
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
                System.Diagnostics.Debug.WriteLine($"LeetCode: Testing connection for user: {username}");
                
                // Test connection by checking if the user profile page exists
                var response = await _httpClient.GetAsync($"{_baseUrl}{username}/");
                
                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"LeetCode: Connection test failed with status: {response.StatusCode}");
                    return false;
                }
                
                var html = await response.Content.ReadAsStringAsync();
                
                // Check if the page contains user data (not a 404 or error page)
                if (html.Contains("User not found") || html.Contains("404") || html.Contains("Page Not Found"))
                {
                    System.Diagnostics.Debug.WriteLine("LeetCode: Connection test failed - user not found");
                    return false;
                }
                
                System.Diagnostics.Debug.WriteLine("LeetCode: Connection test successful");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LeetCode: Connection test error: {ex.Message}");
                return false;
            }
        }
    }
}
