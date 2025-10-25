using System.Windows;
using System.Windows.Controls;
using CodeCrakers.Data;
using CodeCrakers.Models;
using System;

namespace CodeCrakers.Views
{
    public partial class ProfilePage : UserControl
    {
        private int _userId;
    private UserRepository _userRepo = null!;
    private UserProfileRepository _profileRepo = null!;
    private User _currentUser = null!;
    private UserProfile _currentProfile = null!;

        public ProfilePage(int userId)
        {
            InitializeComponent();
            _userId = userId;
            _userRepo = new UserRepository();
            _profileRepo = new UserProfileRepository();
            LoadUserData();
        }

        private void LoadUserData()
        {
            try
            {
                // Load user information
                var user = _userRepo.GetById(_userId);
                if (user == null)
                {
                    MessageBox.Show("User not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                _currentUser = user;

                // Load user profile information
                _currentProfile = _profileRepo.GetByUserId(_userId);
                if (_currentProfile == null)
                {
                    // Create a new profile if it doesn't exist
                    _currentProfile = new UserProfile { UserId = _userId };
                }

                // Populate form fields
                txtUsername.Text = _currentUser.Username;
                txtEmail.Text = _currentUser.Email;
                txtCountry.Text = _currentProfile.Country ?? "";
                txtUniversity.Text = _currentProfile.University ?? "";
                chkHideProfile.IsChecked = _currentProfile.IsHidden == 1;

                // Clear password fields
                txtCurrentPassword.Clear();
                txtNewPassword.Clear();
                txtConfirmPassword.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading user data: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate input
                if (!ValidateInput())
                    return;

                // Check if password change is requested
                bool passwordChangeRequested = !string.IsNullOrEmpty(txtNewPassword.Password) || 
                                             !string.IsNullOrEmpty(txtConfirmPassword.Password);

                if (passwordChangeRequested)
                {
                    // Validate password change
                    if (!ValidatePasswordChange())
                        return;
                }

                // Update user information
                bool userUpdated = UpdateUserInfo();
                bool profileUpdated = UpdateProfileInfo();

                if (userUpdated || profileUpdated)
                {
                    MessageBox.Show("✅ Profile updated successfully!", "Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Notify parent to refresh profile info
                    OnProfileUpdated?.Invoke();
                    
                    // Reload data to reflect changes
                    LoadUserData();
                }
                else
                {
                    MessageBox.Show("No changes were made to your profile.", "Information", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving profile: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInput()
        {
            // Validate username
            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                MessageBox.Show("Username cannot be empty.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtUsername.Focus();
                return false;
            }

            // Validate email
            if (string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                MessageBox.Show("Email cannot be empty.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmail.Focus();
                return false;
            }

            // Basic email validation
            if (!txtEmail.Text.Contains("@") || !txtEmail.Text.Contains("."))
            {
                MessageBox.Show("Please enter a valid email address.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmail.Focus();
                return false;
            }

            return true;
        }

        private bool ValidatePasswordChange()
        {
            // Check if current password is provided when changing password
            if (string.IsNullOrEmpty(txtCurrentPassword.Password))
            {
                MessageBox.Show("Please enter your current password to change it.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCurrentPassword.Focus();
                return false;
            }

            // Verify current password
            if (!_userRepo.ValidatePassword(_userId, txtCurrentPassword.Password))
            {
                MessageBox.Show("Current password is incorrect.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCurrentPassword.Focus();
                return false;
            }

            // Validate new password
            if (string.IsNullOrEmpty(txtNewPassword.Password))
            {
                MessageBox.Show("Please enter a new password.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNewPassword.Focus();
                return false;
            }

            if (txtNewPassword.Password.Length < 6)
            {
                MessageBox.Show("New password must be at least 6 characters long.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNewPassword.Focus();
                return false;
            }

            // Confirm password match
            if (txtNewPassword.Password != txtConfirmPassword.Password)
            {
                MessageBox.Show("New password and confirmation do not match.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtConfirmPassword.Focus();
                return false;
            }

            return true;
        }

        private bool UpdateUserInfo()
        {
            bool hasChanges = false;

            // Check if username changed
            if (_currentUser.Username != txtUsername.Text.Trim())
            {
                // Check if new username is already taken
                if (_userRepo.UsernameExists(txtUsername.Text.Trim(), _userId))
                {
                    MessageBox.Show("This username is already taken. Please choose a different one.", 
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtUsername.Focus();
                    return false;
                }
                _currentUser.Username = txtUsername.Text.Trim();
                hasChanges = true;
            }

            // Check if email changed
            if (_currentUser.Email != txtEmail.Text.Trim())
            {
                // Check if new email is already taken
                if (_userRepo.EmailExists(txtEmail.Text.Trim(), _userId))
                {
                    MessageBox.Show("This email is already taken. Please choose a different one.", 
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtEmail.Focus();
                    return false;
                }
                _currentUser.Email = txtEmail.Text.Trim();
                hasChanges = true;
            }

            // Update password if provided
            if (!string.IsNullOrEmpty(txtNewPassword.Password))
            {
                _currentUser.PasswordHash = PasswordHasher.Hash(txtNewPassword.Password);
                hasChanges = true;
            }

            // Save changes if any
            if (hasChanges)
            {
                _userRepo.UpdateUser(_currentUser);
                return true;
            }

            return false;
        }

        private bool UpdateProfileInfo()
        {
            bool hasChanges = false;

            // Check if country changed
            if (_currentProfile.Country != txtCountry.Text.Trim())
            {
                _currentProfile.Country = string.IsNullOrWhiteSpace(txtCountry.Text) ? null : txtCountry.Text.Trim();
                hasChanges = true;
            }

            // Check if university changed
            if (_currentProfile.University != txtUniversity.Text.Trim())
            {
                _currentProfile.University = string.IsNullOrWhiteSpace(txtUniversity.Text) ? null : txtUniversity.Text.Trim();
                hasChanges = true;
            }

            // Check if visibility setting changed
            int newVisibility = chkHideProfile.IsChecked == true ? 1 : 0;
            if (_currentProfile.IsHidden != newVisibility)
            {
                _currentProfile.IsHidden = newVisibility;
                hasChanges = true;
            }

            // Save changes if any
            if (hasChanges)
            {
                _profileRepo.Upsert(_currentProfile.UserId, 
                    _currentProfile.Codeforces, 
                    _currentProfile.LeetCode, 
                    _currentProfile.Codechef, 
                    _currentProfile.Atcoder,
                    _currentProfile.Country,
                    _currentProfile.University,
                    _currentProfile.IsHidden);
                return true;
            }

            return false;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            // Reload original data
            LoadUserData();
        }

        // Event to notify parent when profile is updated
    public System.Action? OnProfileUpdated { get; set; }
    }
}
