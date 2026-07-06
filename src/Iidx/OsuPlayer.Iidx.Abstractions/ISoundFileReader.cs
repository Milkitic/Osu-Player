namespace OsuPlayer.Iidx.Abstractions;

/// <summary>
/// A <see cref="IFileReader"/> whose payload is a container of audio chunks
/// (e.g. <c>.2dx</c> / <c>.s3p</c>). Exposes the chunks as raw, decoded bytes
/// without any platform-specific audio dependency.
/// </summary>
public interface ISoundFileReader : IFileReader
{
    /// <summary>
    /// The native file extension of the container (e.g. <c>.wav</c>, <c>.wma</c>).
    /// Used by callers to select the appropriate decoder pipeline.
    /// </summary>
    string SoundFileExtension { get; }

    /// <summary>
    /// Number of audio entries discovered after <see cref="ReadToEnd"/>.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Enumerates the decoded audio payloads as owned <see cref="ReadOnlyMemory{T}"/>
    /// buffers. The caller is responsible for respecting the lifetime of the
    /// underlying reader: disposing the reader invalidates the returned buffers.
    /// </summary>
    IEnumerable<ReadOnlyMemory<byte>> EnumerateExtractedAudio();
}