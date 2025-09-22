using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeCrakers.Models;
using CodeCrakers.Data;

namespace CodeCrakers.Services
{
    public class LeaderboardService
    {
        private readonly UserProfileRepository _userRepo = new UserProfileRepository();
        private readonly ExternalUserRepository _externalRepo = new ExternalUserRepository();
        private readonly PlatformApiManager _apiManager = new PlatformApiManager();

        public List<LeaderboardEntry> GetLeaderboard(
            int page = 1, int pageSize = 10,
            string searchName = "", string country = "", string university = "",
            string sortBy = "rating")
        {
            // Get registered users
            var registeredUsers = _userRepo.GetLeaderboard(searchName, country, university, sortBy, page, pageSize);
            
            // Get external users
            var externalUsers = _externalRepo.GetAll();
            
            // Convert registered users to leaderboard entries
            var registeredEntries = registeredUsers.Select(u => new LeaderboardEntry
            {
                Id = u.UserId,
                Name = u.DisplayName ?? $"User {u.UserId}",
                HasCodeforces = !string.IsNullOrEmpty(u.Codeforces),
                HasCodeChef = !string.IsNullOrEmpty(u.Codechef),
                HasLeetCode = !string.IsNullOrEmpty(u.LeetCode),
                HasAtCoder = !string.IsNullOrEmpty(u.Atcoder),
                MaxRating = u.TotalRating,
                ProblemsSolved = u.TotalSolved,
                Country = u.Country,
                University = u.University,
                IsExternal = false,
                CodeforcesUsername = u.Codeforces,
                LeetCodeUsername = u.LeetCode,
                CodeChefUsername = u.Codechef,
                AtCoderUsername = u.Atcoder
            }).ToList();
            
            // Convert external users to leaderboard entries
            var externalEntries = externalUsers.Where(eu => 
                (string.IsNullOrEmpty(searchName) || eu.DisplayName.Contains(searchName, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrEmpty(country) || (eu.Country?.Contains(country, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (string.IsNullOrEmpty(university) || (eu.University?.Contains(university, StringComparison.OrdinalIgnoreCase) ?? false))
            ).Select(eu => new LeaderboardEntry
            {
                Id = eu.Id,
                Name = eu.DisplayName,
                HasCodeforces = !string.IsNullOrEmpty(eu.Codeforces),
                HasCodeChef = !string.IsNullOrEmpty(eu.Codechef),
                HasLeetCode = !string.IsNullOrEmpty(eu.LeetCode),
                HasAtCoder = !string.IsNullOrEmpty(eu.Atcoder),
                MaxRating = eu.MaxRating,
                ProblemsSolved = eu.TotalSolved,
                Country = eu.Country,
                University = eu.University,
                IsExternal = true,
                AddedBy = eu.AddedBy,
                CodeforcesUsername = eu.Codeforces,
                LeetCodeUsername = eu.LeetCode,
                CodeChefUsername = eu.Codechef,
                AtCoderUsername = eu.Atcoder
            }).ToList();
            
            // Combine all entries
            var allEntries = registeredEntries.Concat(externalEntries).ToList();
            
            // Sort based on criteria
            allEntries = sortBy.ToLower() switch
            {
                "rating" => allEntries.OrderByDescending(e => e.MaxRating).ThenByDescending(e => e.ProblemsSolved).ToList(),
                "solved" => allEntries.OrderByDescending(e => e.ProblemsSolved).ThenByDescending(e => e.MaxRating).ToList(),
                "name" => allEntries.OrderBy(e => e.Name).ToList(),
                _ => allEntries.OrderByDescending(e => e.MaxRating).ThenByDescending(e => e.ProblemsSolved).ToList()
            };
            
            // Apply pagination and set ranks
            var paginatedEntries = allEntries.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            
            for (int i = 0; i < paginatedEntries.Count; i++)
            {
                paginatedEntries[i].Rank = (page - 1) * pageSize + i + 1;
            }
            
            return paginatedEntries;
        }

        public async Task<ExternalUser> AddExternalUserAsync(string displayName, string? codeforces = null, 
            string? leetcode = null, string? codechef = null, string? atcoder = null, 
            string? country = null, string? university = null, string addedBy = "Unknown")
        {
            // Validate that at least one platform username is provided
            if (string.IsNullOrEmpty(codeforces) && string.IsNullOrEmpty(leetcode) && 
                string.IsNullOrEmpty(codechef) && string.IsNullOrEmpty(atcoder))
            {
                throw new ArgumentException("At least one platform username must be provided.");
            }

            var externalUser = new ExternalUser
            {
                DisplayName = displayName,
                Codeforces = string.IsNullOrWhiteSpace(codeforces) ? null : codeforces.Trim(),
                LeetCode = string.IsNullOrWhiteSpace(leetcode) ? null : leetcode.Trim(),
                Codechef = string.IsNullOrWhiteSpace(codechef) ? null : codechef.Trim(),
                Atcoder = string.IsNullOrWhiteSpace(atcoder) ? null : atcoder.Trim(),
                Country = string.IsNullOrWhiteSpace(country) ? null : country.Trim(),
                University = string.IsNullOrWhiteSpace(university) ? null : university.Trim(),
                AddedAt = DateTime.Now,
                AddedBy = addedBy
            };

            // Fetch stats from APIs to get initial ratings
            await UpdateExternalUserStatsAsync(externalUser);

            // Add to database
            _externalRepo.Add(externalUser);

            return externalUser;
        }

        public async Task UpdateExternalUserStatsAsync(ExternalUser externalUser)
        {
            var platformStats = new List<PlatformStats>();

            try
            {
                // Fetch stats from each platform
                if (!string.IsNullOrEmpty(externalUser.Codeforces))
                {
                    var cfStats = await _apiManager.GetPlatformStatsAsync("codeforces", externalUser.Codeforces);
                    if (cfStats?.IsConnected == true) platformStats.Add(cfStats);
                }

                if (!string.IsNullOrEmpty(externalUser.LeetCode))
                {
                    var lcStats = await _apiManager.GetPlatformStatsAsync("leetcode", externalUser.LeetCode);
                    if (lcStats?.IsConnected == true) platformStats.Add(lcStats);
                }

                if (!string.IsNullOrEmpty(externalUser.Codechef))
                {
                    var ccStats = await _apiManager.GetPlatformStatsAsync("codechef", externalUser.Codechef);
                    if (ccStats?.IsConnected == true) platformStats.Add(ccStats);
                }

                if (!string.IsNullOrEmpty(externalUser.Atcoder))
                {
                    var acStats = await _apiManager.GetPlatformStatsAsync("atcoder", externalUser.Atcoder);
                    if (acStats?.IsConnected == true) platformStats.Add(acStats);
                }

                // Calculate max rating and total problems solved
                externalUser.MaxRating = platformStats.Any() ? platformStats.Max(s => s.MaxRating) : 0;
                externalUser.TotalSolved = platformStats.Sum(s => s.ProblemsSolved);
            }
            catch (Exception)
            {
                // If API calls fail, set default values
                externalUser.MaxRating = 0;
                externalUser.TotalSolved = 0;
            }
        }

        public async Task RefreshExternalUserStatsAsync(int externalUserId)
        {
            var externalUser = _externalRepo.GetById(externalUserId);
            if (externalUser == null) return;

            await UpdateExternalUserStatsAsync(externalUser);
            _externalRepo.UpdateStats(externalUserId, externalUser.MaxRating, externalUser.TotalSolved);
        }

        public void RemoveExternalUser(int externalUserId)
        {
            _externalRepo.Delete(externalUserId);
        }

        public List<ExternalUser> GetExternalUsers()
        {
            return _externalRepo.GetAll();
        }

        public int GetTotalCount(string searchName = "", string country = "", string university = "")
        {
            var registeredCount = _userRepo.GetLeaderboard(searchName, country, university, "rating", 1, int.MaxValue).Count();
            var externalCount = _externalRepo.GetAll().Where(eu =>
                (string.IsNullOrEmpty(searchName) || eu.DisplayName.Contains(searchName, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrEmpty(country) || (eu.Country?.Contains(country, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (string.IsNullOrEmpty(university) || (eu.University?.Contains(university, StringComparison.OrdinalIgnoreCase) ?? false))
            ).Count();
            
            return registeredCount + externalCount;
        }
    }

    public class LeaderboardEntry
    {
        public int Id { get; set; }
        public int Rank { get; set; }
        public string Name { get; set; }
        public bool HasCodeforces { get; set; }
        public bool HasCodeChef { get; set; }
        public bool HasLeetCode { get; set; }
        public bool HasAtCoder { get; set; }
        public int MaxRating { get; set; }        // Updated from CodeforcesRating
        public int ProblemsSolved { get; set; }
        public string? Country { get; set; }
        public string? University { get; set; }
        public bool IsExternal { get; set; }      // Flag to identify external users
        public string? AddedBy { get; set; }      // Who added this external user
        
        // Platform usernames for display
        public string? CodeforcesUsername { get; set; }
        public string? LeetCodeUsername { get; set; }
        public string? CodeChefUsername { get; set; }
        public string? AtCoderUsername { get; set; }
        
        // Backward compatibility
        public int CodeforcesRating => MaxRating;
    }
}
