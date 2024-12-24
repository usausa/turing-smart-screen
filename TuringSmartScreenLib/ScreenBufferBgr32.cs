namespace TuringSmartScreenLib;

using System.Buffers;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0032
// ReSharper disable ConvertToAutoProperty
public sealed class ScreenBufferBgr32 : IScreenBuffer
{
    private readonly int width;

    private readonly int height;

    private byte[] buffer;

    public int Width => width;

    public int Height => height;

    internal byte[] Buffer => buffer;

    public ScreenBufferBgr32(int width, int height)
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

    public void Clear(byte r = 0, byte g = 0, byte b = 0)
    {
        // TODO optimize
        for (var offset = 0; offset < width * height * 4; offset += 4)
        {
            buffer[offset] = b;
            buffer[offset + 1] = g;
            buffer[offset + 2] = r;
            buffer[offset + 3] = 255;
        }
    }
}
// ReSharper restore ConvertToAutoProperty
#pragma warning restore IDE0032
