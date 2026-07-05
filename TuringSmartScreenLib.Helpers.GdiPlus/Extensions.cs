namespace TuringSmartScreenLib.Helpers.GdiPlus;

using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

[SupportedOSPlatform("windows")]
public static class Extensions
{
    public static IScreenBuffer CreateBufferFrom(this IScreen screen, Bitmap bitmap)
    {
        var buffer = screen.CreateBuffer(bitmap.Width, bitmap.Height);
        buffer.ReadFrom(bitmap);
        return buffer;
    }

    public static IScreenBuffer CreateBufferFrom(this IScreen screen, Bitmap bitmap, int sx, int sy, int sw, int sh)
    {
        var buffer = screen.CreateBuffer(sw, sh);
        buffer.ReadFrom(bitmap, sx, sy, sw, sh);
        return buffer;
    }

    public static void ReadFrom(this IScreenBuffer buffer, Bitmap bitmap) =>
       buffer.ReadFrom(bitmap, 0, 0, bitmap.Width, bitmap.Height);

    public static void ReadFrom(this IScreenBuffer buffer, Bitmap bitmap, int sx, int sy, int sw, int sh)
    {
        var data = bitmap.LockBits(new Rectangle(sx, sy, sw, sh), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var stride = data.Stride;
            var scan0 = data.Scan0;
            var row = new byte[sw * 4];
            for (var y = 0; y < sh; y++)
            {
                Marshal.Copy(scan0 + (y * stride), row, 0, row.Length);
                for (var x = 0; x < sw; x++)
                {
                    var p = x * 4;
                    // Format32bppArgb is laid out as B, G, R, A in memory (little-endian).
                    buffer.SetPixel(x, y, row[p + 2], row[p + 1], row[p]);
                }
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }
}
