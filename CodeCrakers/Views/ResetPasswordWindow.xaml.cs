using CodeCrakers.Data;
using System;
using System.Windows;

namespace CodeCrakers.Views
{
    public partial class ResetPasswordWindow : Window
    {
        public ResetPasswordWindow()
        {
            InitializeComponent();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var token = txtToken.Text.Trim();
                var newPass = txtNewPass.Password.Trim();
                var confirmPass = txtConfirmPass.Password.Trim();

                if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPass) || string.IsNullOrWhiteSpace(confirmPass))
                {
                    MessageBox.Show("Please fill in all fields.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (newPass != confirmPass)
                {
                    MessageBox.Show("Passwords do not match.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var resetRepo = new PasswordResetRepository();
                var userId = resetRepo.ValidateToken(token);
                if (!userId.HasValue)
                {
                    MessageBox.Show("Invalid or expired token.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var userRepo = new UserRepository();
                var user = userRepo.GetById(userId.Value);
                if (user == null)
                {
                    MessageBox.Show("User not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                user.PasswordHash = PasswordHasher.Hash(newPass);
                if (userRepo.UpdateUser(user))
                {
                    resetRepo.MarkTokenUsed(token);
                    MessageBox.Show("Password updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Failed to update password.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Reset error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
