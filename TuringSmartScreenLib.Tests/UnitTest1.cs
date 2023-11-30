namespace TuringSmartScreenLib.Tests;

using System.Text;
using SkiaSharp;

/// <summary>
/// Unit test to compare the output of the Python code with the C# code.
/// </summary>
public class UnitTest1
{
    /// <summary>
    /// Code translated from Python see source code:
    /// https://github.com/mathoudebine/turing-smart-screen-python/issues/90
    /// </summary>
    private static (string, string) GeneratePartialUpdateFromBufferPython(int height, int width, int x, int y, byte[,,] image)
    {
        var msg = new StringBuilder();

        for (var h = 0; h < height; h++)
        {
            msg.Append($"{((x + h) * 800) + y:X6}{width:X4}");
            for (var w = 0; w < width; w++)
            {
                msg.Append($"{image[h, w, 0]:X2}{image[h, w, 1]:X2}{image[h, w, 2]:X2}");
            }
        }

        var updSize = $"{((msg.Length / 2) + 2):X4}";

        if (msg.Length > 500)
        {
            var chunks = Enumerable.Range(0, msg.Length)
                .Where(i => i % 498 == 0)
                .Select(i => msg.ToString().Substring(i, Math.Min(498, msg.Length - i))).ToList();
            var newStr = string.Join("00", chunks);
            msg = new StringBuilder(newStr);
        }

        msg.Append("ef69");
        return (msg.ToString(), updSize);
    }

    [Fact]
    public void TestConvertBufferInto3DArray()
    {
        using var bitmap = SKBitmap.Decode("test2-crop.png");
        var channelCount = 3; // Assuming RGB format
        var tdArray = ConvertTo3DArray(bitmap.Bytes, bitmap.Width, bitmap.Height, channelCount);
        var strUpdate = GeneratePartialUpdateFromBufferPython(bitmap.Height, bitmap.Width, 0, 0, tdArray);
    }

    [Fact]
    public void Test2DArray()
    {
        var width = 2;
        var height = 3;
        var channelCount = 3; // Assuming RGB format

        var oneDimensionalArray = Enumerable.Range(1, (width * height * channelCount) + 1).SelectMany(BitConverter.GetBytes).ToArray();

        // Define the dimensions for the three-dimensional array
        var tdArray = ConvertTo3DArray(oneDimensionalArray, width, height, channelCount);
        var (strData, updMsg) = GeneratePartialUpdateFromBufferPython(height, width, 0, 0, tdArray);

        var (buffer2, updData) = TuringSmartScreenRevisionC.GeneratePartialUpdateFromBuffer(height, width, 0, 0, oneDimensionalArray, 3);
        var toHex = Convert.ToHexString(buffer2);
        Assert.Equal(strData, toHex, ignoreCase: true);
        Assert.Equal(updMsg, Convert.ToHexString(updData), ignoreCase: true);
    }

    [Fact]
    public void Test2DArrayLong()
    {
        var width = 20;
        var height = 30;
        var channelCount = 3; // Assuming RGB format

        var oneDimensionalArray = Enumerable.Range(1, (width * height * channelCount) + 1).SelectMany(BitConverter.GetBytes).ToArray();

        // Define the dimensions for the three-dimensional array
        var tdArray = ConvertTo3DArray(oneDimensionalArray, width, height, channelCount);
        var (strData, updMsg) = GeneratePartialUpdateFromBufferPython(height, width, 0, 0, tdArray);

        var (buffer2, updData) = TuringSmartScreenRevisionC.GeneratePartialUpdateFromBuffer(height, width, 0, 0, oneDimensionalArray, 3);
        var toHex = Convert.ToHexString(buffer2);
        Assert.Equal(strData, toHex, ignoreCase: true);
        Assert.Equal(updMsg, Convert.ToHexString(updData), ignoreCase: true);
    }

    static byte[,,] ConvertTo3DArray(byte[] pixelData, int width, int height, int channelCount)
    {
        var threeDimensionalArray = new byte[height, width, channelCount];

        for (var h = 0; h < height; h++)
        {
            for (var w = 0; w < width; w++)
            {
                for (var c = 0; c < channelCount; c++)
                {
                    var index = (((h * width) + w) * channelCount) + c;
                    threeDimensionalArray[h, w, c] = pixelData[index];
                }
            }
        }

        return threeDimensionalArray;
    }
}
