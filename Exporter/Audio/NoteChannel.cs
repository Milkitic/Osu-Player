using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Media.Audio.Player;
using Milky.OsuPlayer.Media.Audio.Player.Subchannels;
using Milky.OsuPlayer.Shared.Models.NostModels;

namespace Nostool.Audio
{
    public class NoteChannel : MultiElementsChannel
    {
        private readonly string _path;
        private readonly float _seRadio;
        private readonly float _bgmRadio;
        private readonly MusicScore _musicScore;
        private static HashSet<string> _generics = new HashSet<string>();
        private static SemaphoreSlim _ss = new SemaphoreSlim(1, 1);
        public NoteChannel(string path, float seRadio, float bgmRadio, MusicScore musicScore, AudioPlaybackEngine engine)
            : base(engine)
        {
            _path = path;
            _seRadio = seRadio;
            _bgmRadio = bgmRadio;
            _musicScore = musicScore;
        }

        public override async Task<IEnumerable<SoundElement>> GetSoundElements()
        {
            await _ss.WaitAsync();
            try
            {
                if (_generics.Count == 0)
                {
                    var di = new DirectoryInfo(Path.Combine(Domain.DefaultPath, "op"));
                    var allMatches = di.EnumerateFiles("*.wav").SelectMany(k => new string[]
                    {
                        k.Name.Split('_')[0],
                        string.Join("_", k.Name.Split('_').Take(2))
                    });
                    _generics = new HashSet<string>(allMatches, StringComparer.OrdinalIgnoreCase);
                }
            }
            finally
            {
                _ss.Release();
            }

            var dir = Path.GetDirectoryName(_path);
            var all = _musicScore.NoteData
                .SelectMany(k =>
                {
                    var balance = (k.MinKeyIndex + k.MaxKeyIndex) / 2f / 28 - 0.5f;
                    foreach (var musicScoreSubNote in k.SubNoteData) musicScoreSubNote.Balance = balance;
                    return k.SubNoteData;
                });
            var ele = all
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount > 1 ? Environment.ProcessorCount - 1 : 1)
                .Select(k =>
                {
                    var s = _musicScore.TrackInfo.First(o => o.Index == k.TrackIndex).Name;
                    var isGeneric = _generics.Contains(s);

                    var name = s + "_" +
                               KeysoundFilenameUtilities.GetFileSuffix(k.ScalePiano);
                    var path = isGeneric
                        ? Path.Combine(Domain.DefaultPath, "op", name)
                        : Path.Combine(dir, name);
                    float volume = (float)k.Velocity / sbyte.MaxValue;
                    if (_seRadio < 1)
                        volume *= _seRadio;
                    return SoundElement.Create(k.StartTimingMsec, volume, k.Balance, $"{path}.wav", k.EndTimingMsec);
                });
            var soundElements = new List<SoundElement>(ele);
            var backtrack = Path.Combine(dir, "_backtrack.wav");
            if (File.Exists(backtrack))
            {
                float volume = 1;
                if (_bgmRadio < 1)
                    volume *= _bgmRadio;
                soundElements.Insert(0, SoundElement.Create(0, volume, 0, backtrack));
            }

            return soundElements;
        }
    }
}