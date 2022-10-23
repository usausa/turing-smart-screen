namespace Example;

using SkiaSharp;

using TuringSmartScreenLib;
using TuringSmartScreenLib.Helpers.SkiaSharp;

public static class Program
{
    private const int Width = 480;
    private const int Height = 320;

    private const int Margin = 2;

    private const int Digits = 4;

    // ReSharper disable FunctionNeverReturns
    public static void Main()
    {
        // B
        using var screen = new TuringSmartScreenRevisionB("COM10");
        screen.Open();
        screen.SetBrightness(255);
        screen.SetOrientation(TuringSmartScreenRevisionB.Orientation.Landscape);

        screen.DisplayBitmap(0, 0, Width, Height, new byte[Width * Height * 2]);

        using var paint = new SKPaint();
        paint.IsAntialias = true;
        paint.TextSize = 96;
        paint.Color = SKColors.Red;

        // 領域計算
        var bufferWidth = 0;
        var bufferHeight = 0;
        for (var i = 0; i < 10; i++)
        {
            var text = $"{i}";

            var rect = default(SKRect);
            paint.MeasureText(text, ref rect);

            bufferWidth = Math.Max(bufferWidth, (int)Math.Floor(rect.Width));
            bufferHeight = Math.Max(bufferHeight, (int)Math.Floor(rect.Height));
        }

        bufferWidth += Margin * 2;
        bufferHeight += Margin * 2;

        // 元イメージ作成
        var numbers = new TuringSmartScreenBufferB[10];
        for (var i = 0; i < 10; i++)
        {
            using var bitmap = new SKBitmap(bufferWidth, bufferHeight);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.White);

            var text = $"{i}";

            var rect = default(SKRect);
            paint.MeasureText(text, ref rect);

            canvas.DrawText(text, Margin, bufferHeight - Margin, paint);
            canvas.Flush();

            var buffer = new TuringSmartScreenBufferB(bufferWidth, bufferHeight);
            buffer.ReadFrom(bitmap, 0, 0, bufferWidth, bufferHeight);
            numbers[i] = buffer;
        }

        var baseX = (Width - (bufferWidth * Digits)) / 2;
        var baseY = (Height / 2) - (bufferHeight / 2);

        var previousValues = new int[Digits];
        for (var i = 0; i < previousValues.Length; i++)
        {
            previousValues[i] = Int32.MinValue;
        }

        var counter = 0;
        while (true)
        {
            var value = counter;
            for (var i = Digits - 1; i >= 0; i--)
            {
                var v = value % 10;
                if (previousValues[i] != v)
                {
                    screen.DisplayBitmap(baseX + (bufferWidth * i), baseY, bufferWidth, bufferHeight, numbers[v].Buffer);
                    previousValues[i] = v;
                }

                value /= 10;
            }

            counter++;
            if (counter > 9999)
            {
                counter = 0;
            }
        }
    }
    // ReSharper restore FunctionNeverReturns
}
