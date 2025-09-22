using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using CodeCrakers.Models;

namespace CodeCrakers.Services
{
    public class ContestApiService : BaseApiService
    {
        public ContestApiService() : base("https://codeforces.com/api/") { }

        public async Task<List<Contest>> GetUpcomingContestsAsync()
        {
            var contests = new List<Contest>();

            try
            {
                // Fetch from different platforms
                var codeforcesContests = await GetCodeforcesContestsAsync();
                var atcoderContests = await GetAtCoderContestsAsync();
                var codechefContests = await GetCodeChefContestsAsync();

                contests.AddRange(codeforcesContests);
                contests.AddRange(atcoderContests);
                contests.AddRange(codechefContests);

                // Sort by start time
                contests = contests.OrderBy(c => c.StartTime).ToList();
            }
            catch (Exception ex)
            {
                // Log error but continue with partial results
                Console.WriteLine($"Error fetching contests: {ex.Message}");
            }

            return contests;
        }

        private async Task<List<Contest>> GetCodeforcesContestsAsync()
        {
            var contests = new List<Contest>();

            try
            {
                var response = await GetAsync<CodeforcesContestResponse>("contest.list?gym=false");
                
                if (response?.Status == "OK" && response.Result != null)
                {
                    var upcomingContests = response.Result
                        .Where(c => c.Phase == "BEFORE")
                        .Take(10) // Limit to next 10 contests
                        .ToList();

                    foreach (var cfContest in upcomingContests)
                    {
                        contests.Add(new Contest
                        {
                            Name = cfContest.Name,
                            Platform = "Codeforces",
                            StartTime = DateTimeOffset.FromUnixTimeSeconds(cfContest.StartTimeSeconds).DateTime,
                            DurationSeconds = (int)cfContest.DurationSeconds,
                            PlatformContestId = cfContest.Id,
                            Url = $"https://codeforces.com/contest/{cfContest.Id}",
                            Type = DetermineCodeforcesContestType(cfContest.Name),
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching Codeforces contests: {ex.Message}");
            }

            return contests;
        }

        private async Task<List<Contest>> GetAtCoderContestsAsync()
        {
            var contests = new List<Contest>();

            try
            {
                // AtCoder doesn't have a public API, so we'll create some sample upcoming contests
                // In a real implementation, you might scrape their website or use unofficial APIs
                var now = DateTime.UtcNow;
                
                // Sample AtCoder contests (in real implementation, fetch from their site)
                if (now.Hour < 12) // Morning - add some sample contests
                {
                    contests.Add(new Contest
                    {
                        Name = "AtCoder Beginner Contest 330",
                        Platform = "AtCoder",
                        StartTime = now.Date.AddDays(1).AddHours(21), // Tomorrow 9 PM UTC
                        DurationSeconds = 6000, // 100 minutes
                        PlatformContestId = 330,
                        Url = "https://atcoder.jp/contests/abc330",
                        Type = ContestType.AtCoderBeginnerContest,
                        CreatedAt = DateTime.UtcNow
                    });

                    contests.Add(new Contest
                    {
                        Name = "AtCoder Regular Contest 167",
                        Platform = "AtCoder",
                        StartTime = now.Date.AddDays(7).AddHours(21), // Next week
                        DurationSeconds = 7200, // 120 minutes
                        PlatformContestId = 167,
                        Url = "https://atcoder.jp/contests/arc167",
                        Type = ContestType.AtCoderRegularContest,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching AtCoder contests: {ex.Message}");
            }

            return contests;
        }

        private async Task<List<Contest>> GetCodeChefContestsAsync()
        {
            var contests = new List<Contest>();

            try
            {
                // CodeChef doesn't have a reliable public API for contests
                // We'll create sample upcoming contests based on their typical schedule
                var now = DateTime.UtcNow;
                
                // CodeChef typically has contests on specific days
                var nextFriday = GetNextWeekday(now, DayOfWeek.Friday).AddHours(19); // 7:30 PM IST = 2:00 PM UTC
                var nextSunday = GetNextWeekday(now, DayOfWeek.Sunday).AddHours(20); // 8:30 PM IST = 3:00 PM UTC

                if (nextFriday > now)
                {
                    contests.Add(new Contest
                    {
                        Name = "CodeChef Starters",
                        Platform = "CodeChef",
                        StartTime = nextFriday,
                        DurationSeconds = 10800, // 3 hours
                        PlatformContestId = GetCodeChefId(),
                        Url = "https://www.codechef.com/contests",
                        Type = ContestType.CodeChefCookOff,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                if (nextSunday > now)
                {
                    contests.Add(new Contest
                    {
                        Name = "CodeChef Cook-Off",
                        Platform = "CodeChef",
                        StartTime = nextSunday,
                        DurationSeconds = 9000, // 2.5 hours
                        PlatformContestId = GetCodeChefId(),
                        Url = "https://www.codechef.com/contests",
                        Type = ContestType.CodeChefCookOff,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating CodeChef contests: {ex.Message}");
            }

            return contests;
        }

        private ContestType DetermineCodeforcesContestType(string contestName)
        {
            var name = contestName.ToLower();
            if (name.Contains("div. 1")) return ContestType.Div1;
            if (name.Contains("div. 2")) return ContestType.Div2;
            if (name.Contains("div. 3")) return ContestType.Div3;
            if (name.Contains("div. 4")) return ContestType.Div4;
            if (name.Contains("educational")) return ContestType.Educational;
            if (name.Contains("global")) return ContestType.Global;
            return ContestType.Regular;
        }

        private DateTime GetNextWeekday(DateTime start, DayOfWeek day)
        {
            int daysToAdd = ((int)day - (int)start.DayOfWeek + 7) % 7;
            if (daysToAdd == 0 && start.Hour >= 20) daysToAdd = 7; // If it's already that day and past time, get next week
            return start.Date.AddDays(daysToAdd);
        }

        private int GetCodeChefId()
        {
            return new Random().Next(1000, 9999);
        }

        public async Task<Contest?> GetContestDetailsAsync(string platform, int contestId)
        {
            try
            {
                switch (platform.ToLower())
                {
                    case "codeforces":
                        return await GetCodeforcesContestDetailsAsync(contestId);
                    case "atcoder":
                        return await GetAtCoderContestDetailsAsync(contestId);
                    case "codechef":
                        return await GetCodeChefContestDetailsAsync(contestId);
                    default:
                        return null;
                }
            }
            catch
            {
                return null;
            }
        }

        private async Task<Contest?> GetCodeforcesContestDetailsAsync(int contestId)
        {
            try
            {
                var response = await GetAsync<CodeforcesContestResponse>($"contest.list?contestId={contestId}");
                
                if (response?.Status == "OK" && response.Result?.Any() == true)
                {
                    var cfContest = response.Result.First();
                    return new Contest
                    {
                        Name = cfContest.Name,
                        Platform = "Codeforces",
                        StartTime = DateTimeOffset.FromUnixTimeSeconds(cfContest.StartTimeSeconds).DateTime,
                        DurationSeconds = (int)cfContest.DurationSeconds,
                        PlatformContestId = cfContest.Id,
                        Url = $"https://codeforces.com/contest/{cfContest.Id}",
                        Type = DetermineCodeforcesContestType(cfContest.Name),
                        CreatedAt = DateTime.UtcNow
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching Codeforces contest {contestId}: {ex.Message}");
            }

            return null;
        }

        private async Task<Contest?> GetAtCoderContestDetailsAsync(int contestId)
        {
            // Placeholder for AtCoder contest details
            // In real implementation, scrape or use unofficial API
            return null;
        }

        private async Task<Contest?> GetCodeChefContestDetailsAsync(int contestId)
        {
            // Placeholder for CodeChef contest details
            // In real implementation, scrape or use unofficial API
            return null;
        }
    }

    // Codeforces API response models
    public class CodeforcesContestResponse
    {
        public string Status { get; set; } = "";
        public List<CodeforcesContest>? Result { get; set; }
    }

    public class CodeforcesContest
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Phase { get; set; } = "";
        public bool Frozen { get; set; }
        public long DurationSeconds { get; set; }
        public long StartTimeSeconds { get; set; }
        public long RelativeTimeSeconds { get; set; }
        public string? PreparedBy { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? Description { get; set; }
        public int? Difficulty { get; set; }
        public string? Kind { get; set; }
        public string? IcpcRegion { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? Season { get; set; }
    }
}