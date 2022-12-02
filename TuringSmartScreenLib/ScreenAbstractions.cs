namespace TuringSmartScreenLib;

internal sealed class ScreenWrapperRevisionA : IScreen
{
    private readonly TuringSmartScreenRevisionA screen;

    public ScreenWrapperRevisionA(TuringSmartScreenRevisionA screen)
    {
        this.screen = screen;
    }

    public void Dispose() => screen.Dispose();

    public void Reset() => screen.Reset();

    public void Clear() => screen.Clear();

    public void ScreenOff() => screen.ScreenOff();

    public void ScreenOn() => screen.ScreenOn();

    public void SetBrightness(byte level) => screen.SetBrightness(level);

    public void SetOrientation(ScreenOrientation orientation, int width, int height)
    {
        switch (orientation)
        {
            case ScreenOrientation.Portrait:
                screen.SetOrientation(TuringSmartScreenRevisionA.Orientation.Portrait, width, height);
                break;
            case ScreenOrientation.ReversePortrait:
                screen.SetOrientation(TuringSmartScreenRevisionA.Orientation.ReversePortrait, width, height);
                break;
            case ScreenOrientation.Landscape:
                screen.SetOrientation(TuringSmartScreenRevisionA.Orientation.Landscape, width, height);
                break;
            case ScreenOrientation.ReverseLandscape:
                screen.SetOrientation(TuringSmartScreenRevisionA.Orientation.ReverseLandscape, width, height);
                break;
        }
    }

    public IScreenBuffer CreateBuffer(int width, int height) => new TuringSmartScreenBufferA(width, height);

    public void DisplayBitmap(int x, int y, int width, int height, byte[] bitmap) => screen.DisplayBitmap(x, y, width, height, bitmap);
}

internal abstract class ScreenWrapperRevisionB : IScreen
{
    private readonly TuringSmartScreenRevisionB screen;

    protected ScreenWrapperRevisionB(TuringSmartScreenRevisionB screen)
    {
        this.screen = screen;
    }

    public void Dispose() => screen.Dispose();

    public void Reset()
    {
        // Do Nothing
    }

    public void Clear()
    {
        // TODO Emulation
    }

    public void ScreenOff()
    {
        // Emulation
        SetBrightness(0);
    }

    public void ScreenOn()
    {
        // Emulation
        SetBrightness(100);
    }

    public void SetBrightness(byte level) => screen.SetBrightness(CalcBrightness(level));

    protected abstract byte CalcBrightness(byte value);
    public void SetOrientation(ScreenOrientation orientation, int width, int height)
    {
        switch (orientation)
        {
            case ScreenOrientation.Portrait:
                screen.SetOrientation(TuringSmartScreenRevisionB.Orientation.Portrait, width, height);
                break;
            case ScreenOrientation.ReversePortrait:
                // TODO Emulation ?
                break;
            case ScreenOrientation.Landscape:
                screen.SetOrientation(TuringSmartScreenRevisionB.Orientation.Landscape, width, height);
                break;
            case ScreenOrientation.ReverseLandscape:
                // TODO Emulation ?
                break;
        }
    }

    public IScreenBuffer CreateBuffer(int width, int height) => new TuringSmartScreenBufferB(width, height);

    public void DisplayBitmap(int x, int y, int width, int height, byte[] bitmap) => screen.DisplayBitmap(x, y, width, height, bitmap);
}

internal sealed class ScreenWrapperRevisionB0 : ScreenWrapperRevisionB
{
    public ScreenWrapperRevisionB0(TuringSmartScreenRevisionB screen)
        : base(screen)
    {
    }

    protected override byte CalcBrightness(byte value) => value == 0 ? (byte)0 : (byte)1;
}

internal sealed class ScreenWrapperRevisionB1 : ScreenWrapperRevisionB
{
    public ScreenWrapperRevisionB1(TuringSmartScreenRevisionB screen)
        : base(screen)
    {
    }

    protected override byte CalcBrightness(byte value) => (byte)((float)value / 100 * 255);
}
