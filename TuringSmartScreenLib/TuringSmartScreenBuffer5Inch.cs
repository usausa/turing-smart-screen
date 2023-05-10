namespace TuringSmartScreenLib;

using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

#pragma warning disable CA1819
#pragma warning disable IDE0032
// ReSharper disable ConvertToAutoProperty
public sealed class TuringSmartScreenBuffer5Inch : IScreenBuffer
{

    private byte[] pngBuffer = new byte[0];

    public int Width { get; private set; }
    public int SX { get; private set; }
    public int SY { get; private set; }
    public int Height { get; private set; }

    public byte[] Buffer => pngBuffer;

    public void SetPNGData(byte[] bytes, int height, int width, int sx, int sy)
    {
        pngBuffer = bytes;
        Height = height;
        Width = width;
        SX = sx;
        SY = sy;
    }
    public void SetPixel(int x, int y, byte r, byte g, byte b) => throw new NotImplementedException();
    public void Clear(byte r = 0, byte g = 0, byte b = 0) => pngBuffer = new byte[0];
}
// ReSharper restore ConvertToAutoProperty
#pragma warning restore IDE0032
#pragma warning restore CA1819
