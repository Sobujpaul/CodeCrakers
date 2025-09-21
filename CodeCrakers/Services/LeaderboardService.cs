using System;
using System.Collections.Generic;
using System.Linq;
using CodeCrakers.Models;
using CodeCrakers.Data;

namespace CodeCrakers.Services
{
    public class LeaderboardService
    {
        private UserProfileRepository _repo = new UserProfileRepository();

        public List<LeaderboardEntry> GetLeaderboard(
            int page = 1, int pageSize = 10,
            string searchName = "", string country = "", string university = "",
            string sortBy = "rating")
        {
            var users = _repo.GetLeaderboard(searchName, country, university, sortBy, page, pageSize);

            var leaderboard = users.Select((u, index) => new LeaderboardEntry
            {
                Rank = (page - 1) * pageSize + index + 1,
                Name = u.DisplayName ?? $"User {u.UserId}",
                HasCodeforces = !string.IsNullOrEmpty(u.Codeforces),
                HasCodeChef = !string.IsNullOrEmpty(u.Codechef),
                HasLeetCode = !string.IsNullOrEmpty(u.LeetCode),
                HasAtCoder = !string.IsNullOrEmpty(u.Atcoder),
                CodeforcesRating = u.TotalRating,
                ProblemsSolved = u.TotalSolved
            }).ToList();

            return leaderboard;
        }
    }

    public class LeaderboardEntry
    {
        public int Rank { get; set; }
        public string Name { get; set; }
        public bool HasCodeforces { get; set; }
        public bool HasCodeChef { get; set; }
        public bool HasLeetCode { get; set; }
        public bool HasAtCoder { get; set; }
        public int CodeforcesRating { get; set; }
        public int ProblemsSolved { get; set; }
    }
}
