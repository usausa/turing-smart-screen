namespace TuringSmartScreenLib;

using System.Buffers;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0032
// ReSharper disable ConvertToAutoProperty
public sealed class ScreenBufferBgra8888 : IScreenBuffer
{
    private readonly int width;

    private readonly int height;

    private byte[] buffer;

    public int Width => width;

    public int Height => height;

    internal byte[] Buffer => buffer;

    public ScreenBufferBgra8888(int width, int height)
    {
        this.width = width;
        this.height = height;
        buffer = ArrayPool<byte>.Shared.Rent(width * height * 4);
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
    public void SetPixel(int x, int y, byte r, byte g, byte b)
    {
        var offset = ((y * width) + x) * 4;
        buffer[offset] = b;
        buffer[offset + 1] = g;
        buffer[offset + 2] = r;
        buffer[offset + 3] = 255;
    }

    public void Clear() => Clear(0, 0, 0);

    public void Clear(byte r, byte g, byte b)
    {
        buffer[0] = r;
        buffer[1] = g;
        buffer[2] = b;
        buffer[3] = 255;

        var length = 4;
        var size = Width * Height * 4;
        while (length < size - length)
        {
            buffer.AsSpan(0, length).CopyTo(buffer.AsSpan(length));

            length += length;
        }

        if (length < size)
        {
            buffer.AsSpan(0, size - length).CopyTo(buffer.AsSpan(length));
        }
    }
}
// ReSharper restore ConvertToAutoProperty
#pragma warning restore IDE0032
