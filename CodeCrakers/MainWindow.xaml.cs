using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CodeCrakers.Views; // Ensure LoginPage is inside this namespace

namespace CodeCrakers
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // --- Navigation ---
        private void SignIn_Click(object sender, RoutedEventArgs e)
        {
            Views.LoginPage login = new Views.LoginPage();
            login.ShowDialog(); // opens it as a separate window
       
        }

        // --- Window Controls ---
        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                ChangeMaxRestoreIcon("Images/Maximize1.png");
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                ChangeMaxRestoreIcon("Images/Restore.png");
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // --- Drag Window & Double-Click Title Bar ---
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 2)
                {
                    Maximize_Click(sender, e);
                }
                else
                {
                    this.DragMove();
                }
            }
        }

        // --- Helper: Update Max/Restore button icon ---
        private void ChangeMaxRestoreIcon(string imagePath)
        {
            if (MaxRestoreImage != null)
            {
                MaxRestoreImage.Source = new BitmapImage(new System.Uri(imagePath, System.UriKind.Relative));
            }
        }
    }
}
