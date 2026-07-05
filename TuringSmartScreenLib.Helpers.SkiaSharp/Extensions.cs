namespace TuringSmartScreenLib.Helpers.SkiaSharp;

using global::SkiaSharp;

public static class Extensions
{
    public static IScreenBuffer CreateBufferFrom(this IScreen screen, SKBitmap bitmap)
    {
        var buffer = screen.CreateBuffer(bitmap.Width, bitmap.Height);
        buffer.ReadFrom(bitmap);
        return buffer;
    }

    public static IScreenBuffer CreateBufferFrom(this IScreen screen, SKBitmap bitmap, int sx, int sy, int sw, int sh)
    {
        var buffer = screen.CreateBuffer(sw, sh);
        buffer.ReadFrom(bitmap, sx, sy, sw, sh);
        return buffer;
    }

    public static void ReadFrom(this IScreenBuffer buffer, SKBitmap bitmap) =>
        buffer.ReadFrom(bitmap, 0, 0, bitmap.Width, bitmap.Height);

    public static void ReadFrom(this IScreenBuffer buffer, SKBitmap bitmap, int sx, int sy, int sw, int sh)
    {
        var info = bitmap.Info;
        var pixels = bitmap.GetPixelSpan();

        int ro, go, bo;
        switch (info.ColorType)
        {
            case SKColorType.Bgra8888:
                bo = 0;
                go = 1;
                ro = 2;
                break;
            case SKColorType.Rgba8888:
            case SKColorType.Rgb888x:
                ro = 0;
                go = 1;
                bo = 2;
                break;
            default:
                ro = -1;
                go = -1;
                bo = -1;
                break;
        }

        if ((ro < 0) || pixels.IsEmpty || (info.AlphaType == SKAlphaType.Premul))
        {
            for (var y = 0; y < sh; y++)
            {
                for (var x = 0; x < sw; x++)
                {
                    var color = bitmap.GetPixel(x + sx, y + sy);
                    buffer.SetPixel(x, y, color.Red, color.Green, color.Blue);
                }
            }

            return;
        }

        var rowBytes = info.RowBytes;
        var bpp = info.BytesPerPixel;
        for (var y = 0; y < sh; y++)
        {
            var row = pixels.Slice(((y + sy) * rowBytes) + (sx * bpp), sw * bpp);
            for (var x = 0; x < sw; x++)
            {
                var p = x * bpp;
                buffer.SetPixel(x, y, row[p + ro], row[p + go], row[p + bo]);
            }
        }
    }
}
