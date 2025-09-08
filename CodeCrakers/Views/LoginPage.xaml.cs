using CodeCrakers.Data;
using CodeCrakers.Views; // ✅ WPF navigation
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
    }
}
