using CodeCrakers.Data;
using System;
using System.Windows;

namespace CodeCrakers.Views
{
    public partial class SetNewPasswordWindow : Window
    {
        private readonly int _userId;
        public SetNewPasswordWindow(int userId, string? suggestedUsername = null)
        {
            InitializeComponent();
            if (!string.IsNullOrWhiteSpace(suggestedUsername))
            {
                txtUsername.Text = suggestedUsername;
            }
            _userId = userId;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var username = txtUsername.Text.Trim();
                var pass = txtNewPass.Password.Trim();
                var confirm = txtConfirmPass.Password.Trim();
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(pass) || string.IsNullOrWhiteSpace(confirm))
                {
                    MessageBox.Show("All fields required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (pass != confirm)
                {
                    MessageBox.Show("Passwords do not match.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                var userRepo = new UserRepository();
                var user = userRepo.GetById(_userId);
                if (user == null)
                {
                    MessageBox.Show("User not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (!string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Username does not match this account.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                user.PasswordHash = PasswordHasher.Hash(pass);
                if (userRepo.UpdateUser(user))
                {
                    MessageBox.Show("Password updated.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Update failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
