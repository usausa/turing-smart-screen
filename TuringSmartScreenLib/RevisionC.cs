namespace TuringSmartScreenLib;

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Ports;
using System.Reflection;
using System.Text;

public sealed unsafe class TuringSmartScreenRevisionC : IDisposable
{
    private const int WriteSize = 250;
    private const int ReadSize = 1024;
    private const int ReadHelloSize = 23;

    private static readonly byte[] CommandHello = [0x01, 0xef, 0x69, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0xc5, 0xd3];
    private static readonly byte[] CommandSetBrightness = [0x7b, 0xef, 0x69, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00];
    private static readonly byte[] CommandDisplayBitmap = [0xc8, 0xef, 0x69, 0x00, 0x17, 0x70];
    private static readonly byte[] CommandPreUpdateBitmap = [0x86, 0xef, 0x69, 0x00, 0x00, 0x00, 0x01];
    private static readonly byte[] CommandUpdateBitmap = [0xcc, 0xef, 0x69, 0x00, 0x00];
    private static readonly byte[] CommandQueryStatus = [0xcf, 0xef, 0x69, 0x00, 0x00, 0x00, 0x01];
    private static readonly byte[] CommandRestart = [0x84, 0xef, 0x69, 0x00, 0x00, 0x00, 0x01];

    private static readonly byte[] CommandUpdateBitmapTerminate = [0xef, 0x69];

    private readonly SerialPort port;

    private byte[] writeBuffer;

    private byte[] readBuffer;

    private int writeOffset;

#pragma warning disable CA1822
    public int Width => 800;

    public int Height => 480;
#pragma warning restore CA1822

    public TuringSmartScreenRevisionC(string name)
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
            try
            {
                port.Close();
            }
            catch (IOException)
            {
                // Ignore
            }
            catch (TargetInvocationException)
            {
                // Ignore
            }
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

    private ReadOnlySpan<byte> ReadResponse(int length = ReadSize)
    {
        var offset = 0;
        try
        {
            while (offset < length)
            {
                var read = port.Read(readBuffer, offset, length - offset);
                if (read <= 0)
                {
                    break;
                }

                offset += read;
            }
        }
        catch (IOException)
        {
            // Ignore
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

            values = values[block.Length..];
        }

        if (values.Length > 0)
        {
            values.CopyTo(writeBuffer.AsSpan(writeOffset));
            writeOffset += values.Length;
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

    public void Clear(byte r = 0, byte g = 0, byte b = 0)
    {
        // Start
        Write(0x2c);
        Flush(0x2c);

        // DisplayBitmap
        Write(CommandDisplayBitmap);
        Flush();

        // Payload
        var pattern = (Span<byte>)stackalloc byte[4];
        pattern[0] = b;
        pattern[1] = g;
        pattern[2] = r;
        pattern[3] = 0xff;
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                Write(pattern);
            }
        }
        Flush();

        // PreUpdateBitmap
        Write(CommandPreUpdateBitmap);
        Flush();
        ReadResponse();

        // QueryStatus
        Write(CommandQueryStatus);
        Flush();
        ReadResponse();
    }

    public void SetBrightness(int level)
    {
        Write(CommandSetBrightness);
        Write((byte)level);
        Flush();
    }

    public void Reset()
    {
        Write(CommandRestart);
        Flush();
    }

    private int count;

    public void DisplayBitmap(int x, int y, byte[] bitmap, int width, int height)
    {
        if ((x == 0) && (y == 0) && (width == Width) && (height == Height))
        {
            DisplayFullBitmap(bitmap);
            count = 0;
            CanDisplayPartialBitmap = true;
        }
        else
        {
            DisplayPartialBitmap(x, y, bitmap, width, height, count);
            count++;
        }
    }

    private void DisplayFullBitmap(byte[] bitmap)
    {
        // Start
        Write(0x2c);
        Flush(0x2c);

        // DisplayBitmap
        Write(CommandDisplayBitmap);
        Flush();

        // Payload
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var offset = ((y * Width) + x) * 3;
                Write(bitmap.AsSpan(offset, 3));
                Write(0xff);
            }
        }
        Flush();

        // PreUpdateBitmap
        Write(CommandPreUpdateBitmap);
        Flush();
        ReadResponse();

        // QueryStatus
        Write(CommandQueryStatus);
        Flush();
        ReadResponse();
    }

    public bool CanDisplayPartialBitmap { get => field; private set; }

    private void DisplayPartialBitmap(int x, int y, byte[] bitmap, int width, int height, int count)
    {
        var header = (Span<byte>)stackalloc byte[5];
        header[3] = (byte)((width >> 8) & 0xff);
        header[4] = (byte)(width & 0xff);

        var bitmapSize = (((width * 3) + header.Length) * height) + CommandUpdateBitmapTerminate.Length;
        var size = (Span<byte>)stackalloc byte[2];
        size[0] = (byte)((bitmapSize >> 8) & 0xff);
        size[1] = (byte)(bitmapSize & 0xff);

        Span<byte> countBytes = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(countBytes, count);

        // UpdateBitmap
        Write(CommandUpdateBitmap);
        Write(size);
        Write([0x00, 0x00, 0x00]);
        Write(countBytes);
        Flush();

        // Payload
        for (var h = 0; h < height; h++)
        {
            var position = ((y + h) * Width) + x;
            header[0] = (byte)((position >> 16) & 0xff);
            header[1] = (byte)((position >> 8) & 0xff);
            header[2] = (byte)(position & 0xff);
            Write(header);
            for (var w = 0; w < width; w++)
            {
                var offset = ((h * width) + w) * 3;
                Write(bitmap.AsSpan(offset, 3));
            }
        }
        Write(CommandUpdateBitmapTerminate);
        Flush();

        //UpdateBitmap
        Write(CommandQueryStatus);
        Flush();

        var response = ReadResponse();
        if ((response.Length != ReadSize) || (response.IndexOf("needReSend:1"u8) >= 0))
        {
            CanDisplayPartialBitmap = false;
        }
    }
}
