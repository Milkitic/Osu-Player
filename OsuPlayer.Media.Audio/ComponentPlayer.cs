using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.Media.Audio.Music;
using OSharp.Beatmap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Milky.OsuPlayer.Media.Audio
{
    public class ComponentPlayer : IPlayer, IDisposable
    {
        public OsuFile OsuFile { get; }
        private string _filePath;
        internal HitsoundPlayer HitsoundPlayer { get; private set; }
        internal MusicPlayer MusicPlayer { get; private set; }
        public PlayerStatus PlayerStatus => HitsoundPlayer?.PlayerStatus ?? PlayerStatus.Stopped;
        public int Duration => HitsoundPlayer?.Duration ?? 0;
        public int PlayTime => HitsoundPlayer?.PlayTime ?? 0;
        public int HitsoundOffset
        {
            get => HitsoundPlayer.SingleOffset;
            set => HitsoundPlayer.SingleOffset = value;
        }

        public ComponentPlayer(string filePath, OsuFile osuFile)
        {
            Current?.Dispose();
            Current = this;
            _filePath = filePath;
            OsuFile = osuFile;
            HitsoundPlayer = new HitsoundPlayer(filePath, osuFile);

            FileInfo fileInfo = new FileInfo(filePath);
            DirectoryInfo dirInfo = fileInfo.Directory;
            FileInfo musicInfo = new FileInfo(Path.Combine(dirInfo.FullName, OsuFile.General.AudioFilename));
            MusicPlayer = new MusicPlayer(musicInfo.FullName);

            HitsoundPlayer.SetDuration(MusicPlayer.Duration);
            HitsoundPlayer.OnFinished += HitsoundPlayer_OnFinished;
        }

        private void HitsoundPlayer_OnFinished()
        {
            MusicPlayer.Stop();
        }

        public static ComponentPlayer Current { get; set; }

        public void Play()
        {
            if (HitsoundPlayer.IsPlaying)
                return;
            MusicPlayer.Play();
            HitsoundPlayer.Play();
        }

        public void Pause()
        {
            MusicPlayer.Pause();
            HitsoundPlayer.Pause();
        }

        public void Stop()
        {
            HitsoundPlayer.Stop();
            MusicPlayer.Stop();
        }

        public void Replay()
        {
            Stop();
            Play();
        }

        public void SetTime(int ms, bool play = true)
        {
            HitsoundPlayer.SetTime(ms, play);
            if (play)
            {
                MusicPlayer.SetTime(ms);
                HitsoundPlayer.Play();
            }
            else
                MusicPlayer.SetTime(ms, false);
        }

        public void SetPlayMod(PlayMod mod, bool play)
        {
            MusicPlayer.SetPlayMod(mod);
            HitsoundPlayer.SetPlayMod(mod, play);
        }

        public void Dispose()
        {
            HitsoundPlayer?.Dispose();
            MusicPlayer?.Dispose();
            Current = null;
        }

        public static void DisposeAll()
        {
            WavePlayer.Device?.Dispose();
            WavePlayer.MasteringVoice?.Dispose();
        }
    }
}
