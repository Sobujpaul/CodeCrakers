using System.Windows;

namespace CodeCrakers.Views
{
    public partial class EnterResetCodeWindow : Window
    {
        public string? EnteredCode { get; private set; }
        public EnterResetCodeWindow()
        {
            InitializeComponent();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCode.Text))
            {
                MessageBox.Show("Please enter the code.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            EnteredCode = txtCode.Text.Trim();
            DialogResult = true;
            Close();
        }
    }
}
