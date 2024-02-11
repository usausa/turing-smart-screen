namespace TuringSmartScreenLib;

using System;
using System.Buffers;
using System.IO.Ports;

public sealed unsafe class TuringSmartScreenRevisionC2 : IDisposable
{
    public const int Width = 800;
    public const int Height = 480;

    private const int WriteSize = 250;
    private const int ReadSize = 1024;
    private const int ReadHelloSize = 23;

    private static readonly byte[] CommandHello = [0x01, 0xef, 0x69, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0xc5, 0xd3];
    private static readonly byte[] CommandSetBrightness = [0x7b, 0xef, 0x69, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00];
    private static readonly byte[] CommandDisplayBitmap = [0xc8, 0xef, 0x69, 0x00, 0x17, 0x70];
    private static readonly byte[] CommandPreUpdateBitmap = [0x86, 0xef, 0x69, 0x00, 0x00, 0x00, 0x01];
    private static readonly byte[] CommandQueryStatus = [0xcf, 0xef, 0x69, 0x00, 0x00, 0x00, 0x01];

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

    private ReadOnlySpan<byte> ReadResponse(int length = ReadSize)
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

    public void DisplayBitmap(int x, int y, int width, int height, byte[] bitmap)
    {
        if ((x == 0) && (y == 0) && (width == Width) && (height == Height))
        {
            DisplayFullBitmap(bitmap);
        }
        else
        {
            DisplayPartialBitmap(x, y, width, height, bitmap);
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

    private void DisplayPartialBitmap(int x, int y, int width, int height, byte[] bitmap)
    {
        // TODO
    }
}
