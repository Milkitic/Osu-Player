﻿using Milky.OsuPlayer.Media.Audio.Player;
using Milky.OsuPlayer.Media.Audio.Player.Subchannels;
using OSharp.Beatmap;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Milky.OsuPlayer.Shared.Models.NostModels;

namespace Milky.OsuPlayer.Media.Audio
{
    public class SampleChannel : MultiElementsChannel
    {
        private readonly OsuMixPlayer _player;
        private readonly OsuFile _osuFile;
        private readonly string _sourceFolder;
        private NightcoreTilingProvider _nightcore;
        private object _otherFile;
        private readonly string _path;

        public SampleChannel(OsuMixPlayer player, OsuFile osuFile, string sourceFolder, AudioPlaybackEngine engine)
            : base(engine)
        {
            _player = player;
            _osuFile = osuFile;
            _sourceFolder = sourceFolder;

            Description = nameof(SampleChannel);
        }

        public SampleChannel(OsuMixPlayer player, object otherFile, string path, string sourceFolder, AudioPlaybackEngine engine)
            : base(engine)
        {
            _player = player;
            _otherFile = otherFile;
            _path = path;
            _sourceFolder = sourceFolder;

            Description = nameof(SampleChannel);
        }

        public override async Task<IEnumerable<SoundElement>> GetSoundElements()
        {
            if (_osuFile != null)
            {
                var elements = new ConcurrentBag<SoundElement>();
                var samples = _osuFile.Events.SampleInfo;
                if (samples == null)
                    return new List<SoundElement>(elements);

                await Task.Run(() =>
                {
                    samples.AsParallel()
                        .WithDegreeOfParallelism(Environment.ProcessorCount > 1 ? Environment.ProcessorCount - 1 : 1)
                        .ForAll(sample =>
                        {
                            var element = SoundElement.Create(sample.Offset, sample.Volume / 100f, 0,
                                _player.GetFileUntilFind(_sourceFolder,
                                    Path.GetFileNameWithoutExtension(sample.Filename))
                            );
                            elements.Add(element);
                        });
                }).ConfigureAwait(false);

                var elementList = new List<SoundElement>(elements);

                if (PlaybackRate.Equals(1.5f) && !UseTempo)
                {
                    var duration = MathEx.Max(_player.MusicChannel.ChannelEndTime,
                        _player.HitsoundChannel.ChannelEndTime,
                        TimeSpan.FromMilliseconds(samples.Count == 0 ? 0 : samples.Max(k => k.Offset))
                    );
                    _nightcore = new NightcoreTilingProvider(_osuFile, duration);
                    elementList.AddRange(await _nightcore.GetSoundElements().ConfigureAwait(false));
                }

                return elementList;
            }
            else
            {
                if (_otherFile is MusicScore score)
                {
                    return new List<SoundElement>();
                }

                throw new NotImplementedException("unknown file");
            }
        }

        public override async Task SetPlaybackRate(float rate, bool useTempo)
        {
            var oldRate = PlaybackRate;
            var oldTempo = UseTempo;
            await base.SetPlaybackRate(rate, useTempo).ConfigureAwait(false);
            if (oldTempo != UseTempo)
            {
                SoundElements = null;
                await RequeueAsync(Position).ConfigureAwait(false);
            }
        }
    }
}