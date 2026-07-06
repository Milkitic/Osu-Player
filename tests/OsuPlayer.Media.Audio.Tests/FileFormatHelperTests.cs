using System.Text;
using KeyAsio.Core.Audio.Utils;
using Xunit;

namespace OsuPlayer.Media.Audio.Tests;

public sealed class FileFormatHelperTests
{
    [Theory]
    [MemberData(nameof(KnownHeaderCases))]
    public void DetermineFileFormatFromStream_DetectsKnownHeaders(byte[] data, FileFormat expected)
    {
        using var stream = new MemoryStream(data);

        Assert.Equal(expected, FileFormatHelper.DetermineFileFormatFromStream(stream));
    }

    [Fact]
    public void DetermineFileFormatFromStream_PreservesOriginalPosition()
    {
        using var stream = new MemoryStream(CreateRiffLike("RIFF", "WAVE"));
        stream.Position = 3;

        var format = FileFormatHelper.DetermineFileFormatFromStream(stream);

        Assert.Equal(FileFormat.Wav, format);
        Assert.Equal(3, stream.Position);
    }

    [Theory]
    [MemberData(nameof(UnsupportedHeaderCases))]
    public void DetermineFileFormatFromStream_DoesNotTreatUnsupportedHeadersAsSupported(byte[] data)
    {
        using var stream = new MemoryStream(data);

        Assert.Equal(FileFormat.Others, FileFormatHelper.DetermineFileFormatFromStream(stream));
    }

    [Fact]
    public void DetermineFileFormatFromStream_DetectsMpegLayer3AfterId3AndPadding()
    {
        using var stream = new MemoryStream(CreateLayer3MpegData(withId3: true, paddingLength: 32));

        Assert.Equal(FileFormat.Mp3Id3, FileFormatHelper.DetermineFileFormatFromStream(stream));
    }

    [Fact]
    public void DetermineFileFormatFromStream_DoesNotTreatLayer2AsMp3()
    {
        using var stream = new MemoryStream(CreateLayer2MpegData());

        Assert.Equal(FileFormat.Others, FileFormatHelper.DetermineFileFormatFromStream(stream));
    }

    public static IEnumerable<object[]> KnownHeaderCases()
    {
        yield return [CreateRiffLike("RIFF", "WAVE"), FileFormat.Wav];
        yield return [CreateRiffLike("RF64", "WAVE"), FileFormat.Wav];
        yield return [CreateHeader("OggS"), FileFormat.Ogg];
        yield return [CreateHeader("fLaC"), FileFormat.Flac];
        yield return [CreateRiffLike("FORM", "AIFF"), FileFormat.Aiff];
        yield return [CreateRiffLike("FORM", "AIFC"), FileFormat.Aiff];
        yield return [CreateAsfHeader(), FileFormat.Wma];
        yield return [CreateLayer3MpegData(withId3: false, paddingLength: 0), FileFormat.Mp3];
    }

    public static IEnumerable<object[]> UnsupportedHeaderCases()
    {
        yield return [CreateRiffLike("RIFF", "AVI ")];
        yield return [CreateRiffLike("RIFX", "WAVE")];
        yield return [CreateId3TaggedDataWithoutMpegFrame()];
    }

    private static byte[] CreateHeader(string signature)
    {
        var data = new byte[64];
        Encoding.ASCII.GetBytes(signature).CopyTo(data.AsSpan());
        return data;
    }

    private static byte[] CreateRiffLike(string container, string format)
    {
        var data = new byte[64];
        Encoding.ASCII.GetBytes(container).CopyTo(data.AsSpan(0, 4));
        Encoding.ASCII.GetBytes(format).CopyTo(data.AsSpan(8, 4));
        return data;
    }

    private static byte[] CreateAsfHeader()
    {
        var data = new byte[64];
        new byte[]
        {
            0x30, 0x26, 0xB2, 0x75,
            0x8E, 0x66, 0xCF, 0x11,
            0xA6, 0xD9, 0x00, 0xAA,
            0x00, 0x62, 0xCE, 0x6C
        }.CopyTo(data.AsSpan());
        return data;
    }

    private static byte[] CreateId3TaggedDataWithoutMpegFrame()
    {
        const int id3PayloadLength = 5;
        var data = new byte[64];
        Encoding.ASCII.GetBytes("ID3").CopyTo(data.AsSpan(0, 3));
        data[3] = 4;
        data[9] = id3PayloadLength;
        Encoding.ASCII.GetBytes("not-mpeg").CopyTo(data.AsSpan(10 + id3PayloadLength));
        return data;
    }

    private static byte[] CreateLayer3MpegData(bool withId3, int paddingLength)
    {
        return CreateMpegData(
            withId3,
            paddingLength,
            [0xFF, 0xFB, 0xB4, 0x01],
            frameLength: 576);
    }

    private static byte[] CreateLayer2MpegData()
    {
        return CreateMpegData(
            withId3: false,
            paddingLength: 0,
            [0xFF, 0xFD, 0x90, 0x00],
            frameLength: 522);
    }

    private static byte[] CreateMpegData(
        bool withId3,
        int paddingLength,
        byte[] frameHeader,
        int frameLength)
    {
        const int frameCount = 2;
        const int id3PayloadLength = 5;
        var id3Length = withId3 ? 10 + id3PayloadLength : 0;
        var data = new byte[id3Length + paddingLength + frameLength * frameCount];

        if (withId3)
        {
            Encoding.ASCII.GetBytes("ID3").CopyTo(data.AsSpan(0, 3));
            data[3] = 4;
            data[9] = id3PayloadLength;
        }

        data.AsSpan(id3Length, paddingLength).Fill(0x55);
        var firstFrameOffset = id3Length + paddingLength;
        for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
        {
            frameHeader.CopyTo(data.AsSpan(firstFrameOffset + frameIndex * frameLength, frameHeader.Length));
        }

        return data;
    }
}
