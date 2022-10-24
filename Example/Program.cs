namespace Example;

using SkiaSharp;

using TuringSmartScreenLib;
using TuringSmartScreenLib.Helpers.SkiaSharp;

public static class Program
{
    private const int Width = 480;
    private const int Height = 320;

    private const int Margin = 2;

    private const int Digits = 6;

    // ReSharper disable FunctionNeverReturns
    public static void Main()
    {
        // Create screen
        using var screen = ScreenFactory.Create(ScreenType.RevisionB, "COM10");
        //using var screen = ScreenFactory.Create(ScreenType.RevisionA, "COM9");
        screen.SetBrightness(100);
        screen.SetOrientation(ScreenOrientation.Landscape);

        // Clear
        var clearBuffer = screen.CreateBuffer(Width, Height);
        clearBuffer.Clear(255, 255, 255);
        screen.DisplayBuffer(clearBuffer);

        // Paint
        using var paint = new SKPaint();
        paint.IsAntialias = true;
        paint.TextSize = 96;
        paint.Color = SKColors.Red;

        // Calc image size
        var imageWidth = 0;
        var imageHeight = 0;
        for (var i = 0; i < 10; i++)
        {
            var rect = default(SKRect);
            paint.MeasureText($"{i}", ref rect);

            imageWidth = Math.Max(imageWidth, (int)Math.Floor(rect.Width));
            imageHeight = Math.Max(imageHeight, (int)Math.Floor(rect.Height));
        }

        imageWidth += Margin * 2;
        imageHeight += Margin * 2;

        // Create digit image
        var digitImages = new IScreenBuffer[10];
        for (var i = 0; i < 10; i++)
        {
            using var bitmap = new SKBitmap(imageWidth, imageHeight);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.White);
            canvas.DrawText($"{i}", Margin, imageHeight - Margin, paint);
            canvas.Flush();

            var buffer = screen.CreateBuffer(imageWidth, imageHeight);
            buffer.ReadFrom(bitmap, 0, 0, imageWidth, imageHeight);
            digitImages[i] = buffer;
        }

        // Prepare display setting
        var baseX = (Width - (imageWidth * Digits)) / 2;
        var baseY = (Height / 2) - (imageHeight / 2);

        var previousValues = new int[Digits];
        for (var i = 0; i < previousValues.Length; i++)
        {
            previousValues[i] = Int32.MinValue;
        }

        // Display loop
        var max = Math.Pow(10, Digits);
        var counter = 0;
        while (true)
        {
            var value = counter;
            for (var i = Digits - 1; i >= 0; i--)
            {
                var number = value % 10;
                if (previousValues[i] != number)
                {
                    screen.DisplayBuffer(baseX + (imageWidth * i), baseY, digitImages[number]);
                    previousValues[i] = number;
                }

                value /= 10;
            }

            counter++;
            if (counter >= max)
            {
                counter = 0;
            }

            Thread.Sleep(50);
        }
    }
    // ReSharper restore FunctionNeverReturns
}
