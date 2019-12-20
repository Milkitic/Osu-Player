using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Milky.OsuPlayer.Common.Metadata;
using OSharp.Beatmap.MetaData;

namespace Milky.OsuPlayer.ViewModels
{
    class StoryboardVm
    {
        public ObservableCollection<BeatmapDataModel> BeatmapModels { get; set; }

        private static StoryboardVm _default;
        private static object _defaultLock = new object();

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

        private StoryboardVm()
        {
        }
    }
}
