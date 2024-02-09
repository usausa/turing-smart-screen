namespace TuringSmartScreenLib.Helpers.SkiaSharp;

using global::SkiaSharp;

public static partial class Extensions
{
    // RevisionC

    // TODO

    public static void ReadFrom(this TuringSmartScreenBufferC buffer, SKBitmap bitmap, int sw, int sh)
    {
        using var memStream = new MemoryStream();
        using var wStream = new SKManagedWStream(memStream);
        buffer.SetRGB(sw, sh, bitmap.Bytes);
    }
}
