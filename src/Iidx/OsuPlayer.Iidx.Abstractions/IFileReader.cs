namespace OsuPlayer.Iidx.Abstractions;

/// <summary>
/// Marker for readers that consume a whole file/stream into memory in a single pass.
/// Mirrors the <c>IIDXToolbox.Abstractions.IFileReader</c> contract so existing
/// reader implementations can be ported with minimal changes.
/// </summary>
public interface IFileReader
{
    /// <summary>
    /// Reads the entirety of the underlying stream into in-memory state. Must be
    /// called before any enumeration accessor on the implementing reader.
    /// </summary>
    void ReadToEnd();
}