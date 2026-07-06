using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace OsuPlayer.Iidx.Abstractions.Internal;

/// <summary>
/// Low-allocation binary reading helpers for IIDX on-disk structs.
/// Ported from <c>IIDXToolbox.Readers.Internal.BinaryReaderExtensions</c>.
/// </summary>
internal static class BinaryReaderExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ReadStructure<T>(this BinaryReader binaryReader) where T : unmanaged
        => binaryReader.BaseStream.ReadStructure<T>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadStructure<T>(this BinaryReader binaryReader, out T structure) where T : unmanaged
        => binaryReader.BaseStream.ReadStructure(out structure);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ReadStructure<T>(this Stream stream) where T : unmanaged
    {
        int size = Unsafe.SizeOf<T>();
        Span<byte> buffer = stackalloc byte[size];
        int bytesRead = stream.Read(buffer);
        if (bytesRead < size)
        {
            throw new EndOfStreamException(
                $"End of stream reached. Expected {size} bytes to populate struct '{typeof(T).Name}', but only {bytesRead} bytes were available.");
        }

        return MemoryMarshal.Read<T>(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadStructure<T>(this Stream stream, out T structure) where T : unmanaged
    {
        Unsafe.SkipInit(out structure);
        int size = Unsafe.SizeOf<T>();
        Span<byte> span = MemoryMarshal.CreateSpan(ref Unsafe.As<T, byte>(ref structure), size);
        int bytesRead = stream.Read(span);
        if (bytesRead < size)
        {
            structure = default;
            throw new EndOfStreamException(
                $"End of stream reached. Expected {size} bytes to populate struct '{typeof(T).Name}', but only {bytesRead} bytes were available.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadStructureArray<T>(this BinaryReader binaryReader, Span<T> destination) where T : unmanaged
        => binaryReader.BaseStream.ReadStructureArray(destination);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadStructureArray<T>(this Stream stream, Span<T> destination) where T : unmanaged
    {
        if (destination.IsEmpty) return;
        Span<byte> byteSpan = MemoryMarshal.AsBytes(destination);
        stream.ReadExactly(byteSpan);
    }
}