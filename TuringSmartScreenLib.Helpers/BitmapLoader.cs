namespace TuringSmartScreenLib.Helpers;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public static class BitmapLoader
{
    [Obsolete("Use LoadForRevisionA")]
    public static byte[] Load(Stream stream, int left, int top, int width, int height) =>
        LoadForRevisionA(stream, left, top, width, height);

    public static byte[] LoadForRevisionA(Stream stream, int left, int top, int width, int height)
    {
        using var image = Image.Load<Rgb24>(stream);

        var bytes = new byte[width * height * 2];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var color = image[left + x, top + y];
                var rgb = ((color.R >> 3) << 11) | ((color.G >> 2) << 5) | (color.B >> 3);
                var offset = ((y * width) + x) * 2;
                bytes[offset] = (byte)(rgb & 0xFF);
                bytes[offset + 1] = (byte)((rgb >> 8) & 0xFF);
            }
        }

        return bytes;
    }

    public static byte[] LoadForRevisionB(Stream stream, int left, int top, int width, int height)
    {
        using var image = Image.Load<Rgb24>(stream);

        var bytes = new byte[width * height * 2];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var color = image[left + x, top + y];
                var rgb = ((color.R >> 3) << 11) | ((color.G >> 2) << 5) | (color.B >> 3);
                var offset = ((y * width) + x) * 2;
                bytes[offset] = (byte)((rgb >> 8) & 0xFF);
                bytes[offset + 1] = (byte)(rgb & 0xFF);
            }
        }

        return bytes;
    }
}
