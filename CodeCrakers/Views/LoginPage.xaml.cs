using CodeCrakers.Data;
using CodeCrakers.Views; // ✅ WPF navigation
using CodeCrakers.Services;
using System.Windows;
using System.Windows.Input;

namespace CodeCrakers.Views
{
    public partial class LoginPage : Window
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Login button click
        private void btnlogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string usernameOrEmail = TextUser.Text.Trim();
                string password = txtPass.Password.Trim();

                if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show("⚠️ Please enter both username and password.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TextUser.Text = string.Empty;
                    txtPass.Password = string.Empty;
                    TextUser.Focus();
                    return;
                }

                var userRepo = new UserRepository();
                var userId = userRepo.ValidateLogin(usernameOrEmail, password);

                if (userId.HasValue)
                {
                    MessageBox.Show("✅ Login successful!", "Welcome", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Create and show main window
                    MainWindow mainWindow = new MainWindow(userId.Value);
                    mainWindow.Show();
                    
                    // Close login window
                    this.Close();
                }
                else
                {
                    MessageBox.Show("❌ Invalid username or password!", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    TextUser.Text = string.Empty;
                    txtPass.Password = string.Empty;
                    TextUser.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Login error: {ex.Message}\n\nPlease try again or contact support.", 
                    "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        // Toggle password visibility
        private void btnTogglePassword_Click(object sender, RoutedEventArgs e)
        {
            if (txtPass.Visibility == Visibility.Visible)
            {
                txtPassVisible.Text = txtPass.Password;
                txtPass.Visibility = Visibility.Collapsed;
                txtPassVisible.Visibility = Visibility.Visible;
                iconEye.Icon = FontAwesome.Sharp.IconChar.EyeSlash;
            }
            else
            {
                txtPass.Password = txtPassVisible.Text;
                txtPass.Visibility = Visibility.Visible;
                txtPassVisible.Visibility = Visibility.Collapsed;
                iconEye.Icon = FontAwesome.Sharp.IconChar.Eye;
            }
        }

        // Navigate to SignUpPage
        private void SignUpTextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SignUpPage signUp = new SignUpPage();
            signUp.Show();
            this.Close();
        }

        // Forgot password flow
        private void ForgotPassword_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // 1. Ask for email
                var email = Microsoft.VisualBasic.Interaction.InputBox("Enter your registered email to receive a verification code:", "Forgot Password", "");
                if (string.IsNullOrWhiteSpace(email)) return; // cancelled

                int? userId = null; string? username = null;
                using (var con = AppDb.GetConnection())
                {
                    con.Open();
                    using var cmd = con.CreateCommand();
                    cmd.CommandText = "SELECT Id, Username FROM Users WHERE Email=@e LIMIT 1;";
                    cmd.Parameters.AddWithValue("@e", email.Trim());
                    using var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        userId = reader.GetInt32(0);
                        username = reader.GetString(1);
                    }
                }
                if (!userId.HasValue)
                {
                    MessageBox.Show("No account found with that email.", "Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 2. Create one-time code (token) and send
                var resetRepo = new PasswordResetRepository();
                var token = resetRepo.CreateResetToken(userId.Value, TimeSpan.FromMinutes(10));
                var emailService = new EmailService();
                emailService.SendPasswordResetEmail(email.Trim(), token);
                MessageBox.Show("A verification code has been sent to your email (check debug log).", "Code Sent", MessageBoxButton.OK, MessageBoxImage.Information);

                // 3. Prompt user to enter code
                var enterCode = new EnterResetCodeWindow { Owner = this };
                if (enterCode.ShowDialog() != true) return; // user cancelled
                var entered = enterCode.EnteredCode?.Trim();
                if (string.IsNullOrWhiteSpace(entered)) return;

                var validatedUserId = resetRepo.ValidateToken(entered);
                if (!validatedUserId.HasValue || validatedUserId.Value != userId.Value)
                {
                    MessageBox.Show("Invalid or expired code.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 4. Open window to set new password (username + new password)
                var setPassWindow = new SetNewPasswordWindow(userId.Value, username) { Owner = this };
                if (setPassWindow.ShowDialog() == true)
                {
                    // Mark token used
                    resetRepo.MarkTokenUsed(entered!);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error starting password reset: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
