namespace TuringSmartScreenLib;

internal sealed class ScreenWrapperRevisionC : ScreenBase
{
    private readonly TuringSmartScreenRevisionC screen;

    public ScreenWrapperRevisionC(TuringSmartScreenRevisionC screen)
        : base(TuringSmartScreenRevisionC.Width, TuringSmartScreenRevisionC.Height)
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

    protected override bool SetOrientation(ScreenOrientation orientation)
    {
        // TODO Emulation ?
        return false;
    }

    public override IScreenBuffer CreateBuffer(int width, int height) => new TuringSmartScreenBufferC(width, height);

    public override void DisplayBitmap(int x, int y, int width, int height, IScreenBuffer buffer) => screen.DisplayBitmap(x, y, width, height, ((TuringSmartScreenBufferC)buffer).Buffer);
}
