using Milkitic.OsuPlayer.Media.Lyric;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Milkitic.OsuPlayer
{
    public class Config
    {
        public VolumeControl Volume { get; set; } = new VolumeControl();
        public GeneralControl General { get; set; } = new GeneralControl();
        public PlayControl Play { get; set; } = new PlayControl();
        public List<HotKey> HotKeys { get; set; } = new List<HotKey>();
        public LyricControl Lyric { get; set; } = new LyricControl();
        public ExportControl Export { get; set; } = new ExportControl();
        public List<MapIdentity> CurrentList { get; set; } = new List<MapIdentity>();
        public string CurrentPath { get; set; }
        public DateTime? LastUpdateCheck { get; set; } = null;
    }

    public class VolumeControl
    {
        private float _main = 0.8f;
        private float _bgm = 1;
        private float _hs = 0.9f;

        public float Main { get => _main; set => SetValue(ref _main, value); }
        public float Music { get => _bgm; set => SetValue(ref _bgm, value); }
        public float Hitsound { get => _hs; set => SetValue(ref _hs, value); }

        private static void SetValue(ref float source, float value) => source = value < 0 ? 0 : (value > 1 ? 1 : value);
    }

    public class GeneralControl
    {
        public bool RunOnStartup { get; set; } = false;
        public string DbPath { get; set; }
        public bool? ExitWhenClosed { get; set; } = null;
    }

    public class PlayControl
    {
        public int GeneralOffset { get; set; } = 25;
        public bool ReplacePlayList { get; set; } = true;
        public bool UsePlayerV2 { get; set; } = false;
        public PlayMod PlayMod { get; set; } = PlayMod.None;
        public bool AutoPlay { get; set; } = false;
        public bool Memory { get; set; } = true;
        public int DesiredLatency { get; set; } = 5;
    }

    public class HotKey
    {
        public string Name { get; set; }
        public bool Enabled { get; set; }
        public Keys Key { get; set; }
        public bool UseControlKey { get; set; }
        public bool UseAltKey { get; set; }
        public bool UseShiftKey { get; set; }
        [JsonIgnore]
        public Action Callback { get; set; }
    }

    public class LyricControl
    {
        public bool EnableLyric { get; set; } = true;
        public LyricSource LyricSource { get; set; } = LyricSource.Auto;
        public LyricProvider.ProvideTypeEnum ProvideType { get; set; } = LyricProvider.ProvideTypeEnum.Original;
        public bool StrictMode { get; set; } = true;
        public bool EnableCache { get; set; } = true;
    }

    public enum LyricSource
    {
        Auto, Netease, Kugou, QqMusic
    }

    public class ExportControl
    {
        public string MusicPath { get; set; } = Domain.MusicPath;
        public string BgPath { get; set; } = Domain.BackgroundPath;
        public NamingStyle NamingStyle { get; set; } = NamingStyle.ArtistTitle;
        public SortStyle SortStyle { get; set; } = SortStyle.Artist;

    }
    public enum NamingStyle
    {
        Title, ArtistTitle, TitleArtist
    }

    public enum SortStyle
    {
        None, Artist, Mapper, Source
    }
}
