using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;

namespace CodeCrakers.Views
{
    public partial class ComparePage : UserControl
    {
        public event System.Action<string, string>? OnCompareRequested;
        public ComparePage()
        {
            InitializeComponent();
        }

        // Placeholder refresh button handler referenced in XAML
        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("ComparePage: btnRefresh_Click invoked - clearing inputs.");
            if (txtHandle1 != null) txtHandle1.Text = string.Empty;
            if (txtHandle2 != null) txtHandle2.Text = string.Empty;
        }

        private void btnCompare_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("ComparePage: btnCompare_Click invoked.");
            var h1 = (txtHandle1?.Foreground == System.Windows.Media.Brushes.Gray) ? string.Empty : txtHandle1?.Text?.Trim();
            var h2 = (txtHandle2?.Foreground == System.Windows.Media.Brushes.Gray) ? string.Empty : txtHandle2?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(h1) || string.IsNullOrWhiteSpace(h2))
            {
                MessageBox.Show("Please enter both Codeforces handles before comparing.", "Compare", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            OnCompareRequested?.Invoke(h1!, h2!);
        }

        // Shared placeholder logic for both handle TextBoxes
        private void Handle_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                if (tb.Foreground == System.Windows.Media.Brushes.Gray)
                {
                    tb.Text = string.Empty;
                    tb.Foreground = (System.Windows.Media.Brush)FindResource("titleColor1");
                }
            }
        }

        private void Handle_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                if (string.IsNullOrWhiteSpace(tb.Text))
                {
                    // Restore placeholder based on which box
                    if (tb == txtHandle1) tb.Text = "Handle 1"; else if (tb == txtHandle2) tb.Text = "Handle 2";
                    tb.Foreground = System.Windows.Media.Brushes.Gray;
                }
            }
        }
    }
}
