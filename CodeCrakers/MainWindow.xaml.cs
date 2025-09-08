using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using CodeCrakers.Views;
using CodeCrakers.Data; // For UserProfileRepository
using System.Runtime.InteropServices;
using System.Windows.Controls;

namespace CodeCrakers
{
    public partial class MainWindow : Window
    {
        private int _userId; // logged-in user ID
        private UserProfileRepository _profileRepo;
        private DashboardPage _dashboardPage;
        private PlatformPage _platformPage;

        public MainWindow(int userId) // receive userId from LoginPage
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("MainWindow: Starting initialization...");
                InitializeComponent();
                System.Diagnostics.Debug.WriteLine("MainWindow: InitializeComponent completed");
                
                this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;

                _userId = userId;
                _profileRepo = new UserProfileRepository();
                System.Diagnostics.Debug.WriteLine("MainWindow: User profile repository initialized");
                
                // Initialize pages
                System.Diagnostics.Debug.WriteLine("MainWindow: Creating DashboardPage...");
                _dashboardPage = new DashboardPage(_userId);
                System.Diagnostics.Debug.WriteLine("MainWindow: DashboardPage created");
                
                System.Diagnostics.Debug.WriteLine("MainWindow: Creating PlatformPage...");
                _platformPage = new PlatformPage(_userId);
                System.Diagnostics.Debug.WriteLine("MainWindow: PlatformPage created");
                
                // Set up event handlers
                _platformPage.OnSettingsSaved = RefreshDashboard;
                System.Diagnostics.Debug.WriteLine("MainWindow: Event handlers set up");
                
                // Show dashboard by default
                System.Diagnostics.Debug.WriteLine("MainWindow: Navigating to dashboard...");
                NavigateToDashboard();
                System.Diagnostics.Debug.WriteLine("MainWindow: Navigation completed");
                
                System.Diagnostics.Debug.WriteLine("MainWindow: Initialization completed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainWindow: Error during initialization: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"MainWindow: Stack Trace: {ex.StackTrace}");
                
                MessageBox.Show($"Error initializing main window: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}", 
                    "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
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

        // Navigation Methods
        private void NavigateToDashboard()
        {
            contentArea.Content = _dashboardPage;
            txtPageTitle.Text = "Dashboard";
        }

        private void NavigateToPlatform()
        {
            contentArea.Content = _platformPage;
            txtPageTitle.Text = "Platform Settings";
        }

        private void RefreshDashboard()
        {
            // Always refresh the dashboard data, regardless of current view
            _dashboardPage.RefreshData();
            
            // If dashboard is currently visible, it will show the updated data immediately
            // If not, the data will be fresh when user navigates back to dashboard
        }

        // Menu button click handlers (will be connected to XAML)
        public void OnDashboardClick(object sender, RoutedEventArgs e)
        {
            NavigateToDashboard();
        }

        public void OnPlatformClick(object sender, RoutedEventArgs e)
        {
            NavigateToPlatform();
        }

        public void OnNotificationClick(object sender, RoutedEventArgs e)
        {
            // TODO: Implement notifications page
            MessageBox.Show("Notifications feature coming soon!", "Feature Preview", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void OnSuggestionClick(object sender, RoutedEventArgs e)
        {
            // TODO: Implement suggestions page
            MessageBox.Show("Suggestions feature coming soon!", "Feature Preview", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

    }
}
