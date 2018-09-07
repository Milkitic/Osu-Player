using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Milkitic.OsuPlayer
{
    internal static class Domain
    {
        public static string CurrentPath => AppDomain.CurrentDomain.BaseDirectory;
        public static string DefaultPath => Path.Combine(CurrentPath, "default");
    }
}
