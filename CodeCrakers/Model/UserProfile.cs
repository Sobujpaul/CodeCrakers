namespace CodeCrakers.Models
{
    public class UserProfile
    {
        public int UserId { get; set; }      // Foreign key -> Users(Id)
        public string Codeforces { get; set; }
        public string Codechef { get; set; }
        public string Atcoder { get; set; }
    }
}
