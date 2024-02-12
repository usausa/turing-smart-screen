// ReSharper disable RedundantArgumentDefaultValue
using SkiaSharp;

using TuringSmartScreenLib;
using TuringSmartScreenLib.Helpers.SkiaSharp;

// Create screen
using var screen = ScreenFactory.Create(ScreenType.RevisionC, "COM10");

for (var i = 100; i >= 0; i--)
{
    screen.SetBrightness((byte)i);
    Thread.Sleep(10);
}
screen.SetBrightness(100);

screen.Clear();

using var bitmap1 = SKBitmap.Decode("test1.png");
using var buffer1 = new TuringSmartScreenBufferC(800, 480);
buffer1.ReadFrom(bitmap1);
screen.DisplayBitmap(0, 0, 800, 480, buffer1);

using var bitmap2 = SKBitmap.Decode("test2-crop.png");
using var buffer2 = new TuringSmartScreenBufferC(277, 75);
buffer2.ReadFrom(bitmap2);
screen.DisplayBitmap(30, 50, buffer2.Width, buffer2.Height, buffer2);
