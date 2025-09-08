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

        public async Task<PlatformStats> GetUserStatsAsync(string username)
        {
            try
            {
                var userInfo = await GetUserInfoAsync(username);
                var submissions = await GetUserSubmissionsAsync(username, 1, 1000);

                // Calculate problems solved (AC submissions)
                var solvedProblems = submissions
                    .Where(s => s.Verdict == "OK")
                    .Select(s => $"{s.Problem.ContestId}{s.Problem.Index}")
                    .Distinct()
                    .Count();

                // Calculate contests participated
                var contestsParticipated = submissions
                    .Where(s => s.ContestId > 0)
                    .Select(s => s.ContestId)
                    .Distinct()
                    .Count();

                // Get last activity
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
                return new PlatformStats
                {
                    Platform = "Codeforces",
                    Username = username,
                    IsConnected = false
                };
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
                    .Where(s => s.Verdict == "OK")
                    .Select(s => $"{s.Problem.ContestId}{s.Problem.Index}")
                    .Distinct()
                    .Count();

                // Calculate contests participated this week
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
    }
}
