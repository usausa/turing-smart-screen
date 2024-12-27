namespace TuringSmartScreenLib;

internal sealed class ScreenWrapperRevisionC : ScreenBase
{
    private readonly TuringSmartScreenRevisionC screen;

    public ScreenWrapperRevisionC(TuringSmartScreenRevisionC screen)
        : base(screen.Width, screen.Height)
    {
        this.screen = screen;
    }

    public override void Dispose() => screen.Dispose();

    public override void Reset()
    {
        // Do Nothing
    }

    public override void Clear() => screen.Clear();

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

    public override void SetBrightness(byte level) => screen.SetBrightness(level);

    protected override bool SetOrientation(ScreenOrientation orientation) => true;

    public override IScreenBuffer CreateBuffer(int width, int height) => new ScreenBufferBgr888(width, height);

    public override bool DisplayBuffer(int x, int y, IScreenBuffer buffer) => screen.DisplayBitmap(x, y, ((ScreenBufferBgr888)buffer).Buffer, buffer.Width, buffer.Height, CalcRotateOption());

    private RotateOption CalcRotateOption() =>
        Orientation switch
        {
            ScreenOrientation.Landscape => RotateOption.Rotate90,
            ScreenOrientation.ReversePortrait => RotateOption.Rotate180,
            ScreenOrientation.ReverseLandscape => RotateOption.Rotate270,
            _ => RotateOption.None
        };
}
