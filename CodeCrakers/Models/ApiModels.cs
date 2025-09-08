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
        public string Handle { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string Organization { get; set; }
        public int Rating { get; set; }
        public int MaxRating { get; set; }
        public string Rank { get; set; }
        public string MaxRank { get; set; }
        public long RegistrationTimeSeconds { get; set; }
        public long LastOnlineTimeSeconds { get; set; }
        public int FriendOfCount { get; set; }
        public string TitlePhoto { get; set; }
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

    public class Problem
    {
        public int ContestId { get; set; }
        public string Index { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public double? Rating { get; set; }
        public List<string> Tags { get; set; }
    }

    // LeetCode API Models (using GraphQL-like structure)
    public class LeetCodeUserInfo
    {
        public LeetCodeData Data { get; set; }
    }

    public class LeetCodeData
    {
        public LeetCodeUser User { get; set; }
    }

    public class LeetCodeUser
    {
        public LeetCodeUserProfile Profile { get; set; }
        public LeetCodeSubmissionStats SubmissionStats { get; set; }
    }

    public class LeetCodeUserProfile
    {
        public string UserName { get; set; }
        public string RealName { get; set; }
        public string AboutMe { get; set; }
        public string Location { get; set; }
        public string WebsiteUrl { get; set; }
        public string School { get; set; }
        public string Company { get; set; }
        public string JobTitle { get; set; }
        public string SkillTags { get; set; }
        public string PostViewCount { get; set; }
        public string PostViewCountDiff { get; set; }
        public string Reputation { get; set; }
        public string ReputationDiff { get; set; }
        public string SolutionCount { get; set; }
        public string SolutionCountDiff { get; set; }
        public string CategoryDiscussCount { get; set; }
        public string CategoryDiscussCountDiff { get; set; }
    }

    public class LeetCodeSubmissionStats
    {
        public List<LeetCodeSubmissionStat> AcSubmissionNum { get; set; }
        public List<LeetCodeSubmissionStat> TotalSubmissionNum { get; set; }
    }

    public class LeetCodeSubmissionStat
    {
        public string Difficulty { get; set; }
        public int Count { get; set; }
        public int Submissions { get; set; }
    }

    // CodeChef API Models (using web scraping approach)
    public class CodeChefUserInfo
    {
        public string Username { get; set; }
        public string Name { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public string StudentProfessional { get; set; }
        public string Institution { get; set; }
        public int Rating { get; set; }
        public int MaxRating { get; set; }
        public string GlobalRank { get; set; }
        public string CountryRank { get; set; }
        public string RatingDiv { get; set; }
        public int ProblemsSolved { get; set; }
        public int ProblemsPartiallySolved { get; set; }
    }

    // AtCoder API Models
    public class AtCoderUserInfo
    {
        public string UserScreenName { get; set; }
        public string Rating { get; set; }
        public string HighestRating { get; set; }
        public string RatedMatches { get; set; }
        public string LastCompeted { get; set; }
        public string Affiliation { get; set; }
        public string Country { get; set; }
        public string BirthYear { get; set; }
        public string TwitterId { get; set; }
        public string TopCoderId { get; set; }
        public string CodeforcesId { get; set; }
    }

    // Common models for statistics
    public class PlatformStats
    {
        public string Platform { get; set; }
        public string Username { get; set; }
        public int Rating { get; set; }
        public int MaxRating { get; set; }
        public int ProblemsSolved { get; set; }
        public int ContestsParticipated { get; set; }
        public DateTime LastActivity { get; set; }
        public bool IsConnected { get; set; }
    }

    public class WeeklyStats
    {
        public int ProblemsSolved { get; set; }
        public int ContestsParticipated { get; set; }
        public int RatingChange { get; set; }
        public DateTime WeekStart { get; set; }
        public DateTime WeekEnd { get; set; }
    }
}
