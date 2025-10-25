using System;
using System.IO;

namespace CodeCrakers.Utils
{
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static readonly string LogDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodeCrakers");
        private static readonly string LogFilePath = Path.Combine(LogDirectory, "app.log");

        public static void Log(string message)
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                    Directory.CreateDirectory(LogDirectory);

                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}";
                lock (_lock)
                {
                    File.AppendAllText(LogFilePath, line + Environment.NewLine);
                }
            }
            catch
            {
                // Swallow logging errors to avoid impacting the app
            }
        }
    }
}
