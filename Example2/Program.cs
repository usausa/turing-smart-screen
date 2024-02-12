// ReSharper disable RedundantArgumentDefaultValue
using SkiaSharp;

using TuringSmartScreenLib;
using TuringSmartScreenLib.Helpers.SkiaSharp;

using var screen = new TuringSmartScreenRevisionC2("COM10");
screen.Open();
for (var i = 100; i >= 0; i--)
{
    screen.SetBrightness(i);
    Thread.Sleep(10);
}
screen.SetBrightness(100);

screen.Clear(0xff, 0, 0);
screen.Clear(0, 0xff, 0);
screen.Clear(0, 0, 0xff);

screen.Clear();

using var bitmap1 = SKBitmap.Decode("test1.png");
using var buffer1 = new TuringSmartScreenBufferC2(800, 480);
buffer1.ReadFrom(bitmap1);
screen.DisplayBitmap(0, 0, 800, 480, buffer1.RawBuffer);

using var bitmap2 = SKBitmap.Decode("test2-crop.png");
using var buffer2 = new TuringSmartScreenBufferC2(277, 75);
buffer2.ReadFrom(bitmap2);
screen.DisplayBitmap(30, 50, buffer2.Width, buffer2.Height, buffer2.RawBuffer);
