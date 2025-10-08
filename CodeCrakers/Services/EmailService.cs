using System;

namespace CodeCrakers.Services
{
    public class EmailService
    {
        // In a real app, configure SMTP (host, port, credentials, SSL) in config.
        // For now, this stub just logs to Debug output.
        public void SendPasswordResetEmail(string toEmail, string token)
        {
            // Construct a reset link or instructions. Since this is a desktop app,
            // we can instruct user to copy token and use 'Reset Password' window.
            var message = $"Hello,\n\nUse the following reset token to change your password: {token}\nThis token expires in 15 minutes.\n\n- CodeCrakers";
            System.Diagnostics.Debug.WriteLine($"[EmailService] Sending password reset token to {toEmail}: {token}");
            // TODO: Implement SMTP send if required.
        }
    }
}
