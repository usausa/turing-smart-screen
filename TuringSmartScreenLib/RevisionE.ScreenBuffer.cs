namespace TuringSmartScreenLib;

using System.Buffers;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0032
// ReSharper disable ConvertToAutoProperty
public sealed class TuringSmartScreenBufferE : IScreenBuffer
{
    private readonly int width;

    private readonly int height;

    private byte[] buffer;

    public int Width => width;

    public int Height => height;

    internal byte[] Buffer => buffer;

    public TuringSmartScreenBufferE(int width, int height)
    {
        this.width = width;
        this.height = height;
        buffer = ArrayPool<byte>.Shared.Rent(width * height * 3);
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
        var offset = ((y * width) + x) * 3;
        buffer[offset] = b;
        buffer[offset + 1] = g;
        buffer[offset + 2] = r;
    }

    public void Clear(byte r = 0, byte g = 0, byte b = 0)
    {
        if ((r == g) && (r == b))
        {
            buffer.AsSpan(0, width * height * 2).Fill(r);
        }
        else
        {
            for (var offset = 0; offset < width * height * 3; offset += 3)
            {
                buffer[offset] = b;
                buffer[offset + 1] = g;
                buffer[offset + 2] = r;
            }
        }
    }
}
// ReSharper restore ConvertToAutoProperty
#pragma warning restore IDE0032
