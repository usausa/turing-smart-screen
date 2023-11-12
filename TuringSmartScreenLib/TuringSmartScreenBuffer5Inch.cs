namespace TuringSmartScreenLib;

using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

#pragma warning disable CA1819
#pragma warning disable IDE0032
public sealed class TuringSmartScreenBuffer5Inch : IScreenBuffer
{

    internal byte[] img_buffer = new byte[0];
    public int Width { get; private set; }
    public int SX { get; private set; }
    public int SY { get; private set; }

    public int Height { get; private set; }

    public int Length => img_buffer.Length;

    public void SetPixel(int x, int y, byte r, byte g, byte b)
    {
        img_buffer[(y * Width) + x] = r;
        img_buffer[(y * Width) + x + 1] = g;
        img_buffer[(y * Width) + x + 2] = b;
    }
    public void Clear(byte r = 0, byte g = 0, byte b = 0) => img_buffer = new byte[0];
    public void SetRGB(int sw, int sh, byte[] buffer)
    {
        Width = sw;
        Height = sh;
        img_buffer = buffer;
    }

    internal bool IsEmpty() {
        return img_buffer.Length == 0;
    }
}
#pragma warning restore IDE0032
#pragma warning restore CA1819
