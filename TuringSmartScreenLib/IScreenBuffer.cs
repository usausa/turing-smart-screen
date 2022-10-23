namespace TuringSmartScreenLib;

using System;

public interface IScreenBuffer
{
    int Width { get; }

    int Height { get; }

    byte[] Buffer { get; }

    void SetPixel(int x, int y, byte r, byte g, byte b);
}
