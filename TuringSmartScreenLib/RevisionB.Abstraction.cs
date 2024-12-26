namespace TuringSmartScreenLib;

internal abstract class ScreenWrapperRevisionB : ScreenBase
{
    private readonly TuringSmartScreenRevisionB screen;

    protected ScreenWrapperRevisionB(TuringSmartScreenRevisionB screen)
        : base(screen.Width, screen.Height)
    {
        this.screen = screen;
    }

    public override void Dispose() => screen.Dispose();

    public override void Reset()
    {
        // Do Nothing
    }

    public override void Clear()
    {
        // TODO Emulation ?
    }

    public override void ScreenOff()
    {
        // Emulation
        SetBrightness(0);
    }

    public override void ScreenOn()
    {
        // Emulation
        SetBrightness(100);
    }

    public override void SetBrightness(byte level) => screen.SetBrightness(CalcBrightness(level));

    protected abstract byte CalcBrightness(byte value);

    protected override bool SetOrientation(ScreenOrientation orientation)
    {
        switch (orientation)
        {
            case ScreenOrientation.Portrait:
                screen.SetOrientation(TuringSmartScreenRevisionB.Orientation.Portrait);
                return true;
            case ScreenOrientation.ReversePortrait:
                screen.SetOrientation(TuringSmartScreenRevisionB.Orientation.Portrait);
                return true;
            case ScreenOrientation.Landscape:
                screen.SetOrientation(TuringSmartScreenRevisionB.Orientation.Landscape);
                return true;
            case ScreenOrientation.ReverseLandscape:
                screen.SetOrientation(TuringSmartScreenRevisionB.Orientation.Landscape);
                return true;
        }

        return false;
    }

    public override IScreenBuffer CreateBuffer(int width, int height) => new ScreenBufferBgr353(width, height);

    // TODO rotate
    public override bool DisplayBuffer(int x, int y, IScreenBuffer buffer) => screen.DisplayBitmap(x, y, ((ScreenBufferBgr353)buffer).Buffer, buffer.Width, buffer.Height);
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
