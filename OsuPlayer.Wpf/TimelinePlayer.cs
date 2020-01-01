using System;
using Milky.OsuPlayer.Common.Player;
using ReOsuStoryboardPlayer.Player;
using MusicPlayer = Milky.OsuPlayer.Media.Audio.Core.MusicPlayer;

namespace Milky.OsuPlayer
{
    public class TimelinePlayer : PlayerBase, IDisposable
    {
        private readonly MusicPlayer _player;

        public TimelinePlayer(MusicPlayer player)
        {
            _player = player;
        }

        public override float CurrentTime => _player.PlayTime;

        public override float Volume
        {
            get => _player.Volume;
            set => _player.Volume = value;
        }

        public override uint Length => (uint)_player.Duration;

        public override bool IsPlaying => _player.PlayerStatus == PlayerStatus.Playing;

        public override float PlaybackSpeed
        {
            get => _player.Playback;
            set => _player.Playback = value;
        }

        public void Dispose()
        {
            _player?.Dispose();
        }

        public override void Jump(float time, bool pause)
        {
            _player.SetTime((int)time, !pause);
        }

        public override void Pause()
        {
            _player.Pause();
        }

        public override void Play()
        {
            _player.Play();
        }

        public override void Stop()
        {
            _player.Stop();
        }
    }
}
