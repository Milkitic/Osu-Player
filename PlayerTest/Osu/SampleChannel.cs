﻿using OSharp.Beatmap;
using PlayerTest.Player;
using PlayerTest.Player.Subchannels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PlayerTest.Osu
{
    public class SampleChannel : MultiElementsChannel
    {
        private readonly OsuMixPlayer _player;
        private readonly OsuFile _osuFile;
        private readonly string _sourceFolder;
        private NightcoreTilingProvider _nightcore;

        public SampleChannel(OsuMixPlayer player, OsuFile osuFile, string sourceFolder, AudioPlaybackEngine engine)
            : base(engine)
        {
            _player = player;
            _osuFile = osuFile;
            _sourceFolder = sourceFolder;

            Description = nameof(SampleChannel);
        }

        public override async Task<IEnumerable<SoundElement>> GetSoundElements()
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
                            OsuMixPlayer.GetFileUntilFind(_sourceFolder,
                                Path.GetFileNameWithoutExtension(sample.Filename))
                        );
                        elements.Add(element);
                    });
            });

            var elementList = new List<SoundElement>(elements);

            if (PlaybackRate.Equals(1.5f) && !UseTempo)
            {
                var duration = MathEx.Max(_player.MusicChannel.ChannelEndTime,
                    _player.HitsoundChannel.ChannelEndTime,
                    TimeSpan.FromMilliseconds(samples.Max(k => k.Offset))
                );
                _nightcore = new NightcoreTilingProvider(_osuFile, duration);
                elementList.AddRange(await _nightcore.GetSoundElements());
            }

            return elementList;
        }

        public override async Task SetPlaybackRate(float rate, bool useTempo)
        {
            var oldRate = PlaybackRate;
            var oldTempo = UseTempo;
            await base.SetPlaybackRate(rate, useTempo);
            if (!oldRate.Equals(PlaybackRate) && oldTempo != UseTempo)
            {
                SoundElements = null;
                await RequeueAsync(Position);
            }
        }
    }
}