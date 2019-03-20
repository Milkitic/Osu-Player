using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;
using Milky.OsuPlayer.Instances;
using Milky.WpfApi;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Milky.OsuPlayer.Windows
{
    partial class MainWindow
    {
        private Task _searchLyricTask;

        /// <summary>
        /// Update collections in the navigation bar.
        /// </summary>
        public void UpdateCollections()
        {
            var list = (List<Collection>)DbOperate.GetCollections();
            list.Reverse();
            ViewModel.Collection = list;
        }

        private bool IsMapFavourite(MapInfo info)
        {
            var album = DbOperate.GetCollectionsByMap(info);
            bool isFavourite = album != null && album.Any(k => k.Locked);

            return isFavourite;
        }

        private bool IsMapFavourite(MapIdentity identity)
        {
            var info = DbOperate.GetMapFromDb(identity);
            return IsMapFavourite(info);
        }

        /// <summary>
        /// Call lyric provider to check lyric
        /// todo: this should run synchronously.
        /// </summary>
        public void SetLyricSynchronously()
        {
            if (!LyricWindow.IsVisible) return;

            Task.Run(async () =>
            {
                if (_searchLyricTask?.IsTaskBusy() == true)
                    await Task.WhenAny(_searchLyricTask);

                _searchLyricTask = Task.Run(async () =>
                {
                    var player = InstanceManage.GetInstance<PlayersInst>().AudioPlayer;
                    if (player == null) return;
                    var meta = player.OsuFile.Metadata;
                    var lyricInst = InstanceManage.GetInstance<LyricsInst>();
                    var lyric = await lyricInst.LyricProvider.GetLyricAsync(meta.ArtistMeta.ToUnicodeString(),
                        meta.TitleMeta.ToUnicodeString(), player.Duration);
                    LyricWindow.SetNewLyric(lyric, player.OsuFile);
                    LyricWindow.StartWork();
                });
            });
        }

        /// <summary>
        /// Initialize default player settings.
        /// </summary>
        private void LoadSurfaceSettings()
        {
            MasterVolume.Value = PlayerConfig.Current.Volume.Main * 100;
            MusicVolume.Value = PlayerConfig.Current.Volume.Music * 100;
            HitsoundVolume.Value = PlayerConfig.Current.Volume.Hitsound * 100;
        }

        private void ToMiniMode()
        {
            FuncPanel.Orientation = Orientation.Vertical;
            // modules
            PlayProgress.Visibility = Visibility.Collapsed;
        }

        private void ToNormalMode()
        {
            FuncPanel.Orientation = Orientation.Horizontal;
            // modules
            PlayProgress.Visibility = Visibility.Visible;
        }
    }
}
