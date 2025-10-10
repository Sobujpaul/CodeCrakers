using System;
using System.Collections.Generic;

namespace CodeCrakers.Models
{
    // Codeforces API Models
    public class CodeforcesUserInfo
    {
        public string? Status { get; set; }
        public List<CodeforcesUser>? Result { get; set; }
    }

    public class CodeforcesUser
    {
        public string Handle { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Organization { get; set; } = string.Empty;
        public int Rating { get; set; }
        public int MaxRating { get; set; }
    public string Rank { get; set; } = string.Empty;
    public string MaxRank { get; set; } = string.Empty;
        public long RegistrationTimeSeconds { get; set; }
        public long LastOnlineTimeSeconds { get; set; }
        public int FriendOfCount { get; set; }
    public string TitlePhoto { get; set; } = string.Empty;
        public int Contribution { get; set; }
        public int ContestCount { get; set; } // This field exists in the API
    }

    public class CodeforcesSubmissionInfo
    {
        public string? Status { get; set; }
        public List<CodeforcesSubmission>? Result { get; set; }
    }

    public class CodeforcesSubmission
    {
        public int Id { get; set; }
        public int ContestId { get; set; }
        public long CreationTimeSeconds { get; set; }
        public string? Verdict { get; set; }
        public string? ProgrammingLanguage { get; set; }
        public Problem? Problem { get; set; }
    }

    public class CodeforcesRatingChangeInfo
    {
        public string? Status { get; set; }
        public List<CodeforcesRatingChangeEntry>? Result { get; set; }
    }

    public class CodeforcesRatingChangeEntry
    {
        public int ContestId { get; set; }
        public string ContestName { get; set; } = string.Empty;
        public int Handle { get; set; }
        public int Rank { get; set; }
        public long RatingUpdateTimeSeconds { get; set; }
        public int OldRating { get; set; }
        public int NewRating { get; set; }
    }

    public class CodeforcesContestResponse
    {
        public string? Status { get; set; }
        public List<CodeforcesContest>? Result { get; set; }
    }

    public class CodeforcesContest
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Phase { get; set; } = string.Empty;
        public bool Frozen { get; set; }
        public long DurationSeconds { get; set; }
        public long StartTimeSeconds { get; set; }
        public long RelativeTimeSeconds { get; set; }
    }

    // Codeforces problemset models
    public class CodeforcesProblemsetResponse
    {
        public string? Status { get; set; }
        public CodeforcesProblemsetResult? Result { get; set; }
    }

    public class CodeforcesProblemsetResult
    {
        public List<Problem> Problems { get; set; } = new();
        public List<CodeforcesProblemStatistics> ProblemStatistics { get; set; } = new();
    }

    public class CodeforcesProblemStatistics
    {
        public int ContestId { get; set; }
        public string Index { get; set; } = string.Empty;
        public int SolvedCount { get; set; }
    }

    public class Problem
    {
        public int ContestId { get; set; }
        public string Index { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public double? Rating { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    // LeetCode API Models (using GraphQL-like structure)
    public class LeetCodeUserInfo
    {
        public LeetCodeData Data { get; set; } = new();
    }

    public class LeetCodeData
    {
        public LeetCodeUser User { get; set; } = new();
    }

    public class LeetCodeUser
    {
        public LeetCodeUserProfile Profile { get; set; } = new();
        public LeetCodeSubmissionStats SubmissionStats { get; set; } = new();
    }

    public class LeetCodeUserProfile
    {
        public string UserName { get; set; } = string.Empty;
        public string RealName { get; set; } = string.Empty;
        public string AboutMe { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string WebsiteUrl { get; set; } = string.Empty;
        public string School { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string SkillTags { get; set; } = string.Empty;
        public string PostViewCount { get; set; } = string.Empty;
        public string PostViewCountDiff { get; set; } = string.Empty;
        public string Reputation { get; set; } = string.Empty;
        public string ReputationDiff { get; set; } = string.Empty;
        public string SolutionCount { get; set; } = string.Empty;
        public string SolutionCountDiff { get; set; } = string.Empty;
        public string CategoryDiscussCount { get; set; } = string.Empty;
        public string CategoryDiscussCountDiff { get; set; } = string.Empty;
    }

    public class LeetCodeSubmissionStats
    {
        public List<LeetCodeSubmissionStat> AcSubmissionNum { get; set; } = new();
        public List<LeetCodeSubmissionStat> TotalSubmissionNum { get; set; } = new();
    }

    public class LeetCodeSubmissionStat
    {
        public string Difficulty { get; set; } = string.Empty;
        public int Count { get; set; }
        public int Submissions { get; set; }
    }

    // CodeChef API Models (using web scraping approach)
    public class CodeChefUserInfo
    {
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string StudentProfessional { get; set; } = string.Empty;
        public string Institution { get; set; } = string.Empty;
        public int Rating { get; set; }
        public int MaxRating { get; set; }
        public string GlobalRank { get; set; } = string.Empty;
        public string CountryRank { get; set; } = string.Empty;
        public string RatingDiv { get; set; } = string.Empty;
        public int ProblemsSolved { get; set; }
        public int ProblemsPartiallySolved { get; set; }
    }

    // AtCoder API Models
    public class AtCoderUserInfo
    {
        public string UserScreenName { get; set; } = string.Empty;
        public string Rating { get; set; } = string.Empty;
        public string HighestRating { get; set; } = string.Empty;
        public string RatedMatches { get; set; } = string.Empty;
        public string LastCompeted { get; set; } = string.Empty;
        public string Affiliation { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string BirthYear { get; set; } = string.Empty;
        public string TwitterId { get; set; } = string.Empty;
        public string TopCoderId { get; set; } = string.Empty;
        public string CodeforcesId { get; set; } = string.Empty;
    }

    // Common models for statistics
    public class PlatformStats
    {
        public string Platform { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public int Rating { get; set; }
        public int MaxRating { get; set; }
        public int ProblemsSolved { get; set; }
        public int ContestsParticipated { get; set; }
        public DateTime LastActivity { get; set; }
        public bool IsConnected { get; set; }
    }

    public class AggregatedUserStats
    {
        public int UserId { get; set; }
        public int Rank { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string University { get; set; } = string.Empty;
        public bool IsHidden { get; set; }
        public string Codeforces { get; set; } = string.Empty;
        public string LeetCode { get; set; } = string.Empty;
        public string Codechef { get; set; } = string.Empty;
        public string Atcoder { get; set; } = string.Empty;

        // Per-platform stats (optional if not connected)
    public PlatformStats CodeforcesStats { get; set; } = new();
    public PlatformStats LeetCodeStats { get; set; } = new();
    public PlatformStats CodechefStats { get; set; } = new();
    public PlatformStats AtcoderStats { get; set; } = new();

        // Derived
        public int TotalProblemsSolved { get; set; }
        public int HighestRating { get; set; }
    }

    public class WeeklyStats
    {
        public int ProblemsSolved { get; set; }
        public int ContestsParticipated { get; set; }
        public int RatingChange { get; set; }
        public DateTime WeekStart { get; set; }
        public DateTime WeekEnd { get; set; }
    }

    // Detailed Analytics Models
    public class DetailedCodeforcesAnalytics
    {
        public CodeforcesUser UserInfo { get; set; } = new();
        public int TotalSubmissions { get; set; }
        public int SolvedProblems { get; set; }
        public int ContestsParticipated { get; set; }
        public double SuccessRate { get; set; }
        public DateTime LastActivity { get; set; }
        
        public Dictionary<string, int> VerdictStats { get; set; } = new Dictionary<string, int>();
        public Dictionary<int, int> DifficultyDistribution { get; set; } = new Dictionary<int, int>();
        public Dictionary<string, int> TopProblemTags { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> LanguageStats { get; set; } = new Dictionary<string, int>();
        
        public List<RecentSubmission> RecentSubmissions { get; set; } = new List<RecentSubmission>();
    }

    public class RecentSubmission
    {
        public string ProblemName { get; set; } = string.Empty;
        public string ProblemIndex { get; set; } = string.Empty;
        public double? ProblemRating { get; set; }
        public string Verdict { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public DateTime SubmissionTime { get; set; }
        public int ContestId { get; set; }
    }

    public class RatingChange
    {
        public string ContestName { get; set; } = string.Empty;
        public int Rank { get; set; }
        public int OldRating { get; set; }
        public int NewRating { get; set; }
        public int RatingChangeValue { get; set; }
        public DateTime ContestTime { get; set; }
    }

    public class AttemptMetrics
    {
        public double AverageAttempts { get; set; }
        public int MaxAttempts { get; set; }
        public double OneShotPercentage { get; set; }
        public int SolvedProblems { get; set; }
    }
}
