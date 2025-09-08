using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeCrakers.Models;

namespace CodeCrakers.Services
{
    public class PlatformApiManager
    {
        private readonly CodeforcesApiService _codeforcesService;
        private readonly LeetCodeApiService _leetcodeService;
        private readonly CodeChefApiService _codechefService;
        private readonly AtCoderApiService _atcoderService;

        public PlatformApiManager()
        {
            _codeforcesService = new CodeforcesApiService();
            _leetcodeService = new LeetCodeApiService();
            _codechefService = new CodeChefApiService();
            _atcoderService = new AtCoderApiService();
        }

        public async Task<PlatformStats> GetPlatformStatsAsync(string platform, string username)
        {
            if (string.IsNullOrEmpty(username))
                return new PlatformStats { Platform = platform, IsConnected = false };

            try
            {
                return platform.ToLower() switch
                {
                    "codeforces" => await _codeforcesService.GetUserStatsAsync(username),
                    "leetcode" => await _leetcodeService.GetUserStatsAsync(username),
                    "codechef" => await _codechefService.GetUserStatsAsync(username),
                    "atcoder" => await _atcoderService.GetUserStatsAsync(username),
                    _ => new PlatformStats { Platform = platform, IsConnected = false }
                };
            }
            catch (Exception ex)
            {
                return new PlatformStats 
                { 
                    Platform = platform, 
                    Username = username,
                    IsConnected = false 
                };
            }
        }

        public async Task<WeeklyStats> GetWeeklyStatsAsync(string platform, string username)
        {
            if (string.IsNullOrEmpty(username))
                return new WeeklyStats();

            try
            {
                return platform.ToLower() switch
                {
                    "codeforces" => await _codeforcesService.GetWeeklyStatsAsync(username),
                    "leetcode" => await _leetcodeService.GetWeeklyStatsAsync(username),
                    "codechef" => await _codechefService.GetWeeklyStatsAsync(username),
                    "atcoder" => await _atcoderService.GetWeeklyStatsAsync(username),
                    _ => new WeeklyStats()
                };
            }
            catch (Exception)
            {
                return new WeeklyStats();
            }
        }

        public async Task<bool> TestConnectionAsync(string platform, string username)
        {
            if (string.IsNullOrEmpty(username))
                return false;

            try
            {
                return platform.ToLower() switch
                {
                    "codeforces" => await _codeforcesService.TestConnectionAsync(username),
                    "leetcode" => await _leetcodeService.TestConnectionAsync(username),
                    "codechef" => await _codechefService.TestConnectionAsync(username),
                    "atcoder" => await _atcoderService.TestConnectionAsync(username),
                    _ => false
                };
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<PlatformStats>> GetAllPlatformStatsAsync(UserProfile profile)
        {
            var tasks = new List<Task<PlatformStats>>();

            if (!string.IsNullOrEmpty(profile.Codeforces))
                tasks.Add(GetPlatformStatsAsync("codeforces", profile.Codeforces));

            if (!string.IsNullOrEmpty(profile.LeetCode))
                tasks.Add(GetPlatformStatsAsync("leetcode", profile.LeetCode));

            if (!string.IsNullOrEmpty(profile.Codechef))
                tasks.Add(GetPlatformStatsAsync("codechef", profile.Codechef));

            if (!string.IsNullOrEmpty(profile.Atcoder))
                tasks.Add(GetPlatformStatsAsync("atcoder", profile.Atcoder));

            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }

        public async Task<WeeklyStats> GetCombinedWeeklyStatsAsync(UserProfile profile)
        {
            var tasks = new List<Task<WeeklyStats>>();

            if (!string.IsNullOrEmpty(profile.Codeforces))
                tasks.Add(GetWeeklyStatsAsync("codeforces", profile.Codeforces));

            if (!string.IsNullOrEmpty(profile.LeetCode))
                tasks.Add(GetWeeklyStatsAsync("leetcode", profile.LeetCode));

            if (!string.IsNullOrEmpty(profile.Codechef))
                tasks.Add(GetWeeklyStatsAsync("codechef", profile.Codechef));

            if (!string.IsNullOrEmpty(profile.Atcoder))
                tasks.Add(GetWeeklyStatsAsync("atcoder", profile.Atcoder));

            var results = await Task.WhenAll(tasks);
            
            return new WeeklyStats
            {
                ProblemsSolved = results.Sum(r => r.ProblemsSolved),
                ContestsParticipated = results.Sum(r => r.ContestsParticipated),
                RatingChange = results.Sum(r => r.RatingChange),
                WeekStart = DateTime.UtcNow.AddDays(-7),
                WeekEnd = DateTime.UtcNow
            };
        }

        public void Dispose()
        {
            _codeforcesService?.Dispose();
            _leetcodeService?.Dispose();
            _codechefService?.Dispose();
            _atcoderService?.Dispose();
        }
    }
}
