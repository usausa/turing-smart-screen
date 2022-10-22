namespace TuringSmartScreenLib;

using System.Runtime.CompilerServices;

public sealed class TuringSmartScreenCanvasA
{
    private readonly TuringSmartScreenRevisionA screen;

    private readonly int top;

    private readonly int left;

    private readonly int width;

    private readonly int height;

    private readonly byte[] buffer;

    public TuringSmartScreenCanvasA(TuringSmartScreenRevisionA screen, int top, int left, int width, int height)
    {
        this.screen = screen;
        this.top = top;
        this.left = left;
        this.width = width;
        this.height = height;
        buffer = new byte[width * height * 2];
    }

    public void Write()
    {
        screen.DisplayBitmap(top, left, width, height, buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPixel(int x, int y, byte r, byte g, byte b)
    {
        var rgb = ((r >> 3) << 11) | ((g >> 2) << 5) | (b >> 3);
        var offset = ((y * width) + x) * 2;
        buffer[offset] = (byte)(rgb & 0xFF);
        buffer[offset + 1] = (byte)((rgb >> 8) & 0xFF);
    }
}
