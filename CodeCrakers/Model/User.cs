namespace CodeCrakers.Models
{
    public class User
    {
        public int Id { get; set; }              // Primary Key
        public string Username { get; set; } = string.Empty;     // Unique
        public string Email { get; set; } = string.Empty;        // Unique
        public string PasswordHash { get; set; } = string.Empty; // SHA256 hashed password
    }
}
