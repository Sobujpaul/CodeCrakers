using System.Windows;
using CodeCrakers.Data;
using CodeCrakers.Utils;

namespace CodeCrakers
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize SQLite database
            AppDb.Initialize();

            // Optional: log DB path and perform a quick health check
            Logger.Log($"[App] DB Path: {AppDb.GetDatabasePath()}");
            AppDb.HealthCheck();

        }
    }
}
