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
            string sortBy = "rating",
            bool includeRegisteredUsers = true,
            string? onlyExternalAddedBy = null)
        {
            // 1) Build a DB snapshot for ALL matched users (fast, no API calls)
            var allEntries = new List<LeaderboardEntry>();

            if (includeRegisteredUsers)
            {
                var registeredUsers = _userRepo.GetLeaderboardAll(searchName, country, university, sortBy);
                foreach (var u in registeredUsers)
                {
                    // CF-only requirement: we still display only CF metrics; for snapshot, use stored totals
                    allEntries.Add(new LeaderboardEntry
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
                    });
                }
            }

            var externalUsers = _externalRepo.GetAll();
            var filteredExternalUsers = externalUsers.Where(eu =>
                (string.IsNullOrEmpty(searchName) || eu.DisplayName.Contains(searchName, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrEmpty(country) || (eu.Country?.Contains(country, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (string.IsNullOrEmpty(university) || (eu.University?.Contains(university, StringComparison.OrdinalIgnoreCase) ?? false))
            );

            if (!string.IsNullOrEmpty(onlyExternalAddedBy))
            {
                filteredExternalUsers = filteredExternalUsers
                    .Where(eu => string.Equals(eu.AddedBy, onlyExternalAddedBy, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var eu in filteredExternalUsers)
            {
                // Snapshot from DB only (fast)
                allEntries.Add(new LeaderboardEntry
                {
                    Id = eu.Id,
                    Name = eu.DisplayName,
                    HasCodeforces = !string.IsNullOrEmpty(eu.Codeforces),
                    HasCodeChef = !string.IsNullOrEmpty(eu.Codechef),
                    HasLeetCode = !string.IsNullOrEmpty(eu.LeetCode),
                    HasAtCoder = !string.IsNullOrEmpty(eu.Atcoder),
                    CurrentRating = eu.MaxRating, // snapshot uses stored values
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
                });
            }

            // If sorting by Current Rating, refresh CurrentRating for ALL matched entries (bounded by your 50-user cap)
            var sortKey = sortBy.ToLower();
            if (sortKey == "currentrating")
            {
                foreach (var entry in allEntries)
                {
                    if (!string.IsNullOrEmpty(entry.CodeforcesUsername))
                    {
                        try
                        {
                            var cfStats = await _apiManager.GetPlatformStatsAsync("codeforces", entry.CodeforcesUsername);
                            if (cfStats?.IsConnected == true)
                            {
                                entry.CurrentRating = cfStats.Rating;
                                entry.MaxRating = Math.Max(entry.MaxRating, cfStats.MaxRating);
                            }
                            else
                            {
                                entry.CurrentRating = 0;
                            }
                        }
                        catch
                        {
                            entry.CurrentRating = 0;
                        }
                    }
                    else
                    {
                        entry.CurrentRating = 0;
                        entry.MaxRating = 0;
                    }
                }

                allEntries = allEntries
                    .OrderByDescending(e => e.CurrentRating)
                    .ThenByDescending(e => e.ProblemsSolved)
                    .ToList();

                var currentRatedPage = allEntries
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                for (int i = 0; i < currentRatedPage.Count; i++)
                {
                    currentRatedPage[i].Rank = (page - 1) * pageSize + i + 1;
                }

                return currentRatedPage;
            }

            // 2) Apply global sort using snapshot (fast) for other sort keys
            allEntries = sortKey switch
            {
                "rating" => allEntries.OrderByDescending(e => e.MaxRating).ThenByDescending(e => e.ProblemsSolved).ToList(),
                "solved" => allEntries.OrderByDescending(e => e.ProblemsSolved).ThenByDescending(e => e.CurrentRating).ToList(),
                "name" => allEntries.OrderBy(e => e.Name).ToList(),
                _ => allEntries.OrderByDescending(e => e.CurrentRating).ThenByDescending(e => e.ProblemsSolved).ToList()
            };

            // 3) Page the snapshot
            var paginatedEntries = allEntries
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            for (int i = 0; i < paginatedEntries.Count; i++)
            {
                paginatedEntries[i].Rank = (page - 1) * pageSize + i + 1;
            }

            // 4) For visible page only, fetch fresh CF stats to update CurrentRating/MaxRating lightly
            foreach (var entry in paginatedEntries)
            {
                if (!string.IsNullOrEmpty(entry.CodeforcesUsername))
                {
                    try
                    {
                        var cfStats = await _apiManager.GetPlatformStatsAsync("codeforces", entry.CodeforcesUsername);
                        if (cfStats?.IsConnected == true)
                        {
                            entry.CurrentRating = cfStats.Rating;
                            entry.MaxRating = Math.Max(entry.MaxRating, cfStats.MaxRating);
                            // Keep ProblemsSolved from DB snapshot for speed
                        }
                        else
                        {
                            entry.CurrentRating = 0;
                            // Keep existing MaxRating snapshot if any
                        }
                    }
                    catch
                    {
                        // On failure, keep snapshot values
                    }
                }
                else
                {
                    // No CF username -> CF-only metric is 0
                    entry.CurrentRating = 0;
                    entry.MaxRating = 0;
                }
            }

            return paginatedEntries;
        }

        public async Task<ExternalUser> AddExternalUserAsync(string displayName, string? codeforces = null, 
            string? leetcode = null, string? codechef = null, string? atcoder = null, 
            string? country = null, string? university = null, string addedBy = "Unknown")
        {
            // Enforce per-user limit (max 50 added users)
            var existingByUser = _externalRepo.GetAll().Where(eu => string.Equals(eu.AddedBy, addedBy, StringComparison.OrdinalIgnoreCase)).Count();
            if (existingByUser >= 50)
            {
                throw new InvalidOperationException("You have reached the maximum limit of 50 added users.");
            }

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
            try
            {
                // CF-only per requirement
                if (!string.IsNullOrEmpty(externalUser.Codeforces))
                {
                    var cfStats = await _apiManager.GetPlatformStatsAsync("codeforces", externalUser.Codeforces);
                    if (cfStats?.IsConnected == true)
                    {
                        externalUser.MaxRating = cfStats.MaxRating;
                        externalUser.TotalSolved = cfStats.ProblemsSolved;
                        return;
                    }
                }

                // Fallback when no CF or failed to fetch
                externalUser.MaxRating = 0;
                externalUser.TotalSolved = 0;
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

        public int GetTotalCount(
            string searchName = "",
            string country = "",
            string university = "",
            bool includeRegisteredUsers = true,
            string? onlyExternalAddedBy = null)
        {
            int registeredCount = 0;
            if (includeRegisteredUsers)
            {
                registeredCount = _userRepo
                    .GetLeaderboard(searchName, country, university, "rating", 1, int.MaxValue)
                    .Count();
            }

            var externalQuery = _externalRepo.GetAll().Where(eu =>
                (string.IsNullOrEmpty(searchName) || eu.DisplayName.Contains(searchName, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrEmpty(country) || (eu.Country?.Contains(country, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (string.IsNullOrEmpty(university) || (eu.University?.Contains(university, StringComparison.OrdinalIgnoreCase) ?? false))
            );

            if (!string.IsNullOrEmpty(onlyExternalAddedBy))
            {
                externalQuery = externalQuery.Where(eu => string.Equals(eu.AddedBy, onlyExternalAddedBy, StringComparison.OrdinalIgnoreCase));
            }

            var externalCount = externalQuery.Count();
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
