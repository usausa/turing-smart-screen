namespace TuringSmartScreenLib.Helpers.SkiaSharp;

using global::SkiaSharp;

public static class Extensions
{
    public static void ReadFrom(this IScreenBuffer buffer, SKBitmap bitmap) =>
        buffer.ReadFrom(bitmap, 0, 0, bitmap.Width, bitmap.Height);

    public static void ReadFrom(this IScreenBuffer buffer, SKBitmap bitmap, int sx, int sy, int sw, int sh)
    {
        if (buffer is TuringSmartScreenBufferB bufferB)
        {
            bufferB.ReadFrom(bitmap, sx, sy, sw, sh);
        }
        else if (buffer is TuringSmartScreenBufferA bufferA)
        {
            bufferA.ReadFrom(bitmap, sx, sy, sw, sh);
        }
        else if (buffer is TuringSmartScreenBuffer5Inch buffer5)
        {
            buffer5.ReadFrom(bitmap, sx, sy, sw, sh);
        }
        else
        {
            for (var y = sy; y < sy + sh; y++)
            {
                for (var x = sx; x < sx + sw; x++)
                {
                    var color = bitmap.GetPixel(x, y);
                    buffer.SetPixel(x, y, color.Red, color.Green, color.Blue);
                }
            }
        }
    }

    public static void ReadFrom(this TuringSmartScreenBufferA buffer, SKBitmap bitmap) =>
        buffer.ReadFrom(bitmap, 0, 0, bitmap.Width, bitmap.Height);

    public static void ReadFrom(this TuringSmartScreenBufferA buffer, SKBitmap bitmap, int sx, int sy, int sw, int sh)
    {
        for (var y = sy; y < sy + sh; y++)
        {
            for (var x = sx; x < sx + sw; x++)
            {
                var color = bitmap.GetPixel(x, y);
                buffer.SetPixel(x, y, color.Red, color.Green, color.Blue);
            }
        }
    }

    public static bool IsFullScreen(int sx, int sy, int sw, int sh)
    {
        if (sx > 0 || sy > 0)
        {
            return false;
        }

        if (sw == TuringSmartScreen5Inch.WIDTH && sh == TuringSmartScreen5Inch.HEIGHT)
        {
            return true;
        }

        return sh == TuringSmartScreen5Inch.WIDTH && sw == TuringSmartScreen5Inch.HEIGHT;
    }

    public static void ReadFrom(this TuringSmartScreenBuffer5Inch buffer, SKBitmap bitmap, int sx, int sy, int sw, int sh)
    {
        using var memStream = new MemoryStream();
        using var wstream = new SKManagedWStream(memStream);
        buffer.SetRGB(sw, sh, bitmap.Bytes);
  
    }

    public static void ReadFrom(this TuringSmartScreenBufferB buffer, SKBitmap bitmap) =>
        buffer.ReadFrom(bitmap, 0, 0, bitmap.Width, bitmap.Height);

    public static void ReadFrom(this TuringSmartScreenBufferB buffer, SKBitmap bitmap, int sx, int sy, int sw, int sh)
    {
        for (var y = sy; y < sy + sh; y++)
        {
            for (var x = sx; x < sx + sw; x++)
            {
                var color = bitmap.GetPixel(x, y);
                buffer.SetPixel(x, y, color.Red, color.Green, color.Blue);
            }
        }
    }
}
