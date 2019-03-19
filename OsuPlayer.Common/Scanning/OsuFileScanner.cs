using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Milky.OsuPlayer.Common.Data;
using OSharp.Beatmap;

namespace Milky.OsuPlayer.Common.Scanning
{
    public class OsuFileScanner
    {
        private readonly string _path;

        public OsuFileScanner(string path)
        {
            _path = path;
        }

        public async Task NewScanAndAddAsync()
        {
            await BeatmapDbOperator.RemoveLocalAllAsync();
            var dirInfo = new DirectoryInfo(_path);

            foreach (var privateFolder in dirInfo.EnumerateDirectories(searchPattern: "*.*", searchOption: SearchOption.TopDirectoryOnly))
            {
                await ScanPrivateFolderAsync(privateFolder);
            }
        }

        private async Task ScanPrivateFolderAsync(DirectoryInfo privateFolder)
        {
            using (var dbOperator = new BeatmapDbOperator())
            {
                foreach (var fileInfo in privateFolder.EnumerateFiles(searchPattern: "*.osu", searchOption: SearchOption.TopDirectoryOnly))
                {
                    var osuFile = await OsuFile.ReadFromFileAsync(fileInfo.FullName);
                    await AddFileAsync(dbOperator, osuFile, fileInfo);
                }
            }
        }

        private async Task AddFileAsync(BeatmapDbOperator dbOperator, OsuFile osuFile, FileInfo fileInfo)
        {
            var beatmap = Data.EF.Model.Beatmap.ParseFromOSharp(osuFile);
            beatmap.BeatmapFileName = fileInfo.Name;
            beatmap.LastModifiedTime = fileInfo.LastWriteTime;
            beatmap.FolderName = fileInfo.Directory.Name;
            await dbOperator.AddNewMapAsync(beatmap);
        }
    }
}
