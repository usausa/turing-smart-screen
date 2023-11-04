namespace TuringSmartScreenLib;

using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

#pragma warning disable CA1819
#pragma warning disable IDE0032
public sealed class TuringSmartScreenBuffer5Inch : IScreenBuffer
{

    private byte[] pngBuffer = new byte[0];
    private byte[] RGBBuffer = new byte[0];
    public int Width { get; private set; }
    public int SX { get; private set; }
    public int SY { get; private set; }

    public bool IsPngFullscreen { get; private set; }

    public int Height { get; private set; }

    public void SetPNGData(byte[] bytes, int height, int width, int sx, int sy)
    {
        pngBuffer = bytes;
        Height = height;
        Width = width;
        SX = sx;
        SY = sy;
        IsPngFullscreen = true;
    }
    public void SetPixel(int x, int y, byte r, byte g, byte b)
    {
        RGBBuffer[(y * Width) + x] = r;
        RGBBuffer[(y * Width) + x + 1] = g;
        RGBBuffer[(y * Width) + x + 2] = b;
    }
    public void Clear(byte r = 0, byte g = 0, byte b = 0) => pngBuffer = new byte[0];
    public void SetRGB(int sw, int sh)
    {
        Width = sw;
        Height = sh;
        RGBBuffer = new byte[sw * sh * 3];
    }

    internal bool IsEmpty() {
        return RGBBuffer.Length == 0 && pngBuffer.Length == 0;
    }
}
#pragma warning restore IDE0032
#pragma warning restore CA1819
