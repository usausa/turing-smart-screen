namespace TuringSmartScreenLib.Tests;

using SkiaSharp;
using System.Text;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        //int height = /* assign your height value here */;
        //int width = /* assign your width value here */;
        //int x = /* assign your x value here */;
        //int y = /* assign your y value here */;
        //byte[][][] image = /* assign your image array here */;
        //var testData = new byte[][][] { };

        //GeneratePartialUpdateFromBuffer(height, width, x, y, image);
    }

    /// <summary>
    /// Code translated from Python
    /// https://github.com/mathoudebine/turing-smart-screen-python/issues/90
    /// </summary>
    /// <param name="height"></param>
    /// <param name="width"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="image"></param>
    private static string GeneratePartialUpdateFromBufferPython(int height, int width, int x, int y, byte[,,] image)
    {
        StringBuilder msg = new StringBuilder();

        for (int h = 0; h < height; h++)
        {
            msg.Append($"{((x + h) * 800) + y:X6}{width:X4}");
            for (int w = 0; w < width; w++)
            {
                msg.Append($"{image[h, w, 0]:X2}{image[h, w, 1]:X2}{image[h, w, 2]:X2}");
            }
        }

        string UPD_Size = $"{(int)((msg.Length / 2) + 2):X4}";

        if (msg.Length > 500)
        {
            msg = new StringBuilder(string.Join("00", Enumerable.Range(0, msg.Length)
                .Where(i => i % 498 == 0)
                .Select(i => msg.ToString().Substring(i, Math.Min(498, msg.Length - i)))));
        }

        msg.Append("ef69");
        return msg.ToString();
    }

    private static byte[] ConvertAndPad(int number, int fixedLength)
    {
        byte[] byteArray = BitConverter.GetBytes(number);
        // Apply zero padding if necessary
        Array.Resize(ref byteArray, fixedLength);
        return byteArray;
    }

    /// <summary>
    /// Code translated from Python
    /// https://github.com/mathoudebine/turing-smart-screen-python/issues/90
    /// </summary>
    /// <param name="height"></param>
    /// <param name="width"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="image"></param>
    private static byte[] GeneratePartialUpdateFromBuffer(int height, int width, int x, int y, byte[,,] image)
    {
        var data = new List<byte>();

        for (int h = 0; h < height; h++)
        {
            data.AddRange(ConvertAndPad(((x + h) * 800) + y, 6));
            data.AddRange(ConvertAndPad(width, 4));
            for (int w = 0; w < width; w++)
            {
                data.Add(image[h, w, 0]);
                data.Add(image[h, w, 1]);
                data.Add(image[h, w, 2]);
            }
        }

        return data.ToArray();

        //string UPD_Size = $"{(int)((msg.Length / 2) + 2):X4}";

        //if (msg.Length > 500)
        //{
        //    msg = new StringBuilder(string.Join("00", Enumerable.Range(0, msg.Length)
        //        .Where(i => i % 498 == 0)
        //        .Select(i => msg.ToString().Substring(i, Math.Min(498, msg.Length - i)))));
        //}

        //msg.Append("ef69");
        //return msg.ToString();
    }


    [Fact]
    public void TestConvertBufferInto3DArray()
    {
        using var bitmap = SKBitmap.Decode("test2-crop.png");
        int channelCount = 3; // Assuming RGB format
        var tdArray = ConvertTo3DArray(bitmap.Bytes, bitmap.Width, bitmap.Height, channelCount);
        var strUpdate = GeneratePartialUpdateFromBufferPython(bitmap.Height, bitmap.Width, 0, 0, tdArray);
    }

    [Fact]
    public void Test2DArray()
    {

        byte[] oneDimensionalArray = Enumerable.Range(1, 19).SelectMany(BitConverter.GetBytes).ToArray();

        // Define the dimensions for the three-dimensional array
        int width = 2;
        int height = 3;
        int channelCount = 3; // Assuming RGB format
        var tdArray = ConvertTo3DArray(oneDimensionalArray, width, height, channelCount);
        var strUpdate = GeneratePartialUpdateFromBufferPython(height, width, 0, 0, tdArray);
        var data = Convert.FromHexString(strUpdate);

        var buffer2 = GeneratePartialUpdateFromBuffer(height, width, 0, 0, tdArray);

    }

    static byte[,,] ConvertTo3DArray(byte[] pixelData, int width, int height, int channelCount)
    {
        byte[,,] threeDimensionalArray = new byte[height, width, channelCount];

        for (int h = 0; h < height; h++)
        {
            for (int w = 0; w < width; w++)
            {
                for (int c = 0; c < channelCount; c++)
                {
                    int index = (h * width + w) * channelCount + c;
                    threeDimensionalArray[h, w, c] = pixelData[index];
                }
            }
        }

        return threeDimensionalArray;
    }
}
