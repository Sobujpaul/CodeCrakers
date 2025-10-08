using System;

namespace CodeCrakers.Models
{
    public class Contest
    {
        public int Id { get; set; }                    // Primary Key
        public string Name { get; set; } = string.Empty;               // Contest name
        public string Platform { get; set; } = string.Empty;          // Platform (Codeforces, AtCoder, etc.)
        public DateTime StartTime { get; set; }       // Contest start time (UTC)
        public int DurationSeconds { get; set; }      // Contest duration in seconds
        public string? Url { get; set; }              // Contest URL
        public int PlatformContestId { get; set; }    // Contest ID from the platform
        public string? Description { get; set; }      // Contest description
        public ContestType Type { get; set; }         // Contest type
        public DateTime CreatedAt { get; set; }       // When this was added to our database
        public bool IsActive { get; set; } = true;    // Whether contest is still relevant
        
        // Computed properties
        public DateTime EndTime => StartTime.AddSeconds(DurationSeconds);
        public TimeSpan Duration => TimeSpan.FromSeconds(DurationSeconds);
        public bool IsUpcoming => StartTime > DateTime.UtcNow;
        public bool IsOngoing => DateTime.UtcNow >= StartTime && DateTime.UtcNow <= EndTime;
        public bool IsFinished => DateTime.UtcNow > EndTime;
        public TimeSpan TimeUntilStart => IsUpcoming ? StartTime - DateTime.UtcNow : TimeSpan.Zero;
        public string FormattedDuration => Duration.ToString(@"h\:mm");
    }
    
    public enum ContestType
    {
        Unknown,
        Regular,
        Div1,
        Div2,
        Div3,
        Div4,
        Educational,
        Global,
        AtCoderBeginnerContest,
        AtCoderRegularContest,
        AtCoderGrandContest,
        CodeChefLong,
        CodeChefCookOff,
        CodeChefLunchTime,
        LeetCodeWeekly,
        LeetCodeBiweekly
    }
}