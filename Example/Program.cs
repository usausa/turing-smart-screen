namespace Example;

using SkiaSharp;

using TuringSmartScreenLib;
using TuringSmartScreenLib.Helpers.SkiaSharp;

internal static class Program
{
    private const int Margin = 2;

    private const int Digits = 6;

    // ReSharper disable FunctionNeverReturns
    public static void Main()
    {
        // Create screen
        using var screen = ScreenFactory.Create(ScreenType.RevisionB, "COM9");

        screen.SetBrightness(100);
        screen.Orientation = ScreenOrientation.ReverseLandscape;

        screen.Clear();

        // Clear
        using var clearBuffer = screen.CreateBuffer();
        clearBuffer.Clear(255, 255, 255);
        screen.DisplayBuffer(clearBuffer);

        // Paint
        using var paint = new SKPaint();
        paint.IsAntialias = true;
        paint.Color = SKColors.Red;
        using var font = new SKFont();
        font.Size = 96;

        // Calc image size
        var imageWidth = 0;
        var imageHeight = 0;
        for (var i = 0; i < 10; i++)
        {
            font.MeasureText($"{i}", out var rect);
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
            canvas.DrawText($"{i}", Margin, imageHeight - Margin, font, paint);
            canvas.Flush();

            var buffer = screen.CreateBuffer(imageWidth, imageHeight);
            buffer.ReadFrom(bitmap, 0, 0, imageWidth, imageHeight);
            digitImages[i] = buffer;
        }

        // Prepare display setting
        var baseX = (screen.Width - (imageWidth * Digits)) / 2;
        var baseY = (screen.Height / 2) - (imageHeight / 2);

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
