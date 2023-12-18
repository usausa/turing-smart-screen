namespace TuringSmartScreenLib;

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0032
internal ref struct ByteBuffer
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

    public void Clear()
    {
        length = 0;
        buffer.AsSpan().Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GrowIfRequired(int sizeRequired)
    {
        if ((uint)sizeRequired > (uint)buffer.Length)
        {
            var newBuffer = ArrayPool<byte>.Shared.Rent(sizeRequired);
            newBuffer.AsSpan(length).Clear();
            buffer[..length].CopyTo(newBuffer.AsSpan());
            ArrayPool<byte>.Shared.Return(buffer);
            buffer = newBuffer;
        }
    }

    public void Skip(int size)
    {
        length += size;
    }

    public void Write(byte value)
    {
        GrowIfRequired(length + 1);
        buffer[length] = value;
        length++;
    }

    public void Write(byte[] value)
    {
        GrowIfRequired(length + value.Length);
        value.CopyTo(buffer[length..].AsSpan());
        length += value.Length;
    }

    public void Put(int index, byte value)
    {
        GrowIfRequired(index + 1);
        buffer[index] = value;
    }

    public void Put(int index, byte[] value)
    {
        GrowIfRequired(index + value.Length);
        value.CopyTo(buffer[index..].AsSpan());
    }
}
#pragma warning restore IDE0032
