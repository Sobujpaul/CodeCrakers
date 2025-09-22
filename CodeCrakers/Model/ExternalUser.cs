namespace CodeCrakers.Models
{
    public class ExternalUser
    {
        public int Id { get; set; }              // Primary Key
        public string DisplayName { get; set; }  // Display name for leaderboard
        public string? Codeforces { get; set; }  // Codeforces username
        public string? LeetCode { get; set; }    // LeetCode username
        public string? Codechef { get; set; }    // CodeChef username
        public string? Atcoder { get; set; }     // AtCoder username
        public string? Country { get; set; }     // Optional country
        public string? University { get; set; }  // Optional university
        public DateTime AddedAt { get; set; }    // When the user was added
        public string AddedBy { get; set; }      // Who added this user (username)
        
        // Computed properties for leaderboard
        public int MaxRating { get; set; }       // Highest rating across all platforms
        public int TotalSolved { get; set; }     // Total problems solved across platforms
        public bool IsExternal { get; set; } = true; // Flag to identify external users in leaderboard
    }
}