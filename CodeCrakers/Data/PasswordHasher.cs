using System.Security.Cryptography;
using System.Text;

namespace CodeCrakers.Data
{
    public static class PasswordHasher
    {
        // Hash a plain password with SHA256
        public static string Hash(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                sb.Append(b.ToString("x2")); // convert to hex string
            return sb.ToString();
        }
    }
}
