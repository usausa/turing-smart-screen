namespace TuringSmartScreenLib;

using System;
using System.Buffers;
using System.IO.Ports;

public sealed class TuringSmartScreenRevisionC2 : IDisposable
{
    private const int WriteSize = 250;
    private const int ReadSize = 1024;
    private const int ReadHelloSize = 23;

    private static readonly byte[] CommandHello = [0x01, 0xef, 0x69, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0xc5, 0xd3];
    private static readonly byte[] CommandSetBrightness = [0x7b, 0xef, 0x69, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00];

    public enum Orientation : byte
    {
        Portrait = 0,
        ReversePortrait = 1,
        Landscape = 2,
        ReverseLandscape = 3
    }

    private readonly SerialPort port;

    private byte[] writeBuffer;

    private byte[] readBuffer;

    private int writeOffset;

    public TuringSmartScreenRevisionC2(string name)
    {
        port = new SerialPort(name)
        {
            DtrEnable = true,
            RtsEnable = true,
            ReadTimeout = 1000,
            WriteTimeout = 1000,
            BaudRate = 115200,
            StopBits = StopBits.One,
            Parity = Parity.None
        };
        writeBuffer = ArrayPool<byte>.Shared.Rent(WriteSize);
        readBuffer = ArrayPool<byte>.Shared.Rent(ReadSize);
    }

    public void Dispose()
    {
        Close();

        if (writeBuffer.Length > 0)
        {
            ArrayPool<byte>.Shared.Return(writeBuffer);
            writeBuffer = [];
        }
        if (readBuffer.Length > 0)
        {
            ArrayPool<byte>.Shared.Return(readBuffer);
            readBuffer = [];
        }
    }

    public void Close()
    {
        if (port.IsOpen)
        {
            port.Close();
        }
    }

    public void Open()
    {
        port.Open();
        port.DiscardInBuffer();
        port.DiscardOutBuffer();

        Write(CommandHello);
        Flush();

        var response = ReadResponse(ReadHelloSize);
        if ((response.Length != ReadHelloSize) || !response[..9].SequenceEqual("chs_5inch"u8))
        {
            throw new IOException($"Unknown response. response=[{Convert.ToHexString(response)}]");
        }
    }

    private ReadOnlySpan<byte> ReadResponse(int length)
    {
        var offset = 0;
        while (offset < length)
        {
            var read = port.Read(readBuffer, offset, length - offset);
            if (read <= 0)
            {
                break;
            }

            offset += read;
        }

        return readBuffer.AsSpan(0, offset);
    }

    private void Write(ReadOnlySpan<byte> values, byte pad = 0x00)
    {
        while (writeOffset + values.Length > WriteSize - 1)
        {
            var block = values[..(WriteSize - 1 - writeOffset)];
            block.CopyTo(writeBuffer.AsSpan(writeOffset, block.Length));
            writeOffset += block.Length;

            FlushInternal(pad);

            values = values[(WriteSize - 1)..];
        }

        if (values.Length > 0)
        {
            values.CopyTo(writeBuffer);
            writeOffset = values.Length;
        }
    }

    private void Write(byte value, byte pad = 0x00)
    {
        if (writeOffset + 1 > WriteSize - 1)
        {
            FlushInternal(pad);
        }

        writeBuffer[writeOffset] = value;
        writeOffset++;
    }

    private void Flush(byte pad = 0x00)
    {
        if (writeOffset > 0)
        {
            FlushInternal(pad);
        }
    }

    private void FlushInternal(byte pad)
    {
        writeBuffer.AsSpan(writeOffset, WriteSize - writeOffset).Fill(pad);
        port.Write(writeBuffer, 0, WriteSize);
        writeOffset = 0;
    }

    // TODO Clear

    public void SetBrightness(int level)
    {
        Write(CommandSetBrightness);
        Write((byte)level);
        Flush();
    }

    //        var cmd = new List<byte>
    //        {
    //            0x7b,
    //            0xef,
    //            0x69,
    //            0x00,
    //            0x00,
    //            0x00,
    //            0x01,
    //            0x00,
    //            0x00,
    //            0x00,
    //            (byte)level
    //        };
    //        WriteCommand(cmd.ToArray());
    //    }

    // TODO DisplayBitmap

    //    public void Reset() => WriteCommand(RESTART);

    //    public void DisplayBitmap(int x, int y, int width, int height, IScreenBuffer buffer)
    //    {
    //        var cBuffer = (TuringSmartScreenBufferC)buffer;
    //        if (cBuffer.IsEmpty())
    //        {
    //            ClearScreen();
    //        }
    //        else
    //        {
    //            var isFullScreen = height == HEIGHT && width == WIDTH;
    //            //var isRotated = width == HEIGHT && height == WIDTH;
    //            if (!isFullScreen)
    //            {
    //                DisplayPartialImage(x, y, width, height, cBuffer);
    //                WriteCommand(QUERY_STATUS);
    //                var resp = ReadResponse();
    //                if (resp?.Contains("needReSend:1", StringComparison.InvariantCulture) ?? false)
    //                {
    //                    DisplayPartialImage(x, y, width, height, cBuffer);
    //                    WriteCommand(QUERY_STATUS);
    //                }
    //            }
    //            else
    //            {
    //                if (x != 0 || y != 0 || width != WIDTH || height != HEIGHT)
    //                {
    //                    throw new InvalidOperationException("Invalid parameters for full screen image");
    //                }
    //                WriteCommand(START_DISPLAY_BITMAP, 0x2c);
    //                WriteCommand(DISPLAY_BITMAP);
    //                var blockSize = 249;
    //                var currentPosition = 0;
    //                while (currentPosition < cBuffer.Length)
    //                {
    //                    var block = cBuffer.ImgBuffer.Skip(currentPosition).Take(blockSize).ToArray();
    //                    WriteCommand(block);
    //                    currentPosition += blockSize;
    //                }
    //                WriteCommand(PRE_UPDATE_BITMAP);
    //                ReadResponse();
    //                WriteCommand(QUERY_STATUS);
    //                ReadResponse();
    //            }
    //        }
    //    }

    //private static byte[] ConvertAndPad(int number, int fixedLength)
    //{
    //    var byteArray = BitConverter.GetBytes(number);
    //    // Apply zero padding if necessary
    //    Array.Resize(ref byteArray, fixedLength);
    //    Array.Reverse(byteArray);
    //    return byteArray;
    //}

    //internal static (byte[] Data, byte[] UpdateSize) GeneratePartialUpdateFromBuffer(int height, int width, int x, int y, byte[] image, int channelCount = 4)
    //{
    //    var data = new List<byte>();

    //    for (var h = 0; h < height; h++)
    //    {
    //        data.AddRange(ConvertAndPad(((x + h) * 800) + y, 3));
    //        data.AddRange(ConvertAndPad(width, 2));
    //        for (var w = 0; w < width; w++)
    //        {
    //            var indexR = ((h * width) + w) * channelCount;
    //            data.Add(image[indexR]);
    //            var indexG = (((h * width) + w) * channelCount) + 1;
    //            data.Add(image[indexG]);
    //            var indexB = (((h * width) + w) * channelCount) + 2;
    //            data.Add(image[indexB]);
    //        }
    //    }
    //    var updSize = ConvertAndPad(data.Count + 2, 2);
    //    if (data.Count > 250)
    //    {
    //        var newMsg = new List<byte>();
    //        for (var i = 0; i <= data.Count; i++)
    //        {
    //            if (i % 249 == 0)
    //            {
    //                newMsg.AddRange(data.GetRange(i, Math.Min(249, data.Count - i)));
    //                newMsg.Add(0);
    //            }
    //        }
    //        // remove last padding 0
    //        newMsg.RemoveAt(newMsg.Count - 1);
    //        data = newMsg;
    //    }

    //    data.Add(0xef);
    //    data.Add(0x69);
    //    return (data.ToArray(), updSize);
    //}

    //private void DisplayPartialImage(int x, int y, int width, int height, TuringSmartScreenBufferC buffer)
    //{
    //    var (data, updSize) = GeneratePartialUpdateFromBuffer(height, width, x, y, buffer.ImgBuffer);
    //    var cmd = new List<byte>(UPDATE_BITMAP);
    //    cmd.AddRange(updSize);
    //    WriteCommand(cmd);
    //    WriteCommand(data);
    //}
}
