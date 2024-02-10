namespace TuringSmartScreenLib;

// TODO
public sealed class TuringSmartScreenBufferC2 : IScreenBuffer
{
    internal byte[] ImgBuffer { get; set; } = [];

    public int Width { get; private set; }

    public int Height { get; private set; }

    public int Length => ImgBuffer.Length;

    public void Dispose()
    {
        // TODO
    }

    public void SetPixel(int x, int y, byte r, byte g, byte b)
    {
        ImgBuffer[(y * Width) + x] = r;
        ImgBuffer[(y * Width) + x + 1] = g;
        ImgBuffer[(y * Width) + x + 2] = b;
    }

    public void Clear(byte r = 0, byte g = 0, byte b = 0) => ImgBuffer = [];

    public void SetRGB(int sw, int sh, byte[] buffer)
    {
        Width = sw;
        Height = sh;
        ImgBuffer = buffer;
    }

    internal bool IsEmpty()
    {
        return ImgBuffer.Length == 0;
    }
}
