using System.Diagnostics;
using System.IO;

namespace GameLauncher.App.Services
{
    public class GameProcessService
    {
        public bool StartGame(
            string executablePath,
            string? arguments = null)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
                return false;

            if (!File.Exists(executablePath))
                return false;

            var workingDirectory =
                Path.GetDirectoryName(executablePath);

            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = arguments ?? string.Empty,
                WorkingDirectory = workingDirectory ?? string.Empty,
                UseShellExecute = true
            };

            Process.Start(startInfo);

            return true;
        }
    }
}