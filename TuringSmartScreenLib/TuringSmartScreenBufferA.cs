namespace TuringSmartScreenLib;

using System.Runtime.CompilerServices;

#pragma warning disable CA1819
#pragma warning disable IDE0032
// ReSharper disable ConvertToAutoProperty
public sealed class TuringSmartScreenBufferA
{
    private readonly int width;

    private readonly int height;

    private readonly byte[] buffer;

    public int Width => width;

    public int Height => height;

    public byte[] Buffer => buffer;

    public TuringSmartScreenBufferA(int width, int height)
    {
        this.width = width;
        this.height = height;
        buffer = new byte[width * height * 2];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPixel(int x, int y, byte r, byte g, byte b)
    {
        var rgb = ((r >> 3) << 11) | ((g >> 2) << 5) | (b >> 3);
        var offset = ((y * width) + x) * 2;
        buffer[offset] = (byte)((rgb >> 8) & 0xFF);
        buffer[offset + 1] = (byte)(rgb & 0xFF);
    }
}
// ReSharper restore ConvertToAutoProperty
#pragma warning restore IDE0032
#pragma warning restore CA1819
