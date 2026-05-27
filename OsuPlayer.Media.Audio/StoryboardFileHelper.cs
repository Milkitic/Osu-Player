using System;
using System.IO;
using Coosu.Beatmap;

namespace Milky.OsuPlayer.Media.Audio
{
    internal static class StoryboardFileHelper
    {
        public static bool HasOsbStoryboard(OsuFile osuFile, string mapPath)
        {
            if (osuFile == null || string.IsNullOrWhiteSpace(mapPath) || !File.Exists(mapPath))
            {
                return false;
            }

            var baseDirectory = Path.GetDirectoryName(mapPath);
            if (string.IsNullOrWhiteSpace(baseDirectory))
            {
                return false;
            }

            var osbPath = Path.Combine(baseDirectory, GetOsbFilename(osuFile));
            if (!File.Exists(osbPath))
            {
                return false;
            }

            using var sr = new StreamReader(osbPath);
            var text = sr.ReadLine();
            var inSbSection = false;
            var hasInSbSection = false;
            while (!sr.EndOfStream && text != null)
            {
                if (text.StartsWith("//", StringComparison.Ordinal))
                {
                    if (text.StartsWith("//Storyboard Layer", StringComparison.Ordinal))
                    {
                        inSbSection = true;
                        hasInSbSection = true;
                    }
                    else if (hasInSbSection)
                    {
                        break;
                    }
                }
                else if (inSbSection && !string.IsNullOrWhiteSpace(text))
                {
                    return true;
                }

                text = sr.ReadLine();
            }

            return false;
        }

        private static string GetOsbFilename(OsuFile osuFile)
        {
            var fileName = $"{osuFile.Metadata.Artist} - {osuFile.Metadata.Title} ({osuFile.Metadata.Creator}).osb";
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalidChar, '_');
            }

            return fileName;
        }
    }
}