using KeyAsio.Core.OsuAudio.Hitsounds;
using KeyAsio.Core.OsuAudio.Hitsounds.Playback;
using Xunit;

namespace OsuPlayer.Media.Audio.Tests;

public class BeatmapSetContextTests
{
    [Fact]
    public async Task GetHitsoundNodesAsync_HitObjectUsesNearbyFutureTimingSampleIndex()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            File.WriteAllText(Path.Combine(root, "nearby-future-timing.osu"), """
                                                                              osu file format v14

                                                                              [General]
                                                                              AudioFilename: audio.mp3
                                                                              SampleSet: Soft
                                                                              Mode: 0

                                                                              [Metadata]
                                                                              Title:Nearby future timing
                                                                              Artist:Test
                                                                              Creator:Test
                                                                              Version:Test

                                                                              [Difficulty]
                                                                              SliderMultiplier:1
                                                                              SliderTickRate:1

                                                                              [TimingPoints]
                                                                              0,500,4,2,60,80,1,0
                                                                              1002,-100,4,2,61,80,0,0

                                                                              [HitObjects]
                                                                              256,192,1000,1,4,0:0:0:0:
                                                                              """);
            File.WriteAllBytes(Path.Combine(root, "soft-hitnormal60.mp3"), [0]);
            File.WriteAllBytes(Path.Combine(root, "soft-hitnormal61.mp3"), [0]);
            File.WriteAllBytes(Path.Combine(root, "soft-hitfinish61.mp3"), [0]);

            var context = new BeatmapSetContext(root);
            await context.InitializeAsync("nearby-future-timing.osu");
            var osuFile = context.OsuFiles.Single();

            var events = await context.GetHitsoundNodesAsync(osuFile);
            var samples = events.OfType<SampleEvent>().ToArray();

            Assert.Contains(samples, sample =>
                sample.Filename == "soft-hitnormal61.mp3" &&
                sample.ResourceOwner == ResourceOwner.Beatmap);
            Assert.Contains(samples, sample =>
                sample.Filename == "soft-hitfinish61.mp3" &&
                sample.ResourceOwner == ResourceOwner.Beatmap);
            Assert.DoesNotContain(samples, sample => sample.Filename == "soft-hitnormal60.mp3");
            Assert.DoesNotContain(samples, sample => sample.Filename == "soft-hitfinish");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task GetHitsoundNodesAsync_SliderEdgeCeilingUsesNextTimingSampleIndex()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            File.WriteAllText(Path.Combine(root, "slider-edge-samples.osu"), """
                                                                            osu file format v14

                                                                            [General]
                                                                            AudioFilename: audio.mp3
                                                                            SampleSet: Soft
                                                                            Mode: 0

                                                                            [Metadata]
                                                                            Title:Slider edge samples
                                                                            Artist:Test
                                                                            Creator:Test
                                                                            Version:Test

                                                                            [Difficulty]
                                                                            SliderMultiplier:1.4
                                                                            SliderTickRate:1

                                                                            [TimingPoints]
                                                                            0,410.958904109589,4,2,0,80,1,0
                                                                            1000,-100,4,2,31,80,0,1
                                                                            1069,-100,4,2,32,80,0,1
                                                                            1137,-100,4,2,31,80,0,1
                                                                            1206,-100,4,2,25,80,0,1

                                                                            [HitObjects]
                                                                            256,192,1000,2,0,L|224:192,3,23.3333333333333,0|0|0|8,0:0|0:0|0:0|0:0,0:0:0:0:
                                                                            """);
            File.WriteAllBytes(Path.Combine(root, "soft-hitnormal25.mp3"), [0]);
            File.WriteAllBytes(Path.Combine(root, "soft-hitclap25.mp3"), [0]);

            var context = new BeatmapSetContext(root);
            await context.InitializeAsync("slider-edge-samples.osu");
            var osuFile = context.OsuFiles.Single();

            var events = await context.GetHitsoundNodesAsync(osuFile);
            var edgeSamples = events.OfType<SampleEvent>()
                .Where(sample => sample.Offset == 1206)
                .ToArray();

            Assert.Contains(edgeSamples, sample =>
                sample.Filename == "soft-hitnormal25.mp3" &&
                sample.ResourceOwner == ResourceOwner.Beatmap);
            Assert.Contains(edgeSamples, sample =>
                sample.Filename == "soft-hitclap25.mp3" &&
                sample.ResourceOwner == ResourceOwner.Beatmap);
            Assert.DoesNotContain(edgeSamples, sample => sample.Filename == "soft-hitclap");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task GetHitsoundNodesAsync_TimingSampleIndexResolvesBeatmapMp3Samples()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            File.WriteAllText(Path.Combine(root, "indexed-samples.osu"), """
                                                                       osu file format v14

                                                                       [General]
                                                                       AudioFilename: audio.mp3
                                                                       SampleSet: Soft
                                                                       Mode: 0

                                                                       [Metadata]
                                                                       Title:Indexed samples
                                                                       Artist:Test
                                                                       Creator:Test
                                                                       Version:Test

                                                                       [Difficulty]
                                                                       SliderMultiplier:1
                                                                       SliderTickRate:1

                                                                       [TimingPoints]
                                                                       0,500,4,2,0,80,1,0
                                                                       1000,-100,4,2,25,80,0,0

                                                                       [HitObjects]
                                                                       256,192,1000,1,8,0:0:0:0:
                                                                       """);
            File.WriteAllBytes(Path.Combine(root, "soft-hitnormal25.mp3"), [0]);
            File.WriteAllBytes(Path.Combine(root, "soft-hitclap25.mp3"), [0]);

            var context = new BeatmapSetContext(root);
            await context.InitializeAsync("indexed-samples.osu");
            var osuFile = context.OsuFiles.Single();

            var events = await context.GetHitsoundNodesAsync(osuFile);
            var samples = events.OfType<SampleEvent>().ToArray();

            Assert.Contains(samples, sample =>
                sample.Filename == "soft-hitnormal25.mp3" &&
                sample.ResourceOwner == ResourceOwner.Beatmap);
            Assert.Contains(samples, sample =>
                sample.Filename == "soft-hitclap25.mp3" &&
                sample.ResourceOwner == ResourceOwner.Beatmap);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
