using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;
using Milky.OsuPlayer.Instances;
using Milky.WpfApi;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            var list = (List<Collection>)_appDbOperator.GetCollections();
            list.Reverse();
            ViewModel.Collection = new ObservableCollection<Collection>(list);
        }

        private bool IsMapFavorite(MapInfo info)
        {
            var album = _appDbOperator.GetCollectionsByMap(info);
            bool isFavorite = album != null && album.Any(k => k.Locked);

            return isFavorite;
        }

        private bool IsMapFavorite(MapIdentity identity)
        {
            var info = _appDbOperator.GetMapFromDb(identity);
            return IsMapFavorite(info);
        }

        /// <summary>
        /// Call lyric provider to check lyric
        /// </summary>
        public void SetLyricSynchronously()
        {
            if (!LyricWindow.IsVisible)
                return;

            Task.Run(async () =>
            {
                if (_searchLyricTask?.IsTaskBusy() == true)
                    await Task.WhenAny(_searchLyricTask);

                _searchLyricTask = Task.Run(async () =>
                {
                    var player = Services.Get<PlayersInst>().AudioPlayer;
                    if (player == null)
                        return;

                    var meta = player.OsuFile.Metadata;
                    var lyricInst = Services.Get<LyricsInst>();
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
            MasterVolume.Value = AppSettings.Current.Volume.Main * 100;
            MusicVolume.Value = AppSettings.Current.Volume.Music * 100;
            HitsoundVolume.Value = AppSettings.Current.Volume.Hitsound * 100;
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
