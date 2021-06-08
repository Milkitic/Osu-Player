using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly MusicScore _musicScore;

        public NoteChannel(string path, MusicScore musicScore, AudioPlaybackEngine engine)
            : base(engine)
        {
            _path = path;
            _musicScore = musicScore;
        }

        public override async Task<IEnumerable<SoundElement>> GetSoundElements()
        {
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
                    var isGeneric = Generics.Contains(s);

                    var name = s + "_" +
                               KeysoundFilenameUtilities.GetFileSuffix(k.ScalePiano);
                    var path = isGeneric
                        ? Path.Combine(Domain.DefaultPath, "generic", s, name)
                        : Path.Combine(dir, name);
                    float volume = (float)k.Velocity / sbyte.MaxValue;
                    return SoundElement.Create(k.StartTimingMsec, volume, k.Balance, $"{path}.wav", k.EndTimingMsec);
                });
            return new List<SoundElement>(ele);
        }
    }
}