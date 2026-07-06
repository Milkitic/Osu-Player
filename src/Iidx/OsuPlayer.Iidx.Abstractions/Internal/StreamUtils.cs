using Microsoft.IO;

namespace OsuPlayer.Iidx.Abstractions.Internal;

/// <summary>
/// Shared <see cref="RecyclableMemoryStreamManager"/> for IIDX readers that need
/// to buffer extracted payloads (charts, audio) without pressuring the GC.
/// </summary>
internal static class StreamUtils
{
    public static RecyclableMemoryStreamManager RecyclableMemoryStreamManager { get; } = new();
}