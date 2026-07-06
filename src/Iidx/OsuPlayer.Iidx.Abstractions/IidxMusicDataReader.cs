using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text;
using OsuPlayer.Iidx.Abstractions.Internal;
using OsuPlayer.Iidx.Abstractions.Structures;

namespace OsuPlayer.Iidx.Abstractions;

/// <summary>
/// Reads an IIDX <c>music_data.bin</c> database and projects raw records into
/// normalized <see cref="IidxMusicEntry"/> objects.
/// </summary>
public sealed class IidxMusicDataReader : IFileReader, IDisposable
{
    private const string HeaderMagic = "IIDX";

    private readonly Stream _stream;
    private readonly bool _leaveOpen;
    private readonly List<IidxMusicEntry> _entries = [];

    public IidxMusicDataReader(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        _stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public IidxMusicDataReader(Stream stream, bool leaveOpen = false)
    {
        if (!stream.CanRead)
        {
            throw new InvalidOperationException("Stream must support reading.");
        }

        if (!stream.CanSeek)
        {
            throw new InvalidOperationException("Stream must support seeking.");
        }

        _stream = stream;
        _leaveOpen = leaveOpen;
    }

    public IReadOnlyList<IidxMusicEntry> Entries => new ReadOnlyCollection<IidxMusicEntry>(_entries);

    public void ReadToEnd()
    {
        using var binaryReader = new BinaryReader(_stream, Encoding.UTF8, leaveOpen: true);
        binaryReader.ReadStructure<MusicDbHeader32>(out var header);

        var headerFlag = ReadHeaderFlag(in header);
        if (headerFlag != HeaderMagic)
        {
            throw new NotSupportedException("File header does not match expected IIDX music_data.bin format.");
        }

        _entries.Clear();
        if (header.SongCount <= 0)
        {
            return;
        }

        var slotBytes = header.SlotCount * MusicDbHeader32.SlotSize;
        binaryReader.BaseStream.Seek(slotBytes, SeekOrigin.Current);

        var rawEntries = new MusicDbEntry32[header.SongCount];
        binaryReader.ReadStructureArray(rawEntries);

        foreach (ref readonly var rawEntry in rawEntries.AsSpan())
        {
            _entries.Add(IidxMusicEntryDecoder.ToMusicEntry(in rawEntry));
        }
    }

    public void Dispose()
    {
        if (!_leaveOpen)
        {
            _stream.Dispose();
        }
    }

    private static unsafe string ReadHeaderFlag(in MusicDbHeader32 header)
    {
        var copy = header;
        return Encoding.ASCII.GetString(
            MemoryMarshal.CreateReadOnlySpan(ref copy.HeaderFlag[0], MusicDbHeader32.HeaderFlagSize));
    }
}
