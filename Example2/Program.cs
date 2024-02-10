namespace Example2;

using SkiaSharp;

using TuringSmartScreenLib;
using TuringSmartScreenLib.Helpers.SkiaSharp;

public static class Program
{
    // ReSharper disable FunctionNeverReturns
    public static void Main()
    {
        // Create screen
        using var screen = ScreenFactory.Create(ScreenType.RevisionC, "COM10");
        screen.SetBrightness(100);
        screen.Orientation = ScreenOrientation.Landscape;

        // Clear
        using var screenBuffer = screen.CreateBuffer();
        // screenBuffer.Clear(255, 255, 255);
        // screen.DisplayBuffer(screenBuffer);
        using var bitmap = SKBitmap.Decode("test1.png");
        screenBuffer.ReadFrom(bitmap);
        screen.DisplayBuffer(0, 0, screenBuffer);
        using var bitmap2 = SKBitmap.Decode("test2-crop.png");
        screenBuffer.ReadFrom(bitmap2);
        screen.DisplayBuffer(30, 50, screenBuffer);
    }
    // ReSharper restore FunctionNeverReturns
}
