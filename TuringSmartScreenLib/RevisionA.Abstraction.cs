namespace TuringSmartScreenLib;

internal sealed class ScreenWrapperRevisionA : ScreenBase
{
    private readonly TuringSmartScreenRevisionA screen;

    public ScreenWrapperRevisionA(TuringSmartScreenRevisionA screen)
        : base(screen.Width, screen.Height)
    {
        this.screen = screen;
    }

    public override void Dispose() => screen.Dispose();

    public override void Reset() => screen.Reset();

    public override void Clear() => screen.Clear();

    public override void ScreenOff() => screen.ScreenOff();

    public override void ScreenOn() => screen.ScreenOn();

    public override void SetBrightness(byte level) => screen.SetBrightness(255 - (byte)((float)level / 100 * 255));

    protected override bool SetOrientation(ScreenOrientation orientation)
    {
        switch (orientation)
        {
            case ScreenOrientation.Portrait:
                screen.SetOrientation(TuringSmartScreenRevisionA.Orientation.Portrait);
                return true;
            case ScreenOrientation.ReversePortrait:
                screen.SetOrientation(TuringSmartScreenRevisionA.Orientation.ReversePortrait);
                return true;
            case ScreenOrientation.Landscape:
                screen.SetOrientation(TuringSmartScreenRevisionA.Orientation.Landscape);
                return true;
            case ScreenOrientation.ReverseLandscape:
                screen.SetOrientation(TuringSmartScreenRevisionA.Orientation.ReverseLandscape);
                return true;
        }

        return false;
    }

    public override IScreenBuffer CreateBuffer(int width, int height) => new TuringSmartScreenBufferA(width, height);

    public override void DisplayBuffer(int x, int y, IScreenBuffer buffer) => screen.DisplayBitmap(x, y, ((TuringSmartScreenBufferA)buffer).Buffer, buffer.Width, buffer.Height);
    public override bool CanDisplayPartialBitmap() => true;
}
