using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using CodeCrakers.Views; // If navigating to another page

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

        }
    }
}
