namespace CodeCrakers.Models
{
    public class User
    {
        public int Id { get; set; }              // Primary Key
        public string Username { get; set; }     // Unique
        public string Email { get; set; }        // Unique
        public string PasswordHash { get; set; } // SHA256 hashed password
    }
}
