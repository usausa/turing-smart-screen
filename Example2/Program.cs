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
using var buffer1 = screen.CreateBufferFrom(bitmap1);
screen.DisplayBuffer(0, 0, buffer1);

using var bitmap2 = SKBitmap.Decode("test2-crop.png");
using var buffer2 = screen.CreateBufferFrom(bitmap2);

//test multiple partial updates sequentially
//test Y Height offset (should display from top USB corner to bottom)
var yOffset = 0;
while (screen.CanDisplayPartialBitmap() && yOffset < screen.Height - bitmap2.Height)
{
    screen.DisplayBuffer(0, yOffset, buffer2);
    yOffset += 10;
}

//test X Width offset (should display from left USB side to right)
var xOffset = 0;
while (screen.CanDisplayPartialBitmap() && xOffset < screen.Width - bitmap2.Width)
{
    screen.DisplayBuffer(xOffset, yOffset, buffer2);
    xOffset += 10;
}

Thread.Sleep(1000);
screen.Reset();
