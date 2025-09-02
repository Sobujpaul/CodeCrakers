using CodeCrakers.Views; // If navigating to another page // ✅ WPF version
using System.Windows;
using System.Windows.Controls;
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

        }
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnlogin_Click(object sender, RoutedEventArgs e)
        {
            // Example: simple validation
            if (TextUser.Text == "admin" && txtPass.Password == "1234")
            {
                // Create and show MainWindow
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();

                // Close the login window
                this.Close();
            }
            else
            {
                MessageBox.Show("Invalid username or password!", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }



        private void btnTogglePassword_Click(object sender, RoutedEventArgs e)
        {
            if (txtPass.Visibility == Visibility.Visible)
            {
                // Show password as text
                txtPassVisible.Text = txtPass.Password;
                txtPass.Visibility = Visibility.Collapsed;
                txtPassVisible.Visibility = Visibility.Visible;
                iconEye.Icon = FontAwesome.Sharp.IconChar.EyeSlash;
                // Use string, NOT IconChar
            }
            else
            {
                // Hide password
                txtPass.Password = txtPassVisible.Text;
                txtPass.Visibility = Visibility.Visible;
                txtPassVisible.Visibility = Visibility.Collapsed;
                iconEye.Icon = FontAwesome.Sharp.IconChar.Eye; // To show the "eye" icon
            }
        }
        private void SignUpTextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Open SignUpPage
            SignUpPage signUp = new SignUpPage();
            signUp.Show();

            // Close LoginPage
            this.Close();
        }


    }
}
