using SharpDX.Multimedia;
using SharpDX.XAudio2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Milkitic.OsuPlayer.Utils
{
    internal class WavePlayer : IDisposable
    {
        public static readonly XAudio2 Device = new XAudio2();
        public static readonly MasteringVoice MasteringVoice = new MasteringVoice(Device);
        private static List<WavePlayer> _cachedPlayers = new List<WavePlayer>();
        private static readonly object LockObj = new object();
        public static void PlayFile(string path, float volume)
        {
            WavePlayer cachedPlayer;
            lock (LockObj)
                cachedPlayer = _cachedPlayers.FirstOrDefault(p => p.DicPath == path && p.IsFinished) ??
                               SaveToCache(path);

            cachedPlayer.Play(volume);
        }

        public static WavePlayer SaveToCache(string path)
        {
            try
            {
                var player = new WavePlayer(path);
                lock (LockObj)
                    _cachedPlayers.Add(player);

                return player;
            }
            catch (SharpDX.SharpDXException e)
            {
                Console.WriteLine(e);
                var player = new WavePlayer(Path.Combine(Domain.DefaultPath, "default.wav")) { DicPath = path };
                lock (LockObj)
                    _cachedPlayers.Add(player);
                return player;
            }
            catch (InvalidOperationException e)
            {
                if (e.Message.ToLower().Contains("unsupport"))
                    throw new NotSupportedException("有不支持的音效。");
                // todo
                var player = new WavePlayer(Path.Combine(Domain.DefaultPath, "default.wav")) { DicPath = path };
                lock (LockObj)
                    _cachedPlayers.Add(player);
                return player;
            }
        }

        public static void ClearCache()
        {
            lock (LockObj)
            {
                foreach (var item in _cachedPlayers)
                    item.Dispose();
                _cachedPlayers = new List<WavePlayer>();
            }
        }

        public bool IsFinished { get; set; }
        public string DicPath { get; set; }
        public string WavePath { get; set; }
        private readonly SourceVoice _sourceVoice;
        private readonly AudioBuffer _buffer;
        private readonly SoundStream _stream;

        public WavePlayer(string path)
        {
            WavePath = path;
            DicPath = path;
            FileInfo fi = new FileInfo(path);
            if (!fi.Exists)
                WavePath = Path.Combine(Domain.DefaultPath, "default.wav");
            _stream = new SoundStream(File.OpenRead(WavePath));
            WaveFormat waveFormat = _stream.Format;
            _buffer = new AudioBuffer
            {
                Stream = _stream.ToDataStream(),
                AudioBytes = (int)_stream.Length,
                Flags = BufferFlags.EndOfStream
            };
            _stream.Close();
            _sourceVoice = new SourceVoice(Device, waveFormat, true);
            _sourceVoice.BufferEnd += OnSourceVoiceOnBufferEnd;
        }

        private void OnSourceVoiceOnBufferEnd(IntPtr context) => IsFinished = true;

        private void Play(float volume)
        {
            IsFinished = false;
            try
            {
                _sourceVoice.SubmitSourceBuffer(_buffer, _stream.DecodedPacketsInfo);
                _sourceVoice.SetVolume(volume * 0.95f);
                _sourceVoice.Start();
            }
            catch (Exception)
            {
                IsFinished = true;
            }
        }

        public void Dispose()
        {
            _sourceVoice?.DestroyVoice();
            _sourceVoice?.Dispose();
            _buffer.Stream.Dispose();
        }
    }
}
