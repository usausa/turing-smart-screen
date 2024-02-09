namespace TuringSmartScreenLib.Helpers.SkiaSharp;

using global::SkiaSharp;

public static partial class Extensions
{
    public static void ReadFrom(this IScreenBuffer buffer, SKBitmap bitmap) =>
        buffer.ReadFrom(bitmap, 0, 0, bitmap.Width, bitmap.Height);

    public static void ReadFrom(this IScreenBuffer buffer, SKBitmap bitmap, int sx, int sy, int sw, int sh)
    {
        if (buffer is TuringSmartScreenBufferA bufferA)
        {
            bufferA.ReadFrom(bitmap, sx, sy, sw, sh);
        }
        else if (buffer is TuringSmartScreenBufferB bufferB)
        {
            bufferB.ReadFrom(bitmap, sx, sy, sw, sh);
        }
        else if (buffer is TuringSmartScreenBufferC bufferC)
        {
            bufferC.ReadFrom(bitmap, sx, sy, sw, sh);
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
}
