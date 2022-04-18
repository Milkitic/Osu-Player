using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuPlayer.Shared;

public static class Constants
{
    public static string ApplicationDir { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Milki.OsuPlayer");
    public static string ConfigDir { get; } =
        Path.Combine(ApplicationDir, "configs");
    public static string DefaultHitsoundDir { get; } =
        Path.Combine(ApplicationDir, "default");
}