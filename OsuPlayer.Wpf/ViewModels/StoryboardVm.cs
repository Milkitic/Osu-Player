using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Milky.OsuPlayer.Annotations;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Metadata;
using OSharp.Beatmap;
using OSharp.Beatmap.MetaData;

namespace Milky.OsuPlayer.ViewModels
{
    class StoryboardVm : INotifyPropertyChanged
    {
        private bool _isScanning;
        private ObservableCollection<BeatmapDataModel> _beatmapModels;
        private AppDbOperator _dbOperator = new AppDbOperator();

                public bool IsScanning
        {
            get => _isScanning;
            set
            {
                if (value == _isScanning) return;
                _isScanning = value;
                OnPropertyChanged();
            }
        }

        internal async Task ScanBeatmap()
        {
            await Task.Factory.StartNew(async () =>
            {
                var beatmaps = _dbOperator.GetAllBeatmaps();
                var folderGroup = beatmaps.GroupBy(k => (k.FolderName, k.InOwnFolder));
         
                foreach (var group in folderGroup)
                {
                    var inOwnFolder = group.Key.InOwnFolder;
                    var folderName = group.Key.FolderName;
                    var fullFolderPath = inOwnFolder
                        ? Path.Combine(Domain.CustomSongPath, folderName)
                        : Path.Combine(Domain.OsuSongPath, folderName);
                    var diffList = group.ToList();
                    var tasks = new Task[diffList.Count];
                    var maps = new ConcurrentBag<Beatmap>();
                    var delMaps = new ConcurrentBag<Beatmap>();
                    for (int i = 0; i < diffList.Count; i++)
                    {
                        var i1 = i;
                        tasks[i] = Task.Run(async () =>
                        {
                            try
                            {
                                var current = diffList[i1];
                                var mapPath = Path.Combine(fullFolderPath, current.BeatmapFileName);

                                if (i1 == 0)
                                {
                                    var osuFile = await OsuFile.ReadFromFileAsync($@"\\?\{mapPath}",
                                        options =>
                                        {
                                            options.IncludeSection("General", "Metadata", "TimingPoints", "Difficulty", "HitObjects", "Events");
                                            options.IgnoreSample();
                                            options.IgnoreStoryboard();
                                        });
                                    var analyzer = new OsuFileAnalyzer(osuFile);
                                    var osbName = analyzer.OsbFileName;
                                    var osbPath = Path.Combine(fullFolderPath, osbName);
                                    if (File.Exists(osbPath))
                                    {
                                        if (await OsuFile.OsbFileHasStoryboard(osbPath))
                                        {
                                            _dbOperator.SetMapSbFullInfo(new StoryboardFullInfo(folderName, current.InOwnFolder));
                                        }
                                    }
                                }

                                if (!File.Exists(mapPath)) return;

                                var hasStoryboard = await OsuFile.FileHasStoryboard($@"\\?\{mapPath}");
                                if (hasStoryboard)
                                {
                                    maps.Add(current);
                                }
                                else
                                {
                                    delMaps.Add(current);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                            }
                        });
                    }

                    Console.WriteLine("diff: " + diffList.Count);
                    await Task.WhenAll(tasks);

                    foreach (var beatmap in maps)
                    {
                        _dbOperator.SetMapSbInfo(beatmap,
                            new StoryboardInfo(beatmap.Version, beatmap.FolderName));
                    }

                    foreach (var delMap in delMaps)
                    {
                        _dbOperator.RemoveMapSbInfo(delMap);
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }

        public ObservableCollection<BeatmapDataModel> BeatmapModels
        {
            get => _beatmapModels;
            set
            {
                if (Equals(value, _beatmapModels)) return;
                _beatmapModels = value;
                OnPropertyChanged();
            }
        }

        public static StoryboardVm Default
        {
            get
            {
                lock (_defaultLock)
                {
                    return _default ?? (_default = new StoryboardVm());
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static StoryboardVm _default;
        private static object _defaultLock = new object();
        private StoryboardVm()
        {
        }
    }
}
