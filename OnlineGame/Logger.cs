namespace OnlineGame
{
    internal class Logger
    {
        private readonly string logPath = "Logs/server.log";
        private readonly object lockObject = new object();

        public Logger()
        {
            string? directory = Path.GetDirectoryName(logPath);

            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public void Log(string message)
        {
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";

            lock (lockObject)
            {
                File.AppendAllText(logPath, line + Environment.NewLine);
            }
        }

        public void LogError(string message, Exception ex)
        {
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {message} | {ex.GetType().Name}: {ex.Message}";

            lock (lockObject)
            {
                File.AppendAllText(logPath, line + Environment.NewLine);
            }
        }
    }
}