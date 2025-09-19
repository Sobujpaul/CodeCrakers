namespace CodeCrakers.Models
{
    public class UserProfile
    {
        public int UserId { get; set; }      // Foreign key -> Users(Id)
        public string? Codeforces { get; set; }
        public string? LeetCode { get; set; }
        public string? Codechef { get; set; }
        public string? Atcoder { get; set; }
        public string? Country { get; set; }
        public string? University { get; set; }
        public int IsHidden { get; set; }
    }
}
