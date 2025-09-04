using CodeCrakers.Data;
using System.Windows;
using System.Windows.Input;

namespace CodeCrakers.Views
{
    public partial class SignUpPage : Window
    {
        public SignUpPage()
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

        private void btnSignUp_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Password.Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("⚠️ All fields are required!", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtUsername.Text = string.Empty;
                txtEmail.Text = string.Empty;
                txtPassword.Password = string.Empty;
                txtPasswordVisible.Text = string.Empty;

                // Set focus back to username
                txtUsername.Focus();
                return;
            }

            var userRepo = new UserRepository();

            if (userRepo.UsernameOrEmailExists(username, email))
            {
                MessageBox.Show("⚠️ Username or Email already exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtUsername.Text = string.Empty;
                txtEmail.Text = string.Empty;
                txtPassword.Password = string.Empty;
                txtPasswordVisible.Text = string.Empty;

                // Set focus back to username
                txtUsername.Focus();
                return;
            }

            int newUserId = userRepo.CreateUser(username, email, password);
            MessageBox.Show("✅ Account created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

            // Navigate to LoginPage
            LoginPage login = new LoginPage();
            login.Show();
            this.Close();
        }

        // Toggle password visibility
        private void btnTogglePassword_Click(object sender, RoutedEventArgs e)
        {
            if (txtPassword.Visibility == Visibility.Visible)
            {
                txtPasswordVisible.Text = txtPassword.Password;
                txtPassword.Visibility = Visibility.Collapsed;
                txtPasswordVisible.Visibility = Visibility.Visible;
                iconEye.Icon = FontAwesome.Sharp.IconChar.EyeSlash;
            }
            else
            {
                txtPassword.Password = txtPasswordVisible.Text;
                txtPassword.Visibility = Visibility.Visible;
                txtPasswordVisible.Visibility = Visibility.Collapsed;
                iconEye.Icon = FontAwesome.Sharp.IconChar.Eye;
            }
        }

        private void LoginTextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LoginPage login = new LoginPage();
            login.Show();
            this.Close();
        }
    }
}
