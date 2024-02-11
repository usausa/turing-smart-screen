using SkiaSharp;

using TuringSmartScreenLib;
using TuringSmartScreenLib.Helpers.SkiaSharp;

using var screen = new TuringSmartScreenRevisionC2("COM10");
screen.Open();
//for (var i = 100; i >= 0; i--)
//{
//    screen.SetBrightness(i);
//    Thread.Sleep(10);
//}
//screen.SetBrightness(100);

//screen.Clear(0xff, 0, 0);
//screen.Clear(0, 0xff, 0);
//screen.Clear(0, 0, 0xff);

screen.Clear();

using var bitmap = SKBitmap.Decode("test1.png");
using var buffer = new TuringSmartScreenBufferC2(800, 480);
buffer.ReadFrom(bitmap);
screen.DisplayBitmap(0, 0, 800, 480, buffer.RawBuffer);

//using SkiaSharp;

//using TuringSmartScreenLib;
//using TuringSmartScreenLib.Helpers.SkiaSharp;

//// Create screen
//using var screen = ScreenFactory.Create(ScreenType.RevisionC, "COM10");
//screen.Orientation = ScreenOrientation.Landscape;

//// Clear
//using var screenBuffer = screen.CreateBuffer();
//// screenBuffer.Clear(255, 255, 255);
//// screen.DisplayBuffer(screenBuffer);
//using var bitmap = SKBitmap.Decode("test1.png");
//screenBuffer.ReadFrom(bitmap);
//screen.DisplayBuffer(0, 0, screenBuffer);
//using var bitmap2 = SKBitmap.Decode("test2-crop.png");
//screenBuffer.ReadFrom(bitmap2);
//screen.DisplayBuffer(30, 50, screenBuffer);
