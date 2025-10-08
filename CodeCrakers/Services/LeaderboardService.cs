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
            // Fetch all registered users (no pagination) so that sorting is applied globally before slicing
            var registeredUsers = _userRepo.GetLeaderboardAll(searchName, country, university, sortBy);
            // External users (all) then filtered
            var externalUsers = _externalRepo.GetAll();
            
            // Convert registered users to leaderboard entries using cached data (fast loading)
            var registeredEntries = registeredUsers.Select(u => new LeaderboardEntry
            {
                Id = u.UserId,
                Name = u.DisplayName ?? $"User {u.UserId}",
                HasCodeforces = !string.IsNullOrEmpty(u.Codeforces),
                HasCodeChef = !string.IsNullOrEmpty(u.Codechef),
                HasLeetCode = !string.IsNullOrEmpty(u.LeetCode),
                HasAtCoder = !string.IsNullOrEmpty(u.Atcoder),
                CurrentRating = u.TotalRating,
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
            
            // Convert external users to leaderboard entries using stored data (fast loading)
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
                CurrentRating = eu.MaxRating,
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
            
            // Global sort applied BEFORE pagination to ensure correctness
            allEntries = sortBy.ToLower() switch
            {
                "currentrating" => allEntries.OrderByDescending(e => e.CurrentRating).ThenByDescending(e => e.ProblemsSolved).ToList(),
                "rating" => allEntries.OrderByDescending(e => e.MaxRating).ThenByDescending(e => e.ProblemsSolved).ToList(),
                "solved" => allEntries.OrderByDescending(e => e.ProblemsSolved).ThenByDescending(e => e.CurrentRating).ToList(),
                "name" => allEntries.OrderBy(e => e.Name).ToList(),
                _ => allEntries.OrderByDescending(e => e.CurrentRating).ThenByDescending(e => e.ProblemsSolved).ToList()
            };

            var paginatedEntries = allEntries
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            for (int i = 0; i < paginatedEntries.Count; i++)
            {
                paginatedEntries[i].Rank = (page - 1) * pageSize + i + 1;
            }
            
            return paginatedEntries;
        }
        
        // Async method for refreshing with live API data
        public async Task<List<LeaderboardEntry>> GetLeaderboardWithLiveDataAsync(
            int page = 1, int pageSize = 10,
            string searchName = "", string country = "", string university = "",
            string sortBy = "rating")
        {
            // Fetch all for proper global sorting (sorting inside repository would be overridden anyway)
            var registeredUsers = _userRepo.GetLeaderboardAll(searchName, country, university, sortBy);
            var externalUsers = _externalRepo.GetAll();
            
            var allEntries = new List<LeaderboardEntry>();
            
            // Process registered users with live API data
            foreach (var u in registeredUsers)
            {
                var entry = new LeaderboardEntry
                {
                    Id = u.UserId,
                    Name = u.DisplayName ?? $"User {u.UserId}",
                    HasCodeforces = !string.IsNullOrEmpty(u.Codeforces),
                    HasCodeChef = !string.IsNullOrEmpty(u.Codechef),
                    HasLeetCode = !string.IsNullOrEmpty(u.LeetCode),
                    HasAtCoder = !string.IsNullOrEmpty(u.Atcoder),
                    Country = u.Country,
                    University = u.University,
                    IsExternal = false,
                    CodeforcesUsername = u.Codeforces,
                    LeetCodeUsername = u.LeetCode,
                    CodeChefUsername = u.Codechef,
                    AtCoderUsername = u.Atcoder
                };

                try
                {
                    var platformStats = new List<PlatformStats>();

                    if (!string.IsNullOrEmpty(u.Codeforces))
                    {
                        var cfStats = await _apiManager.GetPlatformStatsAsync("codeforces", u.Codeforces);
                        if (cfStats?.IsConnected == true) platformStats.Add(cfStats);
                    }
                    if (!string.IsNullOrEmpty(u.LeetCode))
                    {
                        var lcStats = await _apiManager.GetPlatformStatsAsync("leetcode", u.LeetCode);
                        if (lcStats?.IsConnected == true) platformStats.Add(lcStats);
                    }
                    if (!string.IsNullOrEmpty(u.Codechef))
                    {
                        var ccStats = await _apiManager.GetPlatformStatsAsync("codechef", u.Codechef);
                        if (ccStats?.IsConnected == true) platformStats.Add(ccStats);
                    }
                    if (!string.IsNullOrEmpty(u.Atcoder))
                    {
                        var acStats = await _apiManager.GetPlatformStatsAsync("atcoder", u.Atcoder);
                        if (acStats?.IsConnected == true) platformStats.Add(acStats);
                    }

                    if (platformStats.Any())
                    {
                        entry.CurrentRating = platformStats.Max(s => s.Rating);
                        entry.MaxRating = platformStats.Max(s => s.MaxRating);
                        entry.ProblemsSolved = platformStats.Sum(s => s.ProblemsSolved);
                    }
                    else
                    {
                        entry.CurrentRating = u.TotalRating;
                        entry.MaxRating = u.TotalRating;
                        entry.ProblemsSolved = u.TotalSolved;
                    }
                }
                catch
                {
                    entry.CurrentRating = u.TotalRating;
                    entry.MaxRating = u.TotalRating;
                    entry.ProblemsSolved = u.TotalSolved;
                }

                allEntries.Add(entry);
            }
            
            // Process external users with live data when possible
            var filteredExternalUsers = externalUsers.Where(eu => 
                (string.IsNullOrEmpty(searchName) || eu.DisplayName.Contains(searchName, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrEmpty(country) || (eu.Country?.Contains(country, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (string.IsNullOrEmpty(university) || (eu.University?.Contains(university, StringComparison.OrdinalIgnoreCase) ?? false))
            ).ToList();
            
            foreach (var eu in filteredExternalUsers)
            {
                var entry = new LeaderboardEntry
                {
                    Id = eu.Id,
                    Name = eu.DisplayName,
                    HasCodeforces = !string.IsNullOrEmpty(eu.Codeforces),
                    HasCodeChef = !string.IsNullOrEmpty(eu.Codechef),
                    HasLeetCode = !string.IsNullOrEmpty(eu.LeetCode),
                    HasAtCoder = !string.IsNullOrEmpty(eu.Atcoder),
                    Country = eu.Country,
                    University = eu.University,
                    IsExternal = true,
                    AddedBy = eu.AddedBy,
                    CodeforcesUsername = eu.Codeforces,
                    LeetCodeUsername = eu.LeetCode,
                    CodeChefUsername = eu.Codechef,
                    AtCoderUsername = eu.Atcoder
                };

                try
                {
                    var platformStats = new List<PlatformStats>();

                    if (!string.IsNullOrEmpty(eu.Codeforces))
                    {
                        var cfStats = await _apiManager.GetPlatformStatsAsync("codeforces", eu.Codeforces);
                        if (cfStats?.IsConnected == true) platformStats.Add(cfStats);
                    }
                    if (!string.IsNullOrEmpty(eu.LeetCode))
                    {
                        var lcStats = await _apiManager.GetPlatformStatsAsync("leetcode", eu.LeetCode);
                        if (lcStats?.IsConnected == true) platformStats.Add(lcStats);
                    }
                    if (!string.IsNullOrEmpty(eu.Codechef))
                    {
                        var ccStats = await _apiManager.GetPlatformStatsAsync("codechef", eu.Codechef);
                        if (ccStats?.IsConnected == true) platformStats.Add(ccStats);
                    }
                    if (!string.IsNullOrEmpty(eu.Atcoder))
                    {
                        var acStats = await _apiManager.GetPlatformStatsAsync("atcoder", eu.Atcoder);
                        if (acStats?.IsConnected == true) platformStats.Add(acStats);
                    }

                    if (platformStats.Any())
                    {
                        entry.CurrentRating = platformStats.Max(s => s.Rating);
                        entry.MaxRating = platformStats.Max(s => s.MaxRating);
                        entry.ProblemsSolved = platformStats.Sum(s => s.ProblemsSolved);
                    }
                    else
                    {
                        entry.CurrentRating = eu.MaxRating;
                        entry.MaxRating = eu.MaxRating;
                        entry.ProblemsSolved = eu.TotalSolved;
                    }
                }
                catch
                {
                    entry.CurrentRating = eu.MaxRating;
                    entry.MaxRating = eu.MaxRating;
                    entry.ProblemsSolved = eu.TotalSolved;
                }

                allEntries.Add(entry);
            }
            
            allEntries = sortBy.ToLower() switch
            {
                "currentrating" => allEntries.OrderByDescending(e => e.CurrentRating).ThenByDescending(e => e.ProblemsSolved).ToList(),
                "rating" => allEntries.OrderByDescending(e => e.MaxRating).ThenByDescending(e => e.ProblemsSolved).ToList(),
                "solved" => allEntries.OrderByDescending(e => e.ProblemsSolved).ThenByDescending(e => e.CurrentRating).ToList(),
                "name" => allEntries.OrderBy(e => e.Name).ToList(),
                _ => allEntries.OrderByDescending(e => e.CurrentRating).ThenByDescending(e => e.ProblemsSolved).ToList()
            };

            var paginatedEntries = allEntries
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
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
        public string Name { get; set; } = string.Empty;
        public bool HasCodeforces { get; set; }
        public bool HasCodeChef { get; set; }
        public bool HasLeetCode { get; set; }
        public bool HasAtCoder { get; set; }
        public int CurrentRating { get; set; }    // Current rating from platforms
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
