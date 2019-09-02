using Microsoft.Win32;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;
using Milky.OsuPlayer.Common.Instances;
using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.Control;
using Milky.OsuPlayer.Instances;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Pages;
using Milky.OsuPlayer.ViewModels;
using OSharp.Beatmap;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Milky.OsuPlayer.Control.Notification;
using Milky.OsuPlayer.Media.Audio.Music;
using Milky.WpfApi;
using Unosquare.FFME.Common;

namespace Milky.OsuPlayer.Windows
{
    partial class MainWindow
    {
        private PlayersInst _playerInst = Services.Get<PlayersInst>();
        private OsuDbInst _dbInst = Services.Get<OsuDbInst>();
        private PlayerList _playList = Services.Get<PlayerList>();

        //private bool EnableVideo => PlayerViewModel.Current.EnableVideo;
        private void Controller_OnNewFileLoaded(object sender, HandledEventArgs e)
        {
            Execute.OnUiThread(() =>
            {
                /* Set Lyric */
                SetLyricSynchronously();
            });
        }

        private void Controller_OnThumbClick(object sender, RoutedEventArgs e)
        {
            MainFrame.Content = null;
            //if (!PlayerViewModel.Current.EnableVideo)
            //    PlayerViewModel.Current.EnableVideo = true;
            //else if (PlayerViewModel.Current.EnableVideo)
            //{
            //    if (ResizableArea.Margin == new Thickness(5))
            //        SetFullScr();
            //    else
            //        SetFullScrMini();
            //}
        }

        private async void Controller_OnLikeClick(object sender, RoutedEventArgs e)
        {
            var entry = _beatmapDbOperator.GetBeatmapByIdentifiable(Services.Get<PlayerList>().CurrentIdentity);
            //var entry = App.PlayerList?.CurrentInfo.Beatmap;
            if (entry == null)
            {
                Notification.Show("该图不存在于该osu!db中", Title);
                return;
            }

            if (!ViewModel.IsMiniMode)
                FramePop.Navigate(new SelectCollectionPage(entry));
            else
            {
                var collection = _appDbOperator.GetCollections().First(k => k.Locked);
                if (Services.Get<PlayerList>().CurrentInfo.IsFavorite)
                {
                    _appDbOperator.RemoveMapFromCollection(entry, collection);
                    Services.Get<PlayerList>().CurrentInfo.IsFavorite = false;
                }
                else
                {
                    await SelectCollectionPage.AddToCollectionAsync(collection, new[] { entry });
                    Services.Get<PlayerList>().CurrentInfo.IsFavorite = true;
                }
            }

            IsMapFavorite(Services.Get<PlayerList>().CurrentInfo.Identity);
        }
    }

    internal class MyCancellationTokenSource : CancellationTokenSource
    {
        public Guid Guid { get; }

        public MyCancellationTokenSource()
        {
            Guid = Guid.NewGuid();
        }
    }
}
