namespace TuringSmartScreenLib;

#pragma warning disable CA1819
public interface IScreen : IDisposable
{
    void Reset();

    void Clear();

    void ScreenOff();

    void ScreenOn();

    void SetBrightness(byte level);

    void SetOrientation(ScreenOrientation orientation);

    IScreenBuffer CreateBuffer(int width, int height);

    void DisplayBitmap(int x, int y, int width, int height, byte[] bitmap);
}
#pragma warning restore CA1819
