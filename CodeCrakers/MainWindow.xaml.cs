using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using CodeCrakers.Views;
using CodeCrakers.Data; // For UserProfileRepository
using System.Runtime.InteropServices;

namespace CodeCrakers
{
    public partial class MainWindow : Window
    {
        private int _userId; // logged-in user ID
        private UserProfileRepository _profileRepo;

        public MainWindow(int userId) // receive userId from LoginPage
        {
            InitializeComponent();
            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;

            _userId = userId;
            _profileRepo = new UserProfileRepository();
        }

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        // --- Navigation ---
        private void SignIn_Click(object sender, RoutedEventArgs e)
        {
            LoginPage login = new LoginPage();
            login.ShowDialog();
        }

        private void pnlControlBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            WindowInteropHelper helper = new WindowInteropHelper(this);
            SendMessage(helper.Handle, 161, 2, 0);
        }

        private void pnlControlBar_MouseEnter(object sender, MouseEventArgs e)
        {
            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
                this.WindowState = WindowState.Maximized;
            else
                this.WindowState = WindowState.Normal;
        }

    }
}
