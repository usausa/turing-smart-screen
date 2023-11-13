using SkiaSharp;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.IO;
using System.Net.NetworkInformation;

namespace Example;

using SkiaSharp;

using TuringSmartScreenLib;
using TuringSmartScreenLib.Helpers.SkiaSharp;
using static System.Runtime.InteropServices.JavaScript.JSType;

public static class Program
{
    private const int Margin = 2;

    private const int Digits = 6;

    // ReSharper disable FunctionNeverReturns
    public static void Main()
    {
        // Create screen
        using var screen = ScreenFactory.Create(ScreenType.Large5Inch, "COM9");
        //using var screen = ScreenFactory.Create(ScreenType.RevisionA, "COM9");
        screen.SetBrightness(100);
        screen.Orientation = ScreenOrientation.Landscape;

        // Clear
        var screenBuffer = screen.CreateBuffer();
        //screenBuffer.Clear(255, 255, 255);
        //screen.DisplayBuffer(screenBuffer);
        using var bitmap = SKBitmap.Decode("test1.png");
        screenBuffer.ReadFrom(bitmap);
        screen.DisplayBuffer(0, 0, screenBuffer);
        using var bitmap2 = SKBitmap.Decode("test2-crop.png");
        screenBuffer.ReadFrom(bitmap2);
        screen.DisplayBuffer(30, 50, screenBuffer);
    }
    // ReSharper restore FunctionNeverReturns
}
