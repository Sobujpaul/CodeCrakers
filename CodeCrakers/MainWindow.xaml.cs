using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CodeCrakers.Views; // Ensure LoginPage is inside this namespace
using System.Runtime.InteropServices;
using System.Windows.Interop;
namespace CodeCrakers
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        // --- Navigation ---
        private void SignIn_Click(object sender, RoutedEventArgs e)
        {
            Views.LoginPage login = new Views.LoginPage();
            login.ShowDialog(); // opens it as a separate window

        }
        private void pnlControlBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            WindowInteropHelper helper= new WindowInteropHelper(this);
            SendMessage(helper.Handle, 161,2,0);
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
            if(this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;
            }
            else
            {
                this.WindowState = WindowState.Normal;
            }
        }
    }
}
