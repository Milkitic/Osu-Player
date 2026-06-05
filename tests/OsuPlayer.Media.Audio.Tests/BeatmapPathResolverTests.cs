using Xunit;

namespace OsuPlayer.Media.Audio.Tests;

public class BeatmapPathResolverTests
{
    [Fact]
    public void ResolveBeatmapPath_FreePath_ReturnsFreePath()
    {
        var path = BeatmapPathResolver.ResolveBeatmapPath(
            folder: "",
            beatmapFileName: "song.osu",
            isFromDb: false,
            freePath: @"C:\maps\song.osu");

        Assert.Equal(@"C:\maps\song.osu", path);
    }

    [Fact]
    public void ResolveBeatmapPath_FreePath_Empty_Throws()
    {
        Assert.Throws<InvalidDataException>(() =>
            BeatmapPathResolver.ResolveBeatmapPath(
                folder: "",
                beatmapFileName: "song.osu",
                isFromDb: false,
                freePath: ""));
    }

    [Fact]
    public void ResolveBeatmapPath_FromDb_CombinesFolder()
    {
        var path = BeatmapPathResolver.ResolveBeatmapPath(
            folder: @"C:\maps\1234",
            beatmapFileName: "Artist - Title (Creator) [Easy].osu",
            isFromDb: true,
            freePath: "");

        Assert.Equal(
            Path.Combine(@"C:\maps\1234", "Artist - Title (Creator) [Easy].osu"),
            path);
    }

    [Fact]
    public void ResolveChildPath_EmptyBaseFolder_Throws()
    {
        Assert.Throws<InvalidDataException>(() =>
            BeatmapPathResolver.ResolveChildPath("", "file.osu"));
    }

    [Fact]
    public void ResolveChildPath_EmptyChildPath_Throws()
    {
        Assert.Throws<InvalidDataException>(() =>
            BeatmapPathResolver.ResolveChildPath(@"C:\maps", ""));
    }

    [Fact]
    public void TryResolveChildPath_Empty_ReturnsNull()
    {
        Assert.Null(BeatmapPathResolver.TryResolveChildPath("", ""));
        Assert.Null(BeatmapPathResolver.TryResolveChildPath(@"C:\maps", ""));
        Assert.Null(BeatmapPathResolver.TryResolveChildPath("", "x.osu"));
    }

    [Fact]
    public void TryResolveChildPath_NonEmpty_ReturnsCombined()
    {
        Assert.Equal(
            Path.Combine(@"C:\maps", "song.osu"),
            BeatmapPathResolver.TryResolveChildPath(@"C:\maps", "song.osu"));
    }

    [Fact]
    public void ResolveBackgroundPath_MissingFile_FallsBackToDefault()
    {
        // The default-image path is only returned when the file actually
        // exists. Create a temp file so the test is hermetic.
        var tempDir = Path.Combine(Path.GetTempPath(), "osuplayer-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var fallback = Path.Combine(tempDir, "registration.jpg");
            File.WriteAllBytes(fallback, new byte[] { 0xFF, 0xD8, 0xFF });

            var result = BeatmapPathResolver.ResolveBackgroundPath(
                baseFolder: Path.Combine(tempDir, Guid.NewGuid().ToString("N")),
                backgroundFilename: "missing.jpg",
                defaultImagePath: fallback);

            Assert.Equal(fallback, result);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    [Fact]
    public void ResolveBackgroundPath_ExistingBeatmapBackground_PrefersBeatmap()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "osuplayer-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var beatmapFolder = Path.Combine(tempDir, "beatmap");
            Directory.CreateDirectory(beatmapFolder);
            var backgroundPath = Path.Combine(beatmapFolder, "bg.jpg");
            File.WriteAllBytes(backgroundPath, new byte[] { 0xFF, 0xD8, 0xFF });

            var fallback = Path.Combine(tempDir, "registration.jpg");
            File.WriteAllBytes(fallback, new byte[] { 0xFF, 0xD8, 0xFF });

            var result = BeatmapPathResolver.ResolveBackgroundPath(
                baseFolder: beatmapFolder,
                backgroundFilename: "bg.jpg",
                defaultImagePath: fallback);

            Assert.Equal(backgroundPath, result);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }
}
