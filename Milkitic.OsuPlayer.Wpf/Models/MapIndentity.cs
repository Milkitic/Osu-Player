using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Milkitic.OsuPlayer.Wpf.Models
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
