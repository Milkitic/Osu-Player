using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Nostool.Xwb
{
    public class SingleExtractor
    {
        private static readonly Regex _xwbCountRegex = new Regex(@"XWB has (\d+) streams");
        private static readonly Regex _xsbCountRegex = new Regex(@"XSB wavebank (\d+) has (\d+) sounds");
        private static readonly Regex _xwbRegex = new Regex(@"Stream (\d+): (.+)_(\d+)__(.+)\.xwb");

        private readonly string _xwbPath;
        private string _xsbPath;
        private readonly string _targetDir;

        public SingleExtractor(string xwbPath, string xsbPath = null, string targetDir = null)
        {
            _xwbPath = xwbPath;
            _xsbPath = xsbPath;
            var name = Path.GetFileNameWithoutExtension(xwbPath);
            var folder = Path.GetDirectoryName(xwbPath);
            _xsbPath ??= Path.Combine(Path.GetDirectoryName(xwbPath), name + ".xsb");
            _targetDir = targetDir ?? Path.Combine(folder, name);
        }

        public async Task ExtractAsync()
        {
            var tmpFolder = Dierectoies.TempFolder;

            var mapping = await ReadIndexMappingAsync();
            var tmpXsb = Path.Combine(tmpFolder, Path.GetRandomFileName() + ".xsb");

            using (var sw = File.CreateText(tmpXsb))
            {
                foreach (var mappingValue in mapping.Values)
                {
                    await sw.WriteAsync(mappingValue);
                    await sw.WriteAsync('\0');
                }

                await sw.FlushAsync();
            }

            await InnerExtractAsync(tmpXsb);
        }

        private async Task InnerExtractAsync(string xsbPath)
        {
            if (!Directory.Exists(_targetDir))
                Directory.CreateDirectory(_targetDir);
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo("unxwb.exe")
                {
                    ArgumentList = { "-d", _targetDir, "-b", xsbPath, "0", _xwbPath },
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                }
            };

            proc.Start();
            await proc.StandardInput.WriteLineAsync("all\n");
            await proc.WaitForExitAsync();
            if (proc.ExitCode != 0) throw new Exception("Failed: " + proc.ExitCode);
        }

        private async Task<Dictionary<int, string>> ReadIndexMappingAsync()
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo("xwb_split.exe")
                {
                    ArgumentList = { "-l", "-n", "-x", _xsbPath, _xwbPath },
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            var dic = new Dictionary<int, string>();

            int? streamCount = null;
            int? descCount = null;
            bool startWriting = false;
            string error = "unknown error";
            proc.ErrorDataReceived += (s, e) =>
            {
                //Console.WriteLine(e.Data);
                if (string.IsNullOrWhiteSpace(e.Data)) return;
                error = e.Data.Split(": ", StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
                proc.Kill();
            };
            proc.OutputDataReceived += (s, e) =>
            {
                //Console.WriteLine(e.Data);
                if (e.Data == null) return;
                Match match;
                if (streamCount == null)
                {
                    match = _xwbCountRegex.Match(e.Data);
                    if (match.Success)
                    {
                        streamCount = int.Parse(match.Groups[1].Value);
                        return;
                    }
                }

                if (descCount == null)
                {
                    match = _xsbCountRegex.Match(e.Data);
                    if (match.Success)
                    {
                        descCount = int.Parse(match.Groups[2].Value);

                        if (descCount != streamCount)
                        {
                            //error = "XSB count does not equal XWB count.";
                            //proc.Kill();
                        }

                        return;
                    }
                }

                if (!startWriting && e.Data == "Writting streams...")
                {
                    startWriting = true;
                    return;
                }

                if (startWriting)
                {
                    try
                    {
                        match = _xwbRegex.Match(e.Data);
                        if (match.Success)
                        {
                            var index = int.Parse(match.Groups[1].Value);
                            var name = (match.Groups[4].Value);
                            dic.Add(index, name);
                        }
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception.Message);
                    }
                }

                if (e.Data?.StartsWith("ERROR: ", StringComparison.OrdinalIgnoreCase) == true)
                {
                    error = string.Join(": ", e.Data?.Split(": ", StringSplitOptions.None).Skip(1));
                    proc.Kill();
                }
            };

            new Task(() =>
            {
                try
                {
                    for (int i = 1; i <= 6; i++)
                    {
                        Thread.Sleep(5000);
                        if (!proc.HasExited)
                        {
                            error = $"XWB_SPLIT kept no response for {i * 1 * 5} seconds";
                            Console.WriteLine($"Warn: {error} for \"{Path.GetFileName(_xwbPath)}\"");
                            if (i == 6) proc.Kill();
                        }
                        else
                        {
                            return;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }).Start();

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit();
            if (proc.ExitCode != 0) throw new Exception($"Failed with error code {proc.ExitCode}: {error}");
            return dic;
        }
    }

}
