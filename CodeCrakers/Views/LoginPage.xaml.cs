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
           if(e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;    

        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void btnlogin_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
