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
        else if (buffer is TuringSmartScreenBufferC buffer5)
        {
            // TODO
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

    // RevisionA

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

    // RevisionB

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

    // RevisionC

    // TODO

    public static void ReadFrom(this TuringSmartScreenBufferC buffer, SKBitmap bitmap, int sw, int sh)
    {
        using var memStream = new MemoryStream();
        using var wStream = new SKManagedWStream(memStream);
        buffer.SetRGB(sw, sh, bitmap.Bytes);
    }
}
