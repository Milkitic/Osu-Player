using Coosu.Beatmap;
using KeyAsio.Core.Audio;
using KeyAsio.Core.Audio.Caching;
using KeyAsio.Core.Audio.SampleProviders;
using Microsoft.Extensions.Logging.Abstractions;
using NAudio.Wave;
using Xunit;

namespace OsuPlayer.Media.Audio.Tests;

public sealed class OsuBeatmapAudioSessionTests
{
    [Fact]
    public async Task LoadAsync_MissingReferencedMusicFile_UsesBeatmapDurationForSilentTrack()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var mapPath = Path.Combine(root, "map.osu");
        await File.WriteAllTextAsync(mapPath, """
                                             osu file format v14

                                             [General]
                                             AudioFilename: missing.mp3
                                             Mode: 0

                                             [Metadata]
                                             Title:Silent Track
                                             Artist:Test Artist
                                             Creator:Test Creator
                                             Version:Normal
                                             BeatmapID:1
                                             BeatmapSetID:1

                                             [Difficulty]
                                             HPDrainRate:5
                                             CircleSize:4
                                             OverallDifficulty:5
                                             ApproachRate:9
                                             SliderMultiplier:1.4
                                             SliderTickRate:1

                                             [TimingPoints]
                                             0,500,4,2,0,100,1,0

                                             [HitObjects]
                                             256,192,1234,1,0,0:0:0:0:
                                             """);

        var audioCacheManager = new AudioCacheManager(NullLogger<AudioCacheManager>.Instance);
        var playbackEngine = new FakePlaybackEngine();
        await using var session = new OsuBeatmapAudioSession(
            playbackEngine,
            new StandaloneMusicTransport(playbackEngine),
            audioCacheManager);

        try
        {
            var osuFile = await OsuFile.ReadFromFileAsync(mapPath, options => options.ExcludeSection("Editor"));
            await session.LoadAsync(osuFile, new OsuAudioSessionOptions
            {
                Resources = new BeatmapResources
                {
                    BeatmapFolder = root,
                    BeatmapFilename = Path.GetFileName(mapPath),
                    AudioFilename = "missing.mp3",
                    DefaultHitsoundFolder = "",
                    UserSkinFolder = "",
                },
            });

            Assert.InRange(session.Duration.TotalMilliseconds, 1_233, 1_234);
        }
        finally
        {
            audioCacheManager.ClearAll();
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class FakePlaybackEngine : IPlaybackEngine
    {
        private readonly QueueMixingSampleProvider _mixer =
            new(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));

        public event Action<DeviceDescription>? DeviceStarted { add { } remove { } }
        public event Action? DeviceStopped { add { } remove { } }
        public event Action<Exception>? DeviceError { add { } remove { } }

        public IWavePlayer? CurrentDevice => null;
        public DeviceDescription? CurrentDeviceDescription => null;
        public WaveFormat EngineWaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
        public WaveFormat SourceWaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
        public WaveFormat? WaveFormat => SourceWaveFormat;
        public IMixingSampleProvider EffectMixer => _mixer;
        public IMixingSampleProvider MusicMixer => _mixer;
        public IMixingSampleProvider RootMixer => _mixer;
        public ISampleProvider RootSampleProvider => _mixer;
        public LimiterType LimiterType { get; set; }
        public float MainVolume { get; set; }
        public float EffectVolume { get; set; }
        public float MusicVolume { get; set; }

        public void AddInput(ISampleProvider input) { }
        public void RemoveInput(ISampleProvider input) { }
        public void StartDevice(DeviceDescription? deviceDescription, WaveFormat? waveFormat = null) { }
        public void StopDevice() { }
        public void Dispose() { }
    }
}
