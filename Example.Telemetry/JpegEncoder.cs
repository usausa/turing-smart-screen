namespace Example.Telemetry;

using SkiaSharp;

internal static class JpegEncoder
{
    public static byte[] Encode(SKImage source, int quarterTurns, int maxSize)
    {
        var swap = (quarterTurns % 2) != 0;
        var width = swap ? source.Height : source.Width;
        var height = swap ? source.Width : source.Height;

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        canvas.Translate(width / 2f, height / 2f);
        canvas.RotateDegrees(quarterTurns * 90);
        canvas.Translate(-source.Width / 2f, -source.Height / 2f);
        canvas.DrawImage(source, 0, 0, SKSamplingOptions.Default);
        using var image = surface.Snapshot();

        var quality = 90;
        while (true)
        {
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
            if ((data.Size < maxSize) || (quality <= 50))
            {
                return data.ToArray();
            }

            quality -= 5;
        }
    }
}
