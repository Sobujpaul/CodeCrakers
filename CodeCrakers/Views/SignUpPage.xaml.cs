using Microsoft.Data.Sqlite;
using System;
using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace CodeCrakers.Views
{
    public partial class SignUpPage : Window
    {
        private string dbPath = "Data Source=users.db"; // SQLite database file

        public SignUpPage()
        {
            InitializeComponent();
            InitializeDatabase(); // Create DB if not exists
        }

        // Create SQLite database and Users table if not exists
        private void InitializeDatabase()
        {
            using (SQLiteConnection con = new SQLiteConnection(dbPath))
            {
                con.Open();
                string query = @"CREATE TABLE IF NOT EXISTS Users(
                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                    Username TEXT UNIQUE,
                                    Email TEXT UNIQUE,
                                    Password TEXT
                                );";
                SQLiteCommand cmd = new SQLiteCommand(query, con);
                cmd.ExecuteNonQuery();
            }
        }

        // Hash password using SHA256
        private string HashPassword(string password)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

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

        // Sign Up button click
        private void btnSignUp_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("All fields are required!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string hashedPassword = HashPassword(password);

            try
            {
                using (SQLiteConnection con = new SQLiteConnection(dbPath))
                {
                    con.Open();
                    string query = "INSERT INTO Users (Username, Email, Password) VALUES (@username, @email, @password)";
                    SQLiteCommand cmd = new SQLiteCommand(query, con);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@password", hashedPassword);

                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Sign Up successful! You can now log in.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Navigate to Login Page
                LoginPage login = new LoginPage();
                login.Show();
                this.Close();
            }
            catch (SQLiteException ex)
            {
                if (ex.ResultCode == SQLiteErrorCode.Constraint)
                {
                    MessageBox.Show("Username or Email already exists!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Click "Login" TextBlock to go back to LoginPage
        private void LoginTextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Open LoginPage
            LoginPage login = new LoginPage();
            login.Show();

            // Close SignUpPage so only LoginPage is visible
            this.Close();
        }

    }
}
