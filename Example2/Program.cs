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

using var bitmap2 = SKBitmap.Decode("test2-crop.png");
using var buffer2 = screen.CreateBufferFrom(bitmap2);

screen.DisplayBuffer(0, 0, buffer1);

// loop
for (var y = 0; y <= screen.Height - bitmap2.Height; y += 2)
{
    screen.DisplayBuffer(0, y, buffer2);
}
for (var x = 0; x <= screen.Width - bitmap2.Width; x += 2)
{
    screen.DisplayBuffer(x, screen.Height - bitmap2.Height, buffer2);
}
for (var y = screen.Height - bitmap2.Height; y >= 0; y -= 2)
{
    screen.DisplayBuffer(screen.Width - bitmap2.Width, y, buffer2);
}
for (var x = screen.Width - bitmap2.Width; x >= 0; x -= 2)
{
    screen.DisplayBuffer(x, 0, buffer2);
}

screen.DisplayBuffer(0, 0, buffer1);

// corner
screen.DisplayBuffer(0, 0, buffer2);
screen.DisplayBuffer(0, screen.Height - bitmap2.Height, buffer2);
screen.DisplayBuffer(screen.Width - bitmap2.Width, 0, buffer2);
screen.DisplayBuffer(screen.Width - bitmap2.Width, screen.Height - bitmap2.Height, buffer2);
