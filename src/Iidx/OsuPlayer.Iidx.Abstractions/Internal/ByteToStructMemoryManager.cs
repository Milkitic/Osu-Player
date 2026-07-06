using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace OsuPlayer.Iidx.Abstractions.Internal;

/// <summary>
/// Bridges a <see cref="Memory{Byte}"/> payload to a <see cref="Memory{T}"/>
/// of structs without copies. Used when chart event data is read once into a
/// pooled buffer and then iterated as typed structs.
/// </summary>
internal sealed class ByteToStructMemoryManager<T> : MemoryManager<T> where T : struct
{
    private readonly Memory<byte> _source;
    private GCHandle _handle;
    private int _refCount;

    public ByteToStructMemoryManager(Memory<byte> source)
    {
        if (source.Length % Unsafe.SizeOf<T>() != 0)
        {
            throw new ArgumentException("Source memory length must be a multiple of the size of T.", nameof(source));
        }

        _source = source;
    }

    public override Span<T> GetSpan() => MemoryMarshal.Cast<byte, T>(_source.Span);

    public override unsafe MemoryHandle Pin(int elementIndex = 0)
    {
        if (!MemoryMarshal.TryGetArray(_source, out ArraySegment<byte> segment))
        {
            throw new NotSupportedException("Pinning is only supported for Memory<T> backed by an array.");
        }

        if (Interlocked.Increment(ref _refCount) == 1)
        {
            _handle = GCHandle.Alloc(segment.Array, GCHandleType.Pinned);
        }

        var pointer = (byte*)_handle.AddrOfPinnedObject()
                      + segment.Offset
                      + elementIndex * Unsafe.SizeOf<T>();
        return new MemoryHandle(pointer, _handle, this);
    }

    public override void Unpin()
    {
        if (Interlocked.Decrement(ref _refCount) == 0 && _handle.IsAllocated)
        {
            _handle.Free();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (_refCount > 0)
        {
            _refCount = 0;
            if (_handle.IsAllocated)
            {
                _handle.Free();
            }
        }
    }
}
