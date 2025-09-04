using System.Windows;
using CodeCrakers.Data;

namespace CodeCrakers
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize SQLite database
            AppDb.Initialize();

        }
    }
}
