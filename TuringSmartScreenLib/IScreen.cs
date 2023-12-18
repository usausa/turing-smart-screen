namespace TuringSmartScreenLib;

public interface IScreen : IDisposable
{
    int Width { get; }

    int Height { get; }

    ScreenOrientation Orientation { get; set; }

    void Reset();

    void Clear();

    void ScreenOff();

    void ScreenOn();

    void SetBrightness(byte level);

    IScreenBuffer CreateBuffer(int width, int height);

    void DisplayBitmap(int x, int y, int width, int height, IScreenBuffer buffer);
}
