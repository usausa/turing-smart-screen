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

    public override void Reset() => screen.Reset();

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

    public override void DisplayBuffer(int x, int y, IScreenBuffer buffer) => screen.DisplayBitmap(x, y, ((TuringSmartScreenBufferC)buffer).Buffer, buffer.Width, buffer.Height);

    public override bool CanDisplayPartialBitmap() => screen.CanDisplayPartialBitmap;
}
