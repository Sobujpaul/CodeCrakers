using System;
using System.Windows;
using CodeCrakers.Models;

namespace CodeCrakers.Views
{
    public partial class ExternalUserEditWindow : Window
    {
        public ExternalUser ExternalUser { get; private set; }

        public ExternalUserEditWindow(ExternalUser user)
        {
            InitializeComponent();
            ExternalUser = new ExternalUser
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Codeforces = user.Codeforces,
                LeetCode = user.LeetCode,
                Codechef = user.Codechef,
                Atcoder = user.Atcoder,
                Country = user.Country,
                University = user.University,
                AddedAt = user.AddedAt,
                AddedBy = user.AddedBy,
                MaxRating = user.MaxRating,
                TotalSolved = user.TotalSolved
            };

            // bind to fields
            txtDisplayName.Text = ExternalUser.DisplayName;
            txtCodeforces.Text = ExternalUser.Codeforces;
            txtLeetCode.Text = ExternalUser.LeetCode;
            txtCodeChef.Text = ExternalUser.Codechef;
            txtAtcoder.Text = ExternalUser.Atcoder;
            txtCountry.Text = ExternalUser.Country;
            txtUniversity.Text = ExternalUser.University;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            ExternalUser.DisplayName = (txtDisplayName.Text ?? string.Empty).Trim();
            ExternalUser.Codeforces = string.IsNullOrWhiteSpace(txtCodeforces.Text) ? null : txtCodeforces.Text.Trim();
            ExternalUser.LeetCode = string.IsNullOrWhiteSpace(txtLeetCode.Text) ? null : txtLeetCode.Text.Trim();
            ExternalUser.Codechef = string.IsNullOrWhiteSpace(txtCodeChef.Text) ? null : txtCodeChef.Text.Trim();
            ExternalUser.Atcoder = string.IsNullOrWhiteSpace(txtAtcoder.Text) ? null : txtAtcoder.Text.Trim();
            ExternalUser.Country = string.IsNullOrWhiteSpace(txtCountry.Text) ? null : txtCountry.Text.Trim();
            ExternalUser.University = string.IsNullOrWhiteSpace(txtUniversity.Text) ? null : txtUniversity.Text.Trim();

            this.DialogResult = true;
        }
    }
}


