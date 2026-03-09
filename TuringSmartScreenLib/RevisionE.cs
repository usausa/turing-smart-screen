namespace TuringSmartScreenLib;

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Ports;
using System.Reflection;
using System.Text;

public sealed unsafe class TuringSmartScreenRevisionE : IDisposable
{
    private const int WriteSize = 250;
    private const int ReadSize = 1024;
    private const int PartialBlockSize = 80;

    private static readonly byte[] CommandHello = [0x01, 0xef, 0x69, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0xc5, 0xd3];
    private static readonly byte[] CommandSetBrightness = [0x7b, 0xef, 0x69, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00];
    private static readonly byte[] CommandDisplayBitmapPrefix = [0xc8, 0xef, 0x69, 0x00];
    private static readonly byte[] CommandPreUpdateBitmap = [0x86, 0xef, 0x69, 0x00, 0x00, 0x00, 0x01];
    private static readonly byte[] CommandUpdateBitmap = [0xcc, 0xef, 0x69, 0x00, 0x00];
    private static readonly byte[] CommandQueryStatus = [0xcf, 0xef, 0x69, 0x00, 0x00, 0x00, 0x01];

    private static readonly byte[] CommandUpdateBitmapTerminate = [0xef, 0x69];

    private static readonly byte[] CommandQueryStorageInfo = [0x64, 0xef, 0x69, 0x00, 0x00, 0x00, 0x01];
    private static readonly byte[] CommandStartMedia = [0x96, 0xef, 0x69, 0x00, 0x00, 0x00, 0x01];
    private static readonly byte[] CommandStopMedia = [0x79, 0xef, 0x69, 0x00, 0x00, 0x00, 0x01];

    private readonly byte[] commandDisplayBitmap;

    private readonly SerialPort port;

    private byte[] writeBuffer;

    private byte[] readBuffer;

    private int writeOffset;

    private int renderCount;

    public int Width { get; }

    public int Height { get; }

    // --------------------------------------------------------------------------------
    // Constructor
    // --------------------------------------------------------------------------------

    public TuringSmartScreenRevisionE(string name, int width = 480, int height = 1920)
    {
        Width = width;
        Height = height;

        var payloadSize = width * height * 4;
        commandDisplayBitmap = [
            .. CommandDisplayBitmapPrefix,
            (byte)((payloadSize >> 16) & 0xff),
            (byte)((payloadSize >> 8) & 0xff),
            (byte)(payloadSize & 0xff)
        ];

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

        var response = ReadToTerminate();
        if (!response.StartsWith("chs_"u8))
        {
            throw new IOException($"Unknown response. response=[{Convert.ToHexString(response)}]");
        }
    }

    // --------------------------------------------------------------------------------
    // Raw I/O
    // --------------------------------------------------------------------------------

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
        catch (TimeoutException)
        {
            // Ignore
        }
        catch (IOException)
        {
            // Ignore
        }

        return readBuffer.AsSpan(0, offset);
    }

    private ReadOnlySpan<byte> ReadResponse(byte[] buffer, int length)
    {
        var offset = 0;
        try
        {
            while (offset < length)
            {
                var remainingCapacity = length - offset;
                var bytesToRead = port.BytesToRead;
                if (bytesToRead <= 0)
                {
                    bytesToRead = 1;
                }

                var read = port.Read(buffer, offset, Math.Min(bytesToRead, remainingCapacity));
                if (read <= 0)
                {
                    break;
                }

                var terminateIndex = buffer.AsSpan(offset, read).IndexOf((byte)0x00);
                if (terminateIndex >= 0)
                {
                    offset += terminateIndex;
                    break;
                }

                offset += read;
            }
        }
        catch (TimeoutException)
        {
            // Ignore
        }
        catch (IOException)
        {
            // Ignore
        }

        return buffer.AsSpan(0, offset);
    }

    private ReadOnlySpan<byte> ReadToTerminate()
    {
        var offset = 0;
        try
        {
            while (offset < readBuffer.Length)
            {
                var remainingCapacity = readBuffer.Length - offset;
                var bytesToRead = port.BytesToRead;
                if (bytesToRead <= 0)
                {
                    bytesToRead = 1;
                }

                var read = port.Read(readBuffer, offset, Math.Min(bytesToRead, remainingCapacity));
                if (read <= 0)
                {
                    break;
                }

                var terminateIndex = readBuffer.AsSpan(offset, read).IndexOf((byte)0x00);
                if (terminateIndex >= 0)
                {
                    offset += terminateIndex;
                    break;
                }

                offset += read;
            }
        }
        catch (TimeoutException)
        {
            // Ignore
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

    // --------------------------------------------------------------------------------
    // Command
    // --------------------------------------------------------------------------------

    public void SetBrightness(int level)
    {
        Write(CommandSetBrightness);
        Write((byte)level);
        Flush();
    }

    public void Clear() => Clear(0, 0, 0);

    public void Clear(byte r, byte g, byte b)
    {
        // Start
        Write(0x2c);
        Flush(0x2c);

        // DisplayBitmap
        Write(commandDisplayBitmap);
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

    private bool IsFullBitmap(int width, int height, RotateOption option)
    {
        if (option is RotateOption.None or RotateOption.Rotate180)
        {
            return width == Width && height == Height;
        }
        return width == Height && height == Width;
    }

    public bool DisplayBitmap(int x, int y, byte[] bitmap, int width, int height, RotateOption option = RotateOption.None)
    {
        if ((x == 0) && (y == 0) && IsFullBitmap(width, height, option))
        {
            DisplayFullBitmap(bitmap, option);
            renderCount = 0;
            return true;
        }

        for (var oy = 0; oy < height; oy += PartialBlockSize)
        {
            for (var ox = 0; ox < width; ox += PartialBlockSize)
            {
                if (!DisplayPartialBitmap(x + ox, y + oy, Math.Min(width - ox, PartialBlockSize), Math.Min(height - oy, PartialBlockSize), bitmap, ox, oy, width, option))
                {
                    return false;
                }
                renderCount++;
            }
        }

        return true;
    }

    private void DisplayFullBitmap(byte[] bitmap, RotateOption option)
    {
        // Start
        Write(0x2c);
        Flush(0x2c);

        // DisplayBitmap
        Write(commandDisplayBitmap);
        Flush();

        // Payload
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var offset = option switch
                {
                    RotateOption.Rotate90 => ((Width - 1 - x) * Height) + y,
                    RotateOption.Rotate270 => (x * Height) + (Height - 1 - y),
                    RotateOption.Rotate180 => ((Height - 1 - y) * Width) + (Width - 1 - x),
                    _ => (y * Width) + x
                };
                Write(bitmap.AsSpan(offset * 4, 4));
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

    private bool DisplayPartialBitmap(int x, int y, int w, int h, byte[] bitmap, int bitmapX, int bitmapY, int bitmapWidth, RotateOption option)
    {
        var (width, height, startX, startY) = option switch
        {
            RotateOption.Rotate90 => (h, w, Width - y - h, x),
            RotateOption.Rotate270 => (h, w, y, Height - x - w),
            RotateOption.Rotate180 => (w, h, Width - x - w, Height - y - h),
            _ => (w, h, x, y)
        };

        var header = (Span<byte>)stackalloc byte[5];
        header[3] = (byte)((width >> 8) & 0xff);
        header[4] = (byte)(width & 0xff);

        var bitmapSize = (((width * 4) + header.Length) * height) + CommandUpdateBitmapTerminate.Length;
        var size = (Span<byte>)stackalloc byte[2];
        size[0] = (byte)((bitmapSize >> 8) & 0xff);
        size[1] = (byte)(bitmapSize & 0xff);

        Span<byte> countBytes = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(countBytes, renderCount);

        // UpdateBitmap
        Write(CommandUpdateBitmap);
        Write(size);
        Write(stackalloc byte[3]);
        Write(countBytes);
        Flush();

        // Payload
        for (var oy = 0; oy < height; oy++)
        {
            var position = ((startY + oy) * Width) + startX;
            header[0] = (byte)((position >> 16) & 0xff);
            header[1] = (byte)((position >> 8) & 0xff);
            header[2] = (byte)(position & 0xff);
            Write(header);

            for (var ox = 0; ox < width; ox++)
            {
                var (px, py) = option switch
                {
                    RotateOption.Rotate90 => (bitmapX + oy, bitmapY + h - 1 - ox),
                    RotateOption.Rotate270 => (bitmapX + w - 1 - oy, bitmapY + ox),
                    RotateOption.Rotate180 => (bitmapX + w - 1 - ox, bitmapY + h - 1 - oy),
                    _ => (bitmapX + ox, bitmapY + oy)
                };
                Write(bitmap.AsSpan(((py * bitmapWidth) + px) * 4, 4));
            }
        }
        Write(CommandUpdateBitmapTerminate);
        Flush();

        // QueryStatus
        Write(CommandQueryStatus);
        Flush();

        var response = ReadResponse();
        // Succeed unless the device explicitly requests a resend.
        // Different panel models return different success responses,
        // but all use "needReSend:1" to indicate failure.
        return !response.StartsWith("needReSend:1"u8);
    }

    // --------------------------------------------------------------------------------
    // Storage command
    // --------------------------------------------------------------------------------

    private void SendPathCommand(byte command, string path, ReadOnlySpan<byte> data = default)
    {
        var pathBytes = Encoding.ASCII.GetBytes(path);
        Span<byte> header = stackalloc byte[10];
        header[0] = command;
        header[1] = 0xef;
        header[2] = 0x69;
        // bytes 3-5 = 0
        header[6] = (byte)pathBytes.Length;
        // bytes 7-9 = 0
        Write(header);
        Write(pathBytes);
        if (!data.IsEmpty)
        {
            Write(data);
        }
        Flush();
    }

    public string QueryStorageInfo()
    {
        Write(CommandQueryStorageInfo);
        Flush();
        // TODO check null terminate ?
        return Encoding.UTF8.GetString(ReadResponse(22));
    }

    public void StartMedia()
    {
        Write(CommandStartMedia);
        Flush();
        ReadResponse();
        // TODO check response media_stop ?
    }

    public void StopMedia()
    {
        Write(CommandStopMedia);
        Flush();
        // TODO has response ?
        //ReadResponse();
    }

    private const int ListFilesResponseSize = 10240;

    private static readonly byte[] ListFilesResponsePrefix = "result:dir:file:"u8.ToArray();

    public List<string> ListFiles(string path)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(ListFilesResponseSize);
        try
        {
            SendPathCommand(0x65, path);
            var response = ReadResponse(buffer, ListFilesResponseSize);
            if (!response.StartsWith(ListFilesResponsePrefix))
            {
                return [];
            }

            var files = new List<string>();

            var entries = response[ListFilesResponsePrefix.Length..];
            while (!entries.IsEmpty)
            {
                var index = entries.IndexOf((byte)'/');
                if (index < 0)
                {
                    if (!entries.IsEmpty)
                    {
                        files.Add(Encoding.UTF8.GetString(entries));
                    }

                    break;
                }

                if (index > 0)
                {
                    files.Add(Encoding.UTF8.GetString(entries[..index]));
                }

                entries = entries[(index + 1)..];
            }

            return files;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private const int UploadBlockSize = 4096;
    //private const int UploadBlockSize = 250;

    public bool UploadFile(string path, byte[] data)
    {
        Span<byte> sizeBytes = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(sizeBytes, data.Length);
        SendPathCommand(0x6f, path, sizeBytes);

        var response = ReadResponse();
        if (!response.StartsWith("create_success"u8))
        {
            return false;
        }

        var offset = 0;
        while (offset < data.Length)
        {
            var size = Math.Min(UploadBlockSize, data.Length - offset);
            port.Write(data, offset, size);
            offset += size;

            // TODO response check for each block ?
        }

        return true;
    }

    public void DeleteFile(string path)
    {
        SendPathCommand(0x66, path);
        // TODO response exist ?
    }
}
