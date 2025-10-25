using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeCrakers.Models;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CodeCrakers.Services
{
    public class LeetCodeApiService : BaseApiService
    {
        public LeetCodeApiService() : base("https://leetcode.com/")
        {
            // Harden headers for GraphQL and page preflights
            _httpClient.Timeout = TimeSpan.FromSeconds(20);
            if (!_httpClient.DefaultRequestHeaders.Accept.Any())
            {
                _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            }
            try { _httpClient.DefaultRequestHeaders.UserAgent.Clear(); } catch { }
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/118.0.0.0 Safari/537.36");
            if (!_httpClient.DefaultRequestHeaders.Contains("Origin"))
            {
                _httpClient.DefaultRequestHeaders.Add("Origin", "https://leetcode.com");
            }
            _httpClient.DefaultRequestHeaders.Referrer = new Uri("https://leetcode.com");
        }

        public async Task<LeetCodeUser> GetUserInfoAsync(string username)
        {
            try
            {
                // Preflight to pick up cookies/tokens
                try { await _httpClient.GetAsync("https://leetcode.com/"); } catch { }
                try { await _httpClient.GetAsync($"https://leetcode.com/{Uri.EscapeDataString(username)}/"); } catch { }

                // GraphQL matchedUser
                var gql = @"query getUserProfile($username: String!) {\n  matchedUser(username: $username) {\n    username\n    profile {\n      realName\n      ranking\n      reputation\n      countryName\n      school\n      company\n      websiteUrl\n      aboutMe\n    }\n    submitStats: submitStatsGlobal {\n      acSubmissionNum { difficulty count submissions }\n      totalSubmissionNum { difficulty count submissions }\n    }\n  }\n}";

                var payload = new
                {
                    query = gql,
                    variables = new { username }
                };

                var req = new HttpRequestMessage(HttpMethod.Post, new Uri("https://leetcode.com/graphql"))
                {
                    Content = new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json")
                };
                req.Headers.Referrer = new Uri($"https://leetcode.com/{username}/");

                var res = await _httpClient.SendAsync(req);
                res.EnsureSuccessStatusCode();
                var json = await res.Content.ReadAsStringAsync();

                var root = JObject.Parse(json);
                if (root["errors"] != null)
                    throw new ApiException("LeetCode GraphQL returned errors.");

                var matched = root["data"]?["matchedUser"] as JObject;
                if (matched == null)
                    throw new ApiException($"User '{username}' not found on LeetCode");

                var profile = matched["profile"] as JObject ?? new JObject();
                var stats = matched["submitStats"] as JObject ?? new JObject();

                var acList = stats["acSubmissionNum"]?.ToObject<List<LeetCodeSubmissionStat>>() ?? new List<LeetCodeSubmissionStat>();
                var totalList = stats["totalSubmissionNum"]?.ToObject<List<LeetCodeSubmissionStat>>() ?? new List<LeetCodeSubmissionStat>();

                return new LeetCodeUser
                {
                    Profile = new LeetCodeUserProfile
                    {
                        UserName = username,
                        RealName = profile.Value<string>("realName") ?? string.Empty,
                        Ranking = (profile.Value<int?>("ranking")?.ToString() ?? string.Empty),
                        School = profile.Value<string>("school") ?? string.Empty,
                        Company = profile.Value<string>("company") ?? string.Empty,
                        WebsiteUrl = profile.Value<string>("websiteUrl") ?? string.Empty,
                        AboutMe = profile.Value<string>("aboutMe") ?? string.Empty,
                        Reputation = profile.Value<string>("reputation") ?? (profile.Value<int?>("reputation")?.ToString() ?? "0"),
                        Location = profile.Value<string>("countryName") ?? string.Empty
                    },
                    SubmissionStats = new LeetCodeSubmissionStats
                    {
                        AcSubmissionNum = acList,
                        TotalSubmissionNum = totalList
                    }
                };
            }
            catch (Exception ex)
            {
                // Fall back to community user info (to get Ranking) and profile existence
                var communityInfo = await GetUserInfoFromCommunityAsync(username);
                if (communityInfo != null) return communityInfo;

                // As last resort, if profile exists, return minimal
                if (await ProfileExistsAsync(username))
                {
                    return new LeetCodeUser
                    {
                        Profile = new LeetCodeUserProfile { UserName = username, Reputation = "0", Ranking = string.Empty },
                        SubmissionStats = new LeetCodeSubmissionStats
                        {
                            AcSubmissionNum = new List<LeetCodeSubmissionStat>(),
                            TotalSubmissionNum = new List<LeetCodeSubmissionStat>()
                        }
                    };
                }

                throw new ApiException($"Failed to fetch LeetCode user info: {ex.Message}", ex);
            }
        }

        // Community fallback that includes ranking and solved count
        private async Task<LeetCodeUser?> GetUserInfoFromCommunityAsync(string username)
        {
            var endpoints = new[]
            {
                $"https://leetcode-stats-api.herokuapp.com/{Uri.EscapeDataString(username)}",
                $"https://leetcode-stats-api.vercel.app/{Uri.EscapeDataString(username)}",
            };

            foreach (var url in endpoints)
            {
                try
                {
                    var res = await _httpClient.GetAsync(url);
                    if (!res.IsSuccessStatusCode) continue;
                    var content = await res.Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(content)) continue;
                    var obj = JObject.Parse(content);
                    var message = obj.SelectToken("message")?.ToString();
                    if (!string.IsNullOrEmpty(message) && message.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;

                    var totalSolved = GetInt(obj, new []{ "totalSolved", "total_solved", "data.totalSolved" });
                    var reputation = GetInt(obj, new []{ "reputation", "data.reputation" });
                    var ranking = GetInt(obj, new []{ "ranking", "data.ranking" });

                    return new LeetCodeUser
                    {
                        Profile = new LeetCodeUserProfile
                        {
                            UserName = username,
                            Reputation = reputation.ToString(),
                            Ranking = ranking > 0 ? ranking.ToString() : string.Empty
                        },
                        SubmissionStats = new LeetCodeSubmissionStats
                        {
                            AcSubmissionNum = new List<LeetCodeSubmissionStat>
                            {
                                new LeetCodeSubmissionStat { Difficulty = "All", Count = totalSolved, Submissions = totalSolved }
                            },
                            TotalSubmissionNum = new List<LeetCodeSubmissionStat>()
                        }
                    };
                }
                catch { }
            }
            return null;
        }

        private async Task<bool> ProfileExistsAsync(string username)
        {
            try
            {
                var res = await _httpClient.GetAsync($"https://leetcode.com/{Uri.EscapeDataString(username)}/");
                return res.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        private async Task<PlatformStats?> GetUserStatsFromCommunityAsync(string username)
        {
            var endpoints = new[]
            {
                $"https://leetcode-stats-api.herokuapp.com/{Uri.EscapeDataString(username)}",
                $"https://leetcode-stats-api.vercel.app/{Uri.EscapeDataString(username)}",
            };

            foreach (var url in endpoints)
            {
                try
                {
                    var res = await _httpClient.GetAsync(url);
                    if (!res.IsSuccessStatusCode) continue;
                    var content = await res.Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(content)) continue;
                    var obj = JObject.Parse(content);
                    var message = obj.SelectToken("message")?.ToString();
                    if (!string.IsNullOrEmpty(message) && message.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;

                    var totalSolved = GetInt(obj, new []{ "totalSolved", "total_solved", "data.totalSolved" });
                    var reputation = GetInt(obj, new []{ "reputation", "data.reputation" });
                    var ranking = GetInt(obj, new []{ "ranking", "data.ranking" });

                    return new PlatformStats
                    {
                        Platform = "LeetCode",
                        Username = username,
                        Rating = reputation > 0 ? reputation : ranking,
                        MaxRating = reputation > 0 ? reputation : ranking,
                        ProblemsSolved = totalSolved,
                        ContestsParticipated = 0,
                        LastActivity = DateTime.UtcNow,
                        IsConnected = true
                    };
                }
                catch
                {
                    // try next
                }
            }
            return null;
        }

        private static int GetInt(JObject obj, IEnumerable<string> keys)
        {
            foreach (var k in keys)
            {
                var token = obj.SelectToken(k);
                if (token == null) continue;
                if (token.Type == JTokenType.Integer) return token.Value<int>();
                if (int.TryParse(token.ToString(), out var val)) return val;
            }
            return 0;
        }

        public async Task<PlatformStats> GetUserStatsAsync(string username)
        {
            try
            {
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
            catch (Exception)
            {
                var fallback = await GetUserStatsFromCommunityAsync(username);
                if (fallback != null) return fallback;
                if (await ProfileExistsAsync(username))
                {
                    return new PlatformStats
                    {
                        Platform = "LeetCode",
                        Username = username,
                        Rating = 0,
                        MaxRating = 0,
                        ProblemsSolved = 0,
                        ContestsParticipated = 0,
                        LastActivity = DateTime.UtcNow,
                        IsConnected = true
                    };
                }
                return new PlatformStats { Platform = "LeetCode", Username = username, IsConnected = false };
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
                try { await GetUserInfoAsync(username); return true; } catch { }
                var lower = username.ToLowerInvariant();
                if (!string.Equals(username, lower, StringComparison.Ordinal))
                {
                    try { await GetUserInfoAsync(lower); return true; } catch { }
                }
                if (await ProfileExistsAsync(username)) return true;
                if (!string.Equals(username, lower, StringComparison.Ordinal) && await ProfileExistsAsync(lower)) return true;
                var fb = await GetUserStatsFromCommunityAsync(username);
                if (fb?.IsConnected == true) return true;
                if (!string.Equals(username, lower, StringComparison.Ordinal))
                {
                    var fb2 = await GetUserStatsFromCommunityAsync(lower);
                    if (fb2?.IsConnected == true) return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LeetCode: Connection test error: {ex.Message}");
                return false;
            }
        }
    }
}
