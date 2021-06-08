using System;
using System.IO;

namespace Nostool
{
    static class Dierectoies
    {
        static Dierectoies()
        {
            Type t = typeof(Dierectoies);
            var infos = t.GetProperties();
            foreach (var item in infos)
            {
                try
                {
                    string path = (string)item.GetValue(null, null);
                    if (path == null) continue;
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error while creating folder \"{0}\": {1}", item.Name, ex.Message);
                }
            }
        }

        public static string AppDataFolder { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NostExporter");

        public static string TempFolder { get; } = Path.Combine(AppDataFolder, "temp");
    }
}
