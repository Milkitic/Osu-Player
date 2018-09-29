namespace Milkitic.OsuPlayer.Models
{
    public struct MapIdentity
    {
        public MapIdentity(string folderName, string version) : this()
        {
            FolderName = folderName;
            Version = version;
        }

        public string FolderName { get; set; }
        public string Version { get; set; }
    }
}
