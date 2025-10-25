using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeCrakers.Models;
using Newtonsoft.Json.Linq;

namespace CodeCrakers.Services
{
    public class AtCoderApiService : BaseApiService
    {
        public AtCoderApiService() : base("https://atcoder.jp/")
        {
            // Prefer JSON endpoints; adjust headers slightly
            if (!_httpClient.DefaultRequestHeaders.Accept.Any())
            {
                _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            }
            try { _httpClient.DefaultRequestHeaders.UserAgent.Clear(); } catch { }
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/118.0.0.0 Safari/537.36");
        }

        public async Task<AtCoderUserInfo> GetUserInfoAsync(string username)
        {
            try
            {
                // Use rating history JSON as the authoritative source
                var history = await GetUserRatingHistoryAsync(username) ?? new List<(DateTime time, int rating)>();
                var currentRating = history.Any() ? history.Last().rating : 0;
                var maxRating = history.Any() ? history.Max(h => h.rating) : 0;
                var contests = history.Count;

                return new AtCoderUserInfo
                {
                    UserScreenName = username,
                    Rating = currentRating.ToString(),
                    HighestRating = maxRating.ToString(),
                    RatedMatches = contests.ToString(),
                    LastCompeted = history.Any() ? history.Last().time.ToString("u") : string.Empty
                };
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

                var rating = int.TryParse(userInfo.Rating, out var r) ? r : 0;
                var maxRating = int.TryParse(userInfo.HighestRating, out var mr) ? mr : 0;
                var contestsParticipated = int.TryParse(userInfo.RatedMatches, out var rc) ? rc : 0;

                // Problems solved: distinct AC submissions from AtCoder Problems API
                var acDistinct = await GetDistinctAcceptedCountAsync(username);
                var lastActivity = await GetLastActivityAsync(username, userInfo.LastCompeted);

                return new PlatformStats
                {
                    Platform = "AtCoder",
                    Username = username,
                    Rating = rating,
                    MaxRating = maxRating,
                    ProblemsSolved = acDistinct,
                    ContestsParticipated = contestsParticipated,
                    LastActivity = lastActivity,
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
                var oneWeekAgo = DateTime.UtcNow.AddDays(-7);
                var history = await GetUserRatingHistoryAsync(username) ?? new List<(DateTime time, int rating)>();
                var ratingChange = 0;
                if (history.Count >= 2)
                {
                    var recent = history.Where(h => h.time >= oneWeekAgo).ToList();
                    if (recent.Count > 1)
                    {
                        ratingChange = recent.Last().rating - recent.First().rating;
                    }
                }

                var weekSolved = await GetAcceptedCountSinceAsync(username, oneWeekAgo);
                var weekContests = history.Count(h => h.time >= oneWeekAgo);

                return new WeeklyStats
                {
                    ProblemsSolved = weekSolved,
                    ContestsParticipated = weekContests,
                    RatingChange = ratingChange,
                    WeekStart = oneWeekAgo,
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
                var lower = username.ToLowerInvariant();

                // Try profile page
                try
                {
                    var page = await _httpClient.GetAsync($"{_baseUrl}users/{Uri.EscapeDataString(username)}");
                    if (page.IsSuccessStatusCode) return true;
                    if (!string.Equals(username, lower, StringComparison.Ordinal))
                    {
                        var pageLower = await _httpClient.GetAsync($"{_baseUrl}users/{Uri.EscapeDataString(lower)}");
                        if (pageLower.IsSuccessStatusCode) return true;
                    }
                }
                catch { }

                // Try rating history
                try
                {
                    var hist = await _httpClient.GetAsync($"{_baseUrl}users/{Uri.EscapeDataString(username)}/history/json");
                    if (hist.IsSuccessStatusCode)
                    {
                        var txt = await hist.Content.ReadAsStringAsync();
                        if (!string.IsNullOrWhiteSpace(txt)) return true;
                    }
                    if (!string.Equals(username, lower, StringComparison.Ordinal))
                    {
                        var histLower = await _httpClient.GetAsync($"{_baseUrl}users/{Uri.EscapeDataString(lower)}/history/json");
                        if (histLower.IsSuccessStatusCode)
                        {
                            var txt2 = await histLower.Content.ReadAsStringAsync();
                            if (!string.IsNullOrWhiteSpace(txt2)) return true;
                        }
                    }
                }
                catch { }

                // Try kenkoooo results
                try
                {
                    var url = $"https://kenkoooo.com/atcoder/atcoder-api/results?user={Uri.EscapeDataString(username)}";
                    var res = await _httpClient.GetAsync(url);
                    if (res.IsSuccessStatusCode)
                    {
                        var txt = await res.Content.ReadAsStringAsync();
                        if (!string.IsNullOrWhiteSpace(txt) && txt.TrimStart().StartsWith("[")) return true;
                    }
                    if (!string.Equals(username, lower, StringComparison.Ordinal))
                    {
                        var url2 = $"https://kenkoooo.com/atcoder/atcoder-api/results?user={Uri.EscapeDataString(lower)}";
                        var res2 = await _httpClient.GetAsync(url2);
                        if (res2.IsSuccessStatusCode)
                        {
                            var txt2 = await res2.Content.ReadAsStringAsync();
                            if (!string.IsNullOrWhiteSpace(txt2) && txt2.TrimStart().StartsWith("[")) return true;
                        }
                    }
                }
                catch { }

                return false;
            }
            catch
            {
                return false;
            }
        }

        // --- helpers ---
        private async Task<List<(DateTime time, int rating)>?> GetUserRatingHistoryAsync(string username)
        {
            try
            {
                var res = await _httpClient.GetAsync($"{_baseUrl}users/{Uri.EscapeDataString(username)}/history/json");
                if (!res.IsSuccessStatusCode) return null;
                var json = await res.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json)) return null;
                var arr = JArray.Parse(json);
                var list = new List<(DateTime, int)>();
                foreach (var item in arr)
                {
                    // Rating
                    var rt = item.Value<int?>("NewRating") ?? item.Value<int?>("Rating") ?? 0;

                    // Timestamp: support multiple field formats
                    DateTime? time = null;

                    // Try 'Date' as long (ms or s)
                    var dateLong = item.Value<long?>("Date");
                    if (dateLong.HasValue)
                    {
                        var val = dateLong.Value;
                        // Heuristic: > 10^12 means milliseconds
                        time = (val > 1_000_000_000_000L)
                            ? DateTimeOffset.FromUnixTimeMilliseconds(val).UtcDateTime
                            : DateTimeOffset.FromUnixTimeSeconds(val).UtcDateTime;
                    }

                    // Try 'EndTime' as long or string
                    if (!time.HasValue)
                    {
                        var endTimeToken = item["EndTime"];
                        if (endTimeToken != null)
                        {
                            if (endTimeToken.Type == JTokenType.Integer)
                            {
                                var val = endTimeToken.Value<long>();
                                time = (val > 1_000_000_000_000L)
                                    ? DateTimeOffset.FromUnixTimeMilliseconds(val).UtcDateTime
                                    : DateTimeOffset.FromUnixTimeSeconds(val).UtcDateTime;
                            }
                            else if (endTimeToken.Type == JTokenType.String)
                            {
                                var s = endTimeToken.Value<string>();
                                if (DateTimeOffset.TryParse(s, out var dto))
                                {
                                    time = dto.UtcDateTime;
                                }
                            }
                        }
                    }

                    if (time.HasValue)
                    {
                        list.Add((time.Value, rt));
                    }
                }
                // Lowercase retry if nothing parsed and username has uppercase
                if (list.Count == 0 && username.Any(char.IsUpper))
                {
                    try
                    {
                        var lower = username.ToLowerInvariant();
                        var res2 = await _httpClient.GetAsync($"{_baseUrl}users/{Uri.EscapeDataString(lower)}/history/json");
                        if (res2.IsSuccessStatusCode)
                        {
                            var json2 = await res2.Content.ReadAsStringAsync();
                            if (!string.IsNullOrWhiteSpace(json2))
                            {
                                var arr2 = JArray.Parse(json2);
                                foreach (var item in arr2)
                                {
                                    var rt2 = item.Value<int?>("NewRating") ?? item.Value<int?>("Rating") ?? 0;
                                    DateTime? time2 = null;
                                    var dateLong2 = item.Value<long?>("Date");
                                    if (dateLong2.HasValue)
                                    {
                                        var val = dateLong2.Value;
                                        time2 = (val > 1_000_000_000_000L)
                                            ? DateTimeOffset.FromUnixTimeMilliseconds(val).UtcDateTime
                                            : DateTimeOffset.FromUnixTimeSeconds(val).UtcDateTime;
                                    }
                                    if (!time2.HasValue)
                                    {
                                        var endToken2 = item["EndTime"];
                                        if (endToken2 != null)
                                        {
                                            if (endToken2.Type == JTokenType.Integer)
                                            {
                                                var v = endToken2.Value<long>();
                                                time2 = (v > 1_000_000_000_000L)
                                                    ? DateTimeOffset.FromUnixTimeMilliseconds(v).UtcDateTime
                                                    : DateTimeOffset.FromUnixTimeSeconds(v).UtcDateTime;
                                            }
                                            else if (endToken2.Type == JTokenType.String)
                                            {
                                                var s = endToken2.Value<string>();
                                                if (DateTimeOffset.TryParse(s, out var dto2))
                                                    time2 = dto2.UtcDateTime;
                                            }
                                        }
                                    }
                                    if (time2.HasValue) list.Add((time2.Value, rt2));
                                }
                            }
                        }
                    }
                    catch { }
                }
                return list;
            }
            catch { return null; }
        }

        private async Task<int> GetDistinctAcceptedCountAsync(string username)
        {
            try
            {
                var url = $"https://kenkoooo.com/atcoder/atcoder-api/results?user={Uri.EscapeDataString(username)}";
                var res = await _httpClient.GetAsync(url);
                if (!res.IsSuccessStatusCode) return 0;
                var json = await res.Content.ReadAsStringAsync();
                var arr = JArray.Parse(json);
                var ac = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in arr)
                {
                    if (string.Equals(item.Value<string>("result"), "AC", StringComparison.OrdinalIgnoreCase))
                    {
                        var pid = item.Value<string>("problem_id") ?? string.Empty;
                        if (!string.IsNullOrEmpty(pid)) ac.Add(pid);
                    }
                }
                // Lowercase retry for case-sensitive handles
                if (ac.Count == 0 && username.Any(char.IsUpper))
                {
                    var lowerUrl = $"https://kenkoooo.com/atcoder/atcoder-api/results?user={Uri.EscapeDataString(username.ToLowerInvariant())}";
                    var res2 = await _httpClient.GetAsync(lowerUrl);
                    if (res2.IsSuccessStatusCode)
                    {
                        var json2 = await res2.Content.ReadAsStringAsync();
                        var arr2 = JArray.Parse(json2);
                        foreach (var item in arr2)
                        {
                            if (string.Equals(item.Value<string>("result"), "AC", StringComparison.OrdinalIgnoreCase))
                            {
                                var pid = item.Value<string>("problem_id") ?? string.Empty;
                                if (!string.IsNullOrEmpty(pid)) ac.Add(pid);
                            }
                        }
                    }
                }
                return ac.Count;
            }
            catch { return 0; }
        }

        private async Task<DateTime> GetLastActivityAsync(string username, string lastCompeted)
        {
            try
            {
                var url = $"https://kenkoooo.com/atcoder/atcoder-api/results?user={Uri.EscapeDataString(username)}";
                var res = await _httpClient.GetAsync(url);
                if (res.IsSuccessStatusCode)
                {
                    var txt = await res.Content.ReadAsStringAsync();
                    var arr = JArray.Parse(txt);
                    var last = arr.Select(i => i.Value<long?>("epoch_second") ?? 0).DefaultIfEmpty(0).Max();
                    if (last > 0) return DateTimeOffset.FromUnixTimeSeconds(last).UtcDateTime;
                }
            }
            catch { }
            if (DateTime.TryParse(lastCompeted, out var parsed)) return parsed;
            return DateTime.UtcNow;
        }

        private async Task<int> GetAcceptedCountSinceAsync(string username, DateTime sinceUtc)
        {
            try
            {
                var url = $"https://kenkoooo.com/atcoder/atcoder-api/results?user={Uri.EscapeDataString(username)}";
                var res = await _httpClient.GetAsync(url);
                if (!res.IsSuccessStatusCode) return 0;
                var json = await res.Content.ReadAsStringAsync();
                var arr = JArray.Parse(json);
                var count = 0;
                var threshold = new DateTimeOffset(sinceUtc).ToUnixTimeSeconds();
                foreach (var item in arr)
                {
                    var verdict = item.Value<string>("result");
                    var ts = item.Value<long?>("epoch_second") ?? 0;
                    if (string.Equals(verdict, "AC", StringComparison.OrdinalIgnoreCase) && ts >= threshold)
                    {
                        count++;
                    }
                }
                return count;
            }
            catch { return 0; }
        }
    }
}
