namespace TuringSmartScreenLib;

public interface IScreenBuffer : IDisposable
{
    int Width { get; }

    int Height { get; }

    void SetPixel(int x, int y, byte r, byte g, byte b);

    void Clear(byte r = 0, byte g = 0, byte b = 0);
}
