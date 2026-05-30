namespace LcdDriver.TuringSmartScreen;

using System.Buffers;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;

public sealed class ScreenDevice : IDisposable
{
    private const int CommandSize = 500;
    private const int PaddingCommandSize = (CommandSize + 7) & ~7;
    private const int PacketSize = 512;

    private const int WriteTimeout = 1500;
    private const int ReadTimeout = 2000;
    // Device may take several seconds to write data to storage
    private const int FileWriteReadTimeout = 10000;

    private const int MaxFileChunkSize = 65536;

    private static readonly byte[] KeyIv = "slv3tuzx"u8.ToArray();

    private readonly UsbDevice usbDevice;

    private readonly UsbEndpointReader reader;

    private readonly UsbEndpointWriter writer;

#pragma warning disable CA5351
    private readonly DES des = DES.Create();
#pragma warning restore CA5351

    private byte[] commandBuffer;

    private byte[] encryptedBuffer;

    private byte[] readBuffer;

    // --------------------------------------------------------------------------------
    // Constructor
    // --------------------------------------------------------------------------------

    public ScreenDevice(UsbDevice usbDevice)
    {
        this.usbDevice = usbDevice;

        usbDevice.SetConfiguration(1);
        usbDevice.ClaimInterface(0);

        reader = usbDevice.OpenEndpointReader(ReadEndpointID.Ep01);
        writer = usbDevice.OpenEndpointWriter(WriteEndpointID.Ep01);

        des.Key = KeyIv;
        des.IV = KeyIv;

        commandBuffer = ArrayPool<byte>.Shared.Rent(PaddingCommandSize);
        encryptedBuffer = ArrayPool<byte>.Shared.Rent(PacketSize);
        readBuffer = ArrayPool<byte>.Shared.Rent(PacketSize);
    }

    public void Dispose()
    {
        if (usbDevice.IsOpen)
        {
            usbDevice.ReleaseInterface(0);
            usbDevice.Close();
        }

        if (commandBuffer.Length > 0)
        {
            ArrayPool<byte>.Shared.Return(commandBuffer);
            commandBuffer = [];
        }
        if (encryptedBuffer.Length > 0)
        {
            ArrayPool<byte>.Shared.Return(encryptedBuffer);
            encryptedBuffer = [];
        }
        if (readBuffer.Length > 0)
        {
            ArrayPool<byte>.Shared.Return(readBuffer);
            readBuffer = [];
        }

        des.Dispose();
    }

    // --------------------------------------------------------------------------------
    // Helper
    // --------------------------------------------------------------------------------

    private void PrepareCommandHeader(byte commandId)
    {
        commandBuffer.AsSpan().Clear();

        commandBuffer[0] = commandId;

        commandBuffer[2] = 0x1A;
        commandBuffer[3] = 0x6D;

        var timestamp = (int)DateTime.Now.TimeOfDay.TotalMilliseconds;
        BinaryPrimitives.WriteInt32LittleEndian(commandBuffer.AsSpan(4, 4), timestamp);
    }

    private bool SendCommand()
    {
        encryptedBuffer.AsSpan().Clear();
        des.EncryptCbc(commandBuffer.AsSpan(0, PaddingCommandSize), KeyIv, encryptedBuffer, PaddingMode.None);

        encryptedBuffer[PacketSize - 2] = 0xA1;
        encryptedBuffer[PacketSize - 1] = 0x1A;

        // Flush pending data caused by ZLP handling
        reader.ReadFlush();

        writer.Write(encryptedBuffer.AsSpan(0, PacketSize), WriteTimeout, out var transferLength);
        return transferLength == PacketSize;
    }

    private bool SendCommandWithData(byte[] data, int length)
    {
        encryptedBuffer.AsSpan().Clear();
        des.EncryptCbc(commandBuffer.AsSpan(0, PaddingCommandSize), KeyIv, encryptedBuffer, PaddingMode.None);

        encryptedBuffer[PacketSize - 2] = 0xA1;
        encryptedBuffer[PacketSize - 1] = 0x1A;

        var combinedLength = PacketSize + length;
        var combined = ArrayPool<byte>.Shared.Rent(combinedLength);
        try
        {
            encryptedBuffer.AsSpan(0, PacketSize).CopyTo(combined);
            data.AsSpan(0, length).CopyTo(combined.AsSpan(PacketSize));

            // Flush pending data caused by ZLP handling
            reader.ReadFlush();

            writer.Write(combined.AsSpan(0, combinedLength), WriteTimeout, out var transferLength);
            return transferLength == combinedLength;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(combined);
        }
    }

    private bool SendData(byte[] data)
    {
        writer.Write(data.AsSpan(), WriteTimeout, out var transferLength);
        return transferLength == data.Length;
    }

    private bool ReceiveResponse(int readTimeout = ReadTimeout)
    {
        reader.Read(readBuffer.AsSpan(0, PacketSize), readTimeout, out var transferLength);
        if (transferLength != PacketSize)
        {
            return false;
        }

        // resp[1] == 0xC8 is success
        return readBuffer[1] == 0xC8;
    }

    private void WritePathToCommand(string path)
    {
        var length = Encoding.ASCII.GetBytes(path.AsSpan(), commandBuffer.AsSpan(16));
        BinaryPrimitives.WriteInt32BigEndian(commandBuffer.AsSpan(8, 4), length);
        commandBuffer.AsSpan(12, 4).Clear();
    }

    // --------------------------------------------------------------------------------
    // Command
    // --------------------------------------------------------------------------------

    public bool Sync()
    {
        PrepareCommandHeader(10);
        return SendCommand() && ReceiveResponse();
    }

    public bool Restart()
    {
        PrepareCommandHeader(11);
        return SendCommand() && ReceiveResponse();
    }

    public bool SetOrientation(ScreenOrientation value)
    {
        PrepareCommandHeader(13);
        commandBuffer[8] = (byte)value;
        return SendCommand() && ReceiveResponse();
    }

    public bool SetBrightness(byte value)
    {
        PrepareCommandHeader(14);
        commandBuffer[8] = value;
        return SendCommand() && ReceiveResponse();
    }

    public bool SetFrameRate(byte value)
    {
        PrepareCommandHeader(15);
        commandBuffer[8] = value;
        return SendCommand() && ReceiveResponse();
    }

    public bool DrawPng(byte[] imageBytes)
    {
        PrepareCommandHeader(102);
        BinaryPrimitives.WriteInt32BigEndian(commandBuffer.AsSpan(8, 4), imageBytes.Length);
        return SendCommand() && SendData(imageBytes) && ReceiveResponse();
    }

    public bool DrawJpeg(byte[] imageBytes)
    {
        PrepareCommandHeader(101);
        BinaryPrimitives.WriteInt32BigEndian(commandBuffer.AsSpan(8, 4), imageBytes.Length);
        return SendCommand() && SendData(imageBytes) && ReceiveResponse();
    }

    // --------------------------------------------------------------------------------
    // Extended command
    // --------------------------------------------------------------------------------

    public CapacityInfo? RefreshStorage()
    {
        PrepareCommandHeader(100);
        if (!(SendCommand() && ReceiveResponse()))
        {
            return null;
        }

        var total = BinaryPrimitives.ReadUInt32LittleEndian(readBuffer.AsSpan(8, 4));
        var used = BinaryPrimitives.ReadUInt32LittleEndian(readBuffer.AsSpan(12, 4));
        var valid = BinaryPrimitives.ReadUInt32LittleEndian(readBuffer.AsSpan(16, 4));
        return new CapacityInfo(total, used, valid);
    }

    public bool SaveSettings(byte brightness = 0, byte startup = 0, byte reserved = 0, byte rotation = 0, byte sleep = 0, byte offline = 0)
    {
        PrepareCommandHeader(125);
        commandBuffer[8] = brightness;
        commandBuffer[9] = startup;
        commandBuffer[10] = reserved;
        commandBuffer[11] = rotation;
        commandBuffer[12] = sleep;
        commandBuffer[13] = offline;
        return SendCommand() && ReceiveResponse();
    }

    // --------------------------------------------------------------------------------
    // File command
    // --------------------------------------------------------------------------------

    private bool OpenFile(string devicePath)
    {
        PrepareCommandHeader(38);
        WritePathToCommand(devicePath);
        return SendCommand() && ReceiveResponse();
    }

    private bool WriteFileChunk(byte[] buffer, int length, bool isLast)
    {
        PrepareCommandHeader(39);
        BinaryPrimitives.WriteInt32BigEndian(commandBuffer.AsSpan(8, 4), MaxFileChunkSize);
        BinaryPrimitives.WriteInt32BigEndian(commandBuffer.AsSpan(12, 4), length);
        commandBuffer[16] = isLast ? (byte)1 : (byte)0;
        if (SendCommandWithData(buffer, length) && ReceiveResponse(FileWriteReadTimeout))
        {
            return true;
        }

        // Older firmware fallback
        PrepareCommandHeader(39);
        BinaryPrimitives.WriteInt32BigEndian(commandBuffer.AsSpan(8, 4), length);
        commandBuffer[16] = isLast ? (byte)1 : (byte)0;
        return SendCommandWithData(buffer, length) && ReceiveResponse(FileWriteReadTimeout);
    }

    public bool WriteFile(Stream stream, string devicePath, Action<long, long>? progress = null)
    {
        if (!OpenFile(devicePath))
        {
            return false;
        }

        var size = stream.CanSeek ? stream.Length : -1L;
        var sent = 0L;
        var buffer = ArrayPool<byte>.Shared.Rent(MaxFileChunkSize);
        try
        {
            while (true)
            {
                var bytesRead = stream.Read(buffer, 0, MaxFileChunkSize);
                if (bytesRead == 0)
                {
                    break;
                }

                sent += bytesRead;
                var isLast = stream.CanSeek ? stream.Position >= stream.Length : bytesRead < MaxFileChunkSize;
                if (!WriteFileChunk(buffer, bytesRead, isLast))
                {
                    return false;
                }

                progress?.Invoke(sent, size);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return true;
    }

    public async ValueTask<bool> WriteFileAsync(Stream stream, string devicePath, Action<long, long>? progress = null, CancellationToken cancel = default)
    {
        if (!OpenFile(devicePath))
        {
            return false;
        }

        var size = stream.CanSeek ? stream.Length : -1L;
        var sent = 0L;
        var buffer = ArrayPool<byte>.Shared.Rent(MaxFileChunkSize);
        try
        {
            while (true)
            {
                cancel.ThrowIfCancellationRequested();

                var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, MaxFileChunkSize), cancel).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    break;
                }

                sent += bytesRead;
                var isLast = stream.CanSeek ? stream.Position >= stream.Length : bytesRead < MaxFileChunkSize;
                if (!WriteFileChunk(buffer, bytesRead, isLast))
                {
                    return false;
                }

                progress?.Invoke(sent, size);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return true;
    }

    public bool DeleteFile(string devicePath)
    {
        PrepareCommandHeader(40);
        WritePathToCommand(devicePath);
        return SendCommand();
    }

    // --------------------------------------------------------------------------------
    // Play command
    // --------------------------------------------------------------------------------

    public bool PrepareStreamBuffer()
    {
        PrepareCommandHeader(41);
        return SendCommand() && ReceiveResponse();
    }

    public bool StopPlayback()
    {
        PrepareCommandHeader(111);
        return SendCommand() && ReceiveResponse();
    }

    public bool ResetPlayback()
    {
        PrepareCommandHeader(112);
        return SendCommand() && ReceiveResponse();
    }

    public bool PlayFile(string devicePath)
    {
        PrepareCommandHeader(98);
        WritePathToCommand(devicePath);
        return SendCommand() && ReceiveResponse();
    }

    public bool PlayFile2(string devicePath)
    {
        PrepareCommandHeader(110);
        WritePathToCommand(devicePath);
        return SendCommand() && ReceiveResponse();
    }

    public bool PlayFile3(string devicePath)
    {
        PrepareCommandHeader(113);
        WritePathToCommand(devicePath);
        return SendCommand() && ReceiveResponse();
    }

    // --------------------------------------------------------------------------------
    // Stream command
    // --------------------------------------------------------------------------------

    private int GetH264ChunkSize()
    {
        PrepareCommandHeader(17);
        if (!(SendCommand() && ReceiveResponse()))
        {
            return 202752;
        }

        var negotiated = BinaryPrimitives.ReadInt32BigEndian(readBuffer.AsSpan(8, 4));
        return ((negotiated > 0) && (negotiated <= MaxFileChunkSize)) ? negotiated : 202752;
    }

    private bool WriteH264Chunk(byte[] buffer, int length, bool isLast, out byte queueDepth)
    {
        PrepareCommandHeader(121);
        BinaryPrimitives.WriteInt32BigEndian(commandBuffer.AsSpan(8, 4), length);
        commandBuffer[12] = (byte)(isLast ? 1 : 0);

        if (!SendCommandWithData(buffer, length) || !ReceiveResponse())
        {
            queueDepth = 0;
            return false;
        }

        queueDepth = readBuffer[8];
        return true;
    }

    private bool GetStreamStatus(out byte queueDepth)
    {
        PrepareCommandHeader(122);
        if (!SendCommand() || !ReceiveResponse())
        {
            queueDepth = 0;
            return false;
        }

        queueDepth = readBuffer[8];
        return true;
    }

    private bool StopStream()
    {
        PrepareCommandHeader(123);
        return SendCommand() && ReceiveResponse();
    }

    public bool PlayStream(Stream stream)
    {
        var chunkSize = GetH264ChunkSize();
        var fileSize = stream.Length;
        var sent = 0L;
        var buffer = ArrayPool<byte>.Shared.Rent(chunkSize);
        try
        {
            while (true)
            {
                var bytesRead = stream.Read(buffer, 0, chunkSize);
                if (bytesRead == 0)
                {
                    break;
                }

                sent += bytesRead;
                var isLast = sent >= fileSize;

                if (!WriteH264Chunk(buffer, bytesRead, isLast, out var queueDepth))
                {
                    return false;
                }

                if (queueDepth > 2)
                {
                    while (GetStreamStatus(out var depth) && (depth > 2))
                    {
                        Thread.Sleep(10);
                    }
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (GetStreamStatus(out var remaining) && (remaining > 0) && (watch.ElapsedMilliseconds < 10_000))
        {
            Thread.Sleep(10);
        }

        return StopStream();
    }

    public async ValueTask<bool> PlayStreamAsync(Stream stream, CancellationToken cancel = default)
    {
        var chunkSize = GetH264ChunkSize();
        var fileSize = stream.Length;
        var sent = 0L;
        var buffer = ArrayPool<byte>.Shared.Rent(chunkSize);
        try
        {
            while (!cancel.IsCancellationRequested)
            {
                var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, chunkSize), cancel).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    break;
                }

                sent += bytesRead;
                var isLast = sent >= fileSize;
                if (!WriteH264Chunk(buffer, bytesRead, isLast, out var queueDepth))
                {
                    return false;
                }

                if (queueDepth > 2)
                {
                    while (!cancel.IsCancellationRequested && GetStreamStatus(out var depth) && depth > 2)
                    {
                        await Task.Delay(10, cancel).ConfigureAwait(false);
                    }
                }
            }

            cancel.ThrowIfCancellationRequested();

            var watch = System.Diagnostics.Stopwatch.StartNew();
            while (GetStreamStatus(out var remaining) && (remaining > 0) && (watch.ElapsedMilliseconds < 10_000))
            {
                await Task.Delay(10, cancel).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            StopStream();
            throw;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return StopStream();
    }

    public bool StopMedia()
    {
        PrepareCommandHeader(111);
        return RequestResponse();
    }
}
