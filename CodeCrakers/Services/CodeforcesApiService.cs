using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeCrakers.Models;

namespace CodeCrakers.Services
{
    public class CodeforcesApiService : BaseApiService
    {
        public CodeforcesApiService() : base("https://codeforces.com/api/") { }

        public async Task<CodeforcesUser> GetUserInfoAsync(string username)
        {
            var response = await GetAsync<CodeforcesUserInfo>($"user.info?handles={username}");
            
            if (response.Status != "OK" || response.Result == null || !response.Result.Any())
            {
                throw new ApiException($"User '{username}' not found on Codeforces");
            }

            return response.Result.First();
        }

        public async Task<List<CodeforcesSubmission>> GetUserSubmissionsAsync(string username, int from = 1, int count = 1000)
        {
            var response = await GetAsync<CodeforcesSubmissionInfo>($"user.status?handle={username}&from={from}&count={count}");
            
            if (response.Status != "OK")
            {
                throw new ApiException($"Failed to fetch submissions for user '{username}'");
            }

            return response.Result ?? new List<CodeforcesSubmission>();
        }

        // Fetch all submissions with pagination to avoid truncation (fix for leaderboard incorrect solved count)
        private async Task<List<CodeforcesSubmission>> GetAllUserSubmissionsAsync(string username, int batchSize = 1000, int hardLimit = 200000)
        {
            var all = new List<CodeforcesSubmission>();
            int from = 1;

            while (from <= hardLimit)
            {
                var batch = await GetUserSubmissionsAsync(username, from, batchSize);
                if (batch.Count == 0)
                    break;

                all.AddRange(batch);

                // If fewer than batchSize returned, we've reached the end
                if (batch.Count < batchSize)
                    break;

                from += batchSize;
            }

            return all;
        }

        public async Task<PlatformStats> GetUserStatsAsync(string username)
        {
            try
            {
                var userInfo = await GetUserInfoAsync(username);
                // Use full (paginated) submission history instead of only first 1000 to ensure accurate solved count
                var submissions = await GetAllUserSubmissionsAsync(username);

                var solvedProblems = submissions
                    .Where(s => s.Verdict == "OK" && s.Problem != null)
                    .Select(s => $"{s.Problem!.ContestId}-{s.Problem.Index}")
                    .Distinct()
                    .Count();

                var contestsParticipated = submissions
                    .Where(s => s.ContestId > 0)
                    .Select(s => s.ContestId)
                    .Distinct()
                    .Count();

                var lastActivity = submissions.Any()
                    ? DateTimeOffset.FromUnixTimeSeconds(submissions.Max(s => s.CreationTimeSeconds)).DateTime
                    : DateTimeOffset.FromUnixTimeSeconds(userInfo.LastOnlineTimeSeconds).DateTime;

                return new PlatformStats
                {
                    Platform = "Codeforces",
                    Username = username,
                    Rating = userInfo.Rating,
                    MaxRating = userInfo.MaxRating,
                    ProblemsSolved = solvedProblems,
                    ContestsParticipated = contestsParticipated,
                    LastActivity = lastActivity,
                    IsConnected = true
                };
            }
            catch (ApiException)
            {
                return new PlatformStats { Platform = "Codeforces", Username = username, IsConnected = false };
            }
        }

        public async Task<WeeklyStats> GetWeeklyStatsAsync(string username)
        {
            try
            {
                var submissions = await GetUserSubmissionsAsync(username, 1, 1000);
                var weekAgo = DateTime.UtcNow.AddDays(-7);

                // Filter submissions from last week
                var weeklySubmissions = submissions
                    .Where(s => DateTimeOffset.FromUnixTimeSeconds(s.CreationTimeSeconds).DateTime >= weekAgo)
                    .ToList();

                // Calculate problems solved this week
                var problemsSolved = weeklySubmissions
                    .Where(s => s.Verdict == "OK" && s.Problem != null)
                    .Select(s => $"{s.Problem!.ContestId}-{s.Problem.Index}")
                    .Distinct()
                    .Count();

                // Calculate contests participated this week (only actual contests)
                var contestsParticipated = weeklySubmissions
                    .Where(s => s.ContestId > 0)
                    .Select(s => s.ContestId)
                    .Distinct()
                    .Count();

                return new WeeklyStats
                {
                    ProblemsSolved = problemsSolved,
                    ContestsParticipated = contestsParticipated,
                    RatingChange = 0, // Would need to track rating changes over time
                    WeekStart = weekAgo,
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
                await GetUserInfoAsync(username);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<DetailedCodeforcesAnalytics> GetDetailedAnalyticsAsync(string username)
        {
            try
            {
                var userInfo = await GetUserInfoAsync(username);
                // Reuse the same pagination logic to avoid relying on an oversized single request
                var submissions = await GetAllUserSubmissionsAsync(username, 1000); // full analytics always gets all
                
                var analytics = new DetailedCodeforcesAnalytics
                {
                    UserInfo = userInfo,
                    TotalSubmissions = submissions.Count,
                    LastActivity = submissions.Any() 
                        ? DateTimeOffset.FromUnixTimeSeconds(submissions.Max(s => s.CreationTimeSeconds)).DateTime
                        : DateTimeOffset.FromUnixTimeSeconds(userInfo.LastOnlineTimeSeconds).DateTime
                };

                // Calculate submission verdicts
                analytics.VerdictStats = submissions
                    .GroupBy(s => s.Verdict ?? "Unknown")
                    .ToDictionary(g => g.Key, g => g.Count());

                // Calculate accepted problems
                var acceptedSubmissions = submissions.Where(s => s.Verdict == "OK" && s.Problem != null).ToList();
                analytics.SolvedProblems = acceptedSubmissions
                    .Select(s => $"{s.Problem!.ContestId}-{s.Problem.Index}")
                    .Distinct()
                    .Count();

                // Calculate problem difficulty distribution
                var problemsWithRating = acceptedSubmissions
                    .Where(s => s.Problem!.Rating.HasValue)
                    .Select(s => s.Problem!.Rating!.Value)
                    .ToList();

                analytics.DifficultyDistribution = problemsWithRating
                    .GroupBy(r => (int)(r / 100) * 100) // Group by hundreds (800, 900, 1000, etc.)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Calculate problem tags statistics
                var allTags = acceptedSubmissions
                    .Where(s => s.Problem!.Tags != null)
                    .SelectMany(s => s.Problem!.Tags!)
                    .ToList();

                analytics.TopProblemTags = allTags
                    .GroupBy(tag => tag)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Calculate programming languages
                analytics.LanguageStats = submissions
                    .Where(s => !string.IsNullOrEmpty(s.ProgrammingLanguage))
                    .GroupBy(s => s.ProgrammingLanguage!)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Calculate contests participated (from unique contest IDs)
                analytics.ContestsParticipated = submissions
                    .Where(s => s.ContestId > 0)
                    .Select(s => s.ContestId)
                    .Distinct()
                    .Count();

                // Get recent submissions (last 20)
                analytics.RecentSubmissions = submissions
                    .OrderByDescending(s => s.CreationTimeSeconds)
                    .Take(20)
                    .Select(s => new RecentSubmission
                    {
                        ProblemName = s.Problem?.Name ?? "Unknown",
                        ProblemIndex = s.Problem?.Index ?? "A",
                        ProblemRating = s.Problem?.Rating,
                        Verdict = s.Verdict ?? "Unknown",
                        Language = s.ProgrammingLanguage ?? "Unknown",
                        SubmissionTime = DateTimeOffset.FromUnixTimeSeconds(s.CreationTimeSeconds).DateTime,
                        ContestId = s.ContestId
                    })
                    .ToList();

                // Calculate success rate
                if (analytics.TotalSubmissions > 0)
                {
                    var acceptedCount = analytics.VerdictStats.GetValueOrDefault("OK", 0);
                    analytics.SuccessRate = (double)acceptedCount / analytics.TotalSubmissions * 100;
                }

                return analytics;
            }
            catch (ApiException)
            {
                return null!; // Caller handles null (analytics window checks and shows error state)
            }
        }

        public async Task<List<RatingChange>> GetUserRatingHistoryAsync(string username)
        {
            try
            {
                var response = await GetAsync<CodeforcesRatingChangeInfo>($"user.rating?handle={username}");
                
                if (response?.Status == "OK" && response.Result != null)
                {
                    return response.Result.OrderBy(r => r.RatingUpdateTimeSeconds)
                        .Select(r => new RatingChange
                        {
                            ContestName = r.ContestName,
                            Rank = r.Rank,
                            OldRating = r.OldRating,
                            NewRating = r.NewRating,
                            RatingChangeValue = r.NewRating - r.OldRating,
                            ContestTime = DateTimeOffset.FromUnixTimeSeconds(r.RatingUpdateTimeSeconds).DateTime
                        })
                        .ToList();
                }
                
                return new List<RatingChange>();
            }
            catch (ApiException)
            {
                return new List<RatingChange>();
            }
        }
    }
}
