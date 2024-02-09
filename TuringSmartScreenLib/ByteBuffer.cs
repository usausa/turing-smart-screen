namespace TuringSmartScreenLib;

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0032
internal struct ByteBuffer : IBufferWriter<byte>, IDisposable
{
    private byte[] buffer;

    private int length;

    public readonly int WrittenCount => length;

    public readonly byte[] Buffer => buffer;

    public ByteBuffer(int initialCapacity)
    {
        buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
        buffer.AsSpan().Clear();
    }

    public void Dispose()
    {
        if (buffer.Length > 0)
        {
            ArrayPool<byte>.Shared.Return(buffer);
            buffer = [];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        length = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count)
    {
        length += count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        GrowIfRequired(sizeHint);
        return buffer.AsMemory(length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        GrowIfRequired(sizeHint);
        return buffer.AsSpan(length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GrowIfRequired(int sizeHint)
    {
        if (sizeHint == 0)
        {
            sizeHint = 1;
        }

        var newSize = length + sizeHint;
        if ((uint)newSize > (uint)buffer.Length)
        {
            var newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
            newBuffer.AsSpan(length).Clear();
            buffer[..length].CopyTo(newBuffer.AsSpan());
            ArrayPool<byte>.Shared.Return(buffer);
            buffer = newBuffer;
        }
    }
}
#pragma warning restore IDE0032
