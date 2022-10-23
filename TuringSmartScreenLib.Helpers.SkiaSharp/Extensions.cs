namespace TuringSmartScreenLib.Helpers.SkiaSharp;

using global::SkiaSharp;

public static class Extensions
{
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
