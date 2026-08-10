namespace GameLauncher.App.Models
{
    public class PatchFileInfo
    {
        public string Path { get; set; } = string.Empty;

        public long Size { get; set; }

        public string Hash { get; set; } = string.Empty;
    }
}