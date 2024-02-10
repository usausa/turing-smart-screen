//namespace TuringSmartScreenLib;

// TODO
//internal sealed class ScreenWrapperC2 : ScreenBase
//{
//    private readonly TuringSmartScreenRevisionC screen;

//    public ScreenWrapperC(TuringSmartScreenRevisionC screen, int width, int height)
//        : base(width, height)
//    {
//        this.screen = screen;
//    }

//    public override void Dispose() => screen.Dispose();

//    public override void Reset() => screen.Reset();

//    public override void Clear() => screen.Clear();

//    public override void ScreenOff() => screen.ScreenOff();

//    public override void ScreenOn() => screen.ScreenOn();

//    public override void SetBrightness(byte level) => screen.SetBrightness(level);

//    protected override bool SetOrientation(ScreenOrientation orientation)
//    {
//        switch (orientation)
//        {
//            case ScreenOrientation.Portrait:
//                screen.SetOrientation(TuringSmartScreenRevisionC.Orientation.Portrait, Width, Height);
//                return true;
//            case ScreenOrientation.ReversePortrait:
//                screen.SetOrientation(TuringSmartScreenRevisionC.Orientation.ReversePortrait, Width, Height);
//                return true;
//            case ScreenOrientation.Landscape:
//                screen.SetOrientation(TuringSmartScreenRevisionC.Orientation.Landscape, Width, Height);
//                return true;
//            case ScreenOrientation.ReverseLandscape:
//                screen.SetOrientation(TuringSmartScreenRevisionC.Orientation.ReverseLandscape, Width, Height);
//                return true;
//        }

//        return false;
//    }

//    public override IScreenBuffer CreateBuffer(int width, int height) => new TuringSmartScreenBufferC();

//    public override void DisplayBitmap(int x, int y, int width, int height, IScreenBuffer buffer) => screen.DisplayBitmap(x, y, width, height, buffer);
//}
