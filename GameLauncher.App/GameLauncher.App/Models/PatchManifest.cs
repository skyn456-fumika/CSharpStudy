namespace GameLauncher.App.Models
{
    public class PatchManifest
    {
        public string Version { get; set; } = "0.0.0";

        public List<PatchFileInfo> Files { get; set; } = [];
    }
}