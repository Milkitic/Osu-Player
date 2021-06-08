using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Exporter.Xwb
{
    public class MultiExtractor
    {
        private readonly string _sourceDir;
        private string _destDir;

        public MultiExtractor(string sourceDir, string destDir, string subDirName = null)
        {
            _sourceDir = sourceDir;
            _destDir = subDirName != null ? Path.Combine(destDir, subDirName) : destDir;
        }

        public async Task ExtractAsync()
        {
            var directoryInfo = new DirectoryInfo(_sourceDir);
            var stdSource = directoryInfo.FullName + "\\";
            var tasks = IOUtil
                .EnumerateFiles(_sourceDir, ".xml", ".xwb")
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount - 1)
                .Select(async fi =>
                {
                    var dir = fi.Directory.FullName + "\\";
                    var relativePath = new Uri(stdSource).MakeRelativeUri(new Uri(fi.FullName));
                    var relative = new Uri(stdSource).MakeRelativeUri(new Uri(dir));
                    var targetDir = Path.Combine(_destDir, relative.ToString().Replace("/", "\\"));
                    if (!Directory.Exists(targetDir))
                        Directory.CreateDirectory(targetDir);

                    if (!fi.Extension.Equals(".xml", StringComparison.OrdinalIgnoreCase))
                    {
                        var ext = new SingleExtractor(fi.FullName, null, targetDir);

                        var s = $"Processing \"{relativePath}\"...";
                        Console.WriteLine(s);
                        try
                        {
                            await ext.ExtractAsync();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"  Failed to extract \"{relativePath}\": " +
                                              (ex.InnerException?.Message ?? ex.Message));
                        }
                    }
                    else
                    {
                        var fileName = Path.GetFileName(fi.FullName);
                        File.Copy(fi.FullName, Path.Combine(targetDir, fileName), true);
                    }
                });
            await Task.WhenAll(tasks);
        }
    }
}