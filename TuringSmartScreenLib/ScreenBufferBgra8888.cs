namespace TuringSmartScreenLib;

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
        ref var p = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(buffer), ((y * width) + x) * 4);
        p = b;
        Unsafe.Add(ref p, 1) = g;
        Unsafe.Add(ref p, 2) = r;
        Unsafe.Add(ref p, 3) = 255;
    }

    public void Clear() => Clear(0, 0, 0);

    public void Clear(byte r, byte g, byte b)
    {
        var pattern = (Span<byte>)stackalloc byte[4];
        pattern[0] = b;
        pattern[1] = g;
        pattern[2] = r;
        pattern[3] = 255;
        Helper.Fill(buffer.AsSpan(0, Width * Height * 4), pattern);
    }
}
// ReSharper restore ConvertToAutoProperty
#pragma warning restore IDE0032
