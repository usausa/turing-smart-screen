namespace LcdDriver.TuringSmartScreen;

using System.Buffers;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

using LibUsbDotNet;
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

    private const int MaxFileChunkSize = 1024 * 1024;

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

        if (usbDevice is IUsbDevice wholeUsbDevice)
        {
            wholeUsbDevice.SetConfiguration(1);
            wholeUsbDevice.ClaimInterface(0);
        }

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
            reader.Dispose();
            writer.Dispose();

            if (usbDevice is IUsbDevice wholeUsbDevice)
            {
                wholeUsbDevice.ReleaseInterface(0);
            }

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

    /// <summary>
    /// Encodes an ASCII path into <c>commandBuffer[16..]</c> and writes the byte length
    /// big-endian into <c>commandBuffer[8..11]</c>.
    /// Uses <see cref="Encoding.ASCII"/> <c>GetBytes(ReadOnlySpan&lt;char&gt;, Span&lt;byte&gt;)</c>
    /// to avoid a heap allocation for the intermediate byte array.
    /// </summary>
    private void WritePathToCommand(string path)
    {
        var len = Encoding.ASCII.GetBytes(path.AsSpan(), commandBuffer.AsSpan(16));
        BinaryPrimitives.WriteInt32BigEndian(commandBuffer.AsSpan(8, 4), len);
        commandBuffer.AsSpan(12, 4).Clear();
    }

    /// <summary>
    /// Encrypts <c>commandBuffer</c>, appends the end marker, flushes the IN endpoint,
    /// and writes the 512-byte encrypted packet to the device.
    /// </summary>
    private bool SendCommand()
    {
        encryptedBuffer.AsSpan().Clear();
        des.EncryptCbc(commandBuffer.AsSpan(0, PaddingCommandSize), KeyIv, encryptedBuffer, PaddingMode.None);

        encryptedBuffer[PacketSize - 2] = 0xA1;
        encryptedBuffer[PacketSize - 1] = 0x1A;

        // Flush pending data caused by ZLP handling
        reader.ReadFlush();

        var errorCode = writer.Write(encryptedBuffer, 0, PacketSize, WriteTimeout, out var transferLength);
        return (errorCode == ErrorCode.None) && (transferLength == PacketSize);
    }

    /// <summary>
    /// Encrypts the command header and concatenates the payload into a single USB bulk write.
    /// Mirrors Python's <c>write_to_device(encrypt_command_packet(pkt) + payload)</c>.
    /// </summary>
    private bool SendCommandWithPayload(byte[] data)
    {
        encryptedBuffer.AsSpan().Clear();
        des.EncryptCbc(commandBuffer.AsSpan(0, PaddingCommandSize), KeyIv, encryptedBuffer, PaddingMode.None);

        encryptedBuffer[PacketSize - 2] = 0xA1;
        encryptedBuffer[PacketSize - 1] = 0x1A;

        var combinedLength = PacketSize + data.Length;
        var combined = ArrayPool<byte>.Shared.Rent(combinedLength);
        try
        {
            encryptedBuffer.AsSpan(0, PacketSize).CopyTo(combined);
            data.CopyTo(combined, PacketSize);

            reader.ReadFlush();

            var errorCode = writer.Write(combined, 0, combinedLength, WriteTimeout, out var transferLength);
            return (errorCode == ErrorCode.None) && (transferLength == combinedLength);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(combined);
        }
    }

    /// <summary>
    /// Writes a raw data payload to the device. Used after <see cref="SendCommand"/> when
    /// the command is followed by a separate data transfer.
    /// </summary>
    public bool SendData(byte[] data)
    {
        var errorCode = writer.Write(data, 0, data.Length, WriteTimeout, out var transferLength);
        return (errorCode == ErrorCode.None) && (transferLength == data.Length);
    }

    /// <summary>
    /// Reads one 512-byte response packet and verifies the success byte (<c>resp[1] == 0xC8</c>).
    /// All commands except cmd 40 (DeleteFile) return a response — confirmed by device probing.
    /// </summary>
    private bool ReceiveResponse(int readTimeout = ReadTimeout)
    {
        var errorCode = reader.Read(readBuffer, 0, PacketSize, readTimeout, out var transferLength);
        if ((errorCode != ErrorCode.None) || (transferLength != PacketSize))
        {
            return false;
        }

        // resp[1] == 0xC8 indicates success on all responding commands
        return readBuffer[1] == 0xC8;
    }

    /// <summary>Sends a raw command and probes for a response. Used only for diagnostics.</summary>
    public bool ProbeCommand(byte commandId, out byte[]? responseBytes, out bool received, int timeoutMs = 2000, Action<byte[]>? configureBuffer = null)
    {
        responseBytes = null;
        received = false;
        PrepareCommandHeader(commandId);
        configureBuffer?.Invoke(commandBuffer);
        if (!SendCommand())
        {
            return false;
        }

        var buf = new byte[PacketSize];
        var errorCode = reader.Read(buf, 0, PacketSize, timeoutMs, out var transferLength);
        received = errorCode == ErrorCode.None && transferLength == PacketSize;
        if (received)
        {
            responseBytes = buf;
        }

        return true;
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

    /// <summary>
    /// Refreshes storage capacity information from the device (cmd 100).
    /// Mirrors Python's <c>send_refresh_storage_command</c>.
    /// </summary>
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

    /// <summary>Opens a remote file on the device storage for writing (cmd 38).</summary>
    private bool OpenFile(string devicePath)
    {
        PrepareCommandHeader(38);
        WritePathToCommand(devicePath);
        return SendCommand() && ReceiveResponse();
    }

    /// <summary>Writes a chunk of data to the previously opened remote file.</summary>
    private bool WriteFileChunk(byte[] data, bool isLast)
    {
        // Primary layout: [8..11]=capacity, [12..15]=chunk_len, [16]=last_flag
        PrepareCommandHeader(39);
        BinaryPrimitives.WriteInt32BigEndian(commandBuffer.AsSpan(8, 4), MaxFileChunkSize);
        BinaryPrimitives.WriteInt32BigEndian(commandBuffer.AsSpan(12, 4), data.Length);
        commandBuffer[16] = isLast ? (byte)1 : (byte)0;
        if (SendCommandWithPayload(data) && ReceiveResponse(FileWriteReadTimeout))
        {
            return true;
        }

        // Legacy fallback: [8..11]=chunk_len only (older firmware)
        PrepareCommandHeader(39);
        BinaryPrimitives.WriteInt32BigEndian(commandBuffer.AsSpan(8, 4), data.Length);
        commandBuffer[16] = isLast ? (byte)1 : (byte)0;
        return SendCommandWithPayload(data) && ReceiveResponse(FileWriteReadTimeout);
    }

    /// <summary>
    /// Uploads a stream to the device storage.
    /// </summary>
    /// <param name="stream">Source data stream.</param>
    /// <param name="devicePath">Target path on device storage.</param>
    /// <param name="progress">Optional progress callback (bytesSent, totalBytes); totalBytes is -1 when unknown.</param>
    public bool WriteFile(Stream stream, string devicePath, Action<long, long>? progress = null)
    {
        if (!OpenFile(devicePath))
        {
            return false;
        }

        var fileSize = stream.CanSeek ? stream.Length : -1L;
        var sent = 0L;
        var buffer = new byte[MaxFileChunkSize];

        while (true)
        {
            var bytesRead = stream.Read(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                break;
            }

            sent += bytesRead;
            var isLast = stream.CanSeek
                ? stream.Position >= stream.Length
                : bytesRead < buffer.Length;

            var chunk = bytesRead == buffer.Length ? buffer : buffer[..bytesRead].ToArray();

            if (!WriteFileChunk(chunk, isLast))
            {
                return false;
            }

            progress?.Invoke(sent, fileSize);
        }

        return true;
    }

    /// <summary>
    /// Deletes a file from the device storage (cmd 40).
    /// Device probing confirmed cmd 40 does NOT return a response; only SendCommand is called.
    /// </summary>
    public bool DeleteFile(string devicePath)
    {
        PrepareCommandHeader(40);
        WritePathToCommand(devicePath);
        return SendCommand();
    }

    // --------------------------------------------------------------------------------
    // Play command
    // --------------------------------------------------------------------------------

    /// <summary>Prepares the device stream buffer queue before H264 streaming (cmd 41).</summary>
    public bool PrepareStreamBuffer()
    {
        PrepareCommandHeader(41);
        return SendCommand() && ReceiveResponse();
    }

    /// <summary>Stops any running playback, pass 1 of 2 (cmd 111).</summary>
    public bool StopPlayback()
    {
        PrepareCommandHeader(111);
        return SendCommand() && ReceiveResponse();
    }

    /// <summary>Stops any running playback, pass 2 of 2 (cmd 112).</summary>
    public bool ResetPlayback()
    {
        PrepareCommandHeader(112);
        return SendCommand() && ReceiveResponse();
    }

    /// <summary>Requests video playback of a file stored on the device (cmd 98).</summary>
    public bool PlayFile(string devicePath)
    {
        PrepareCommandHeader(98);
        WritePathToCommand(devicePath);
        return SendCommand() && ReceiveResponse();
    }

    /// <summary>Requests alternate video playback of a file stored on the device (cmd 110).</summary>
    public bool PlayFile2(string devicePath)
    {
        PrepareCommandHeader(110);
        WritePathToCommand(devicePath);
        return SendCommand() && ReceiveResponse();
    }

    /// <summary>Requests image display of a file stored on the device (cmd 113).</summary>
    public bool PlayFile3(string devicePath)
    {
        PrepareCommandHeader(113);
        WritePathToCommand(devicePath);
        return SendCommand() && ReceiveResponse();
    }

    // --------------------------------------------------------------------------------
    // Stream command
    // --------------------------------------------------------------------------------

    /// <summary>
    /// Sends a chunk of H264 bitstream data for live streaming (cmd 121) and reads the response.
    /// <c>resp[8]</c> carries the queue depth used for flow control.
    /// Returns <c>false</c> only when the USB write itself fails.
    /// </summary>
    private bool PlayH264Chunk(byte[] data, bool isLast, out byte queueDepth)
    {
        queueDepth = 0;
        PrepareCommandHeader(121);
        BinaryPrimitives.WriteInt32BigEndian(commandBuffer.AsSpan(8, 4), data.Length);
        if (isLast)
        {
            commandBuffer[12] = 1;
        }

        if (!SendCommandWithPayload(data))
        {
            return false;
        }

        // The device responds to every chunk; resp[8] is the queue depth (mirrors cmd 122).
        if (ReceiveResponse())
        {
            queueDepth = readBuffer[8];
        }
        return true;
    }

    /// <summary>
    /// Streams H264 bitstream from <paramref name="stream"/> to the device synchronously
    /// and blocks until playback is complete.
    /// </summary>
    /// <param name="stream">H264 bitstream. Must be seekable (length is used to detect the last chunk).</param>
    /// <returns><c>false</c> if a USB write error occurred during streaming.</returns>
    public bool StreamH264(Stream stream)
    {
        var chunkSize = GetH264ChunkSize();
        var buffer = new byte[chunkSize];
        var fileSize = stream.Length;
        var sent = 0L;

        while (true)
        {
            var bytesRead = stream.Read(buffer, 0, chunkSize);
            if (bytesRead == 0)
            {
                break;
            }

            sent += bytesRead;
            var isLast = sent >= fileSize;
            var chunk = bytesRead == chunkSize ? buffer : buffer[..bytesRead].ToArray();

            if (!PlayH264Chunk(chunk, isLast, out var queueDepth))
            {
                return false;
            }

            if (queueDepth > 2)
            {
                while (GetStreamStatus() > 2)
                {
                    Thread.Sleep(10);
                }
            }
        }

        // Block until the device drains its playback queue.
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (GetStreamStatus() > 0 && sw.ElapsedMilliseconds < 10_000)
        {
            Thread.Sleep(10);
        }

        return true;
    }

    /// <summary>
    /// Streams H264 bitstream from <paramref name="stream"/> to the device asynchronously.
    /// Waits for playback to complete before the returned task finishes.
    /// Cancellation stops feeding new chunks and calls <see cref="StopStream"/>.
    /// </summary>
    /// <param name="stream">H264 bitstream. Must be seekable (length is used to detect the last chunk).</param>
    /// <param name="ct">Cancellation token.</param>
    public async ValueTask StreamH264Async(Stream stream, CancellationToken ct = default)
    {
        var chunkSize = GetH264ChunkSize();
        var buffer = new byte[chunkSize];
        var fileSize = stream.Length;
        var sent = 0L;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var bytesRead = await stream.ReadAsync(buffer, ct).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    break;
                }

                sent += bytesRead;
                var isLast = sent >= fileSize;
                var chunk = bytesRead == chunkSize ? buffer : buffer[..bytesRead].ToArray();

                if (!PlayH264Chunk(chunk, isLast, out var queueDepth))
                {
                    throw new InvalidOperationException("PlayH264Chunk USB write failed.");
                }

                if (queueDepth > 2)
                {
                    while (!ct.IsCancellationRequested && GetStreamStatus() > 2)
                    {
                        Thread.Sleep(10);
                    }
                }
            }

            ct.ThrowIfCancellationRequested();

            // Wait until the device drains its playback queue.
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (GetStreamStatus() > 0 && sw.ElapsedMilliseconds < 10_000)
            {
                Thread.Sleep(10);
            }
        }
        catch (OperationCanceledException)
        {
            StopStream();
            throw;
        }
    }

    /// <summary>
    /// Queries the preferred H264 chunk size from the device (cmd 17).
    /// Device probing shows resp[8..11] is all-zero on this model; falls back to 202752 bytes.
    /// </summary>
    public int GetH264ChunkSize()
    {
        PrepareCommandHeader(17);
        if (!(SendCommand() && ReceiveResponse()))
        {
            return 202752;
        }

        var negotiated = BinaryPrimitives.ReadInt32BigEndian(readBuffer.AsSpan(8, 4));
        return (negotiated > 0 && negotiated <= MaxFileChunkSize) ? negotiated : 202752;
    }

    /// <summary>
    /// Returns the stream queue depth from the device (cmd 122).
    /// resp[8] holds the queue depth; 0 means ready.
    /// </summary>
    public byte GetStreamStatus()
    {
        PrepareCommandHeader(122);
        return (SendCommand() && ReceiveResponse()) ? readBuffer[8] : (byte)0;
    }

    public bool StopStream()
    {
        PrepareCommandHeader(123);
        return SendCommand() && ReceiveResponse();
    }
}
