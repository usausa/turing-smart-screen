namespace LcdDriver.TrofeoVisionLy;

using System.Buffers;
using System.Buffers.Binary;

using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;

public sealed class ScreenDevice : IDisposable
{
    private const int HandshakeSize = 2048;
    private const int PacketSize = 512;

    // Frame chunk: Header (16 bytes) + Data (496 bytes)
    private const int ChunkHeaderSize = 16;
    private const int ChunkDataSize = PacketSize - ChunkHeaderSize;
    // Chunk count is padded to a multiple of 4, and written in 4096 bytes units
    private const int ChunkAlignment = 4;
    private const int BurstSize = 4096;
    private const int ChunksPerBurst = BurstSize / PacketSize;

    private const int WriteTimeout = 1000;
    private const int ReadTimeout = 1000;

    private readonly UsbDevice usbDevice;

    private readonly UsbEndpointReader reader;

    private readonly UsbEndpointWriter writer;

    private byte[] writeBuffer;

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
        writer = usbDevice.OpenEndpointWriter(WriteEndpointID.Ep09);

        writeBuffer = ArrayPool<byte>.Shared.Rent(BurstSize);
        readBuffer = ArrayPool<byte>.Shared.Rent(PacketSize);
    }

    public void Dispose()
    {
        if (usbDevice.IsOpen)
        {
            usbDevice.ReleaseInterface(0);
            usbDevice.Close();
        }

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

    // --------------------------------------------------------------------------------
    // Helper
    // --------------------------------------------------------------------------------

    private bool SendData(int length)
    {
        writer.Write(writeBuffer.AsSpan(0, length), WriteTimeout, out var transferLength);
        return transferLength == length;
    }

    private bool ReceiveResponse()
    {
        reader.Read(readBuffer.AsSpan(0, PacketSize), ReadTimeout, out var transferLength);
        if (transferLength != PacketSize)
        {
            return false;
        }

        // resp[0] == 0x03 && resp[1] == 0xFF && resp[8] == 0x01 is success
        return (readBuffer[0] == 0x03) && (readBuffer[1] == 0xFF) && (readBuffer[8] == 0x01);
    }

    // --------------------------------------------------------------------------------
    // Command
    // --------------------------------------------------------------------------------

    public DeviceInfo? Handshake()
    {
        var request = writeBuffer.AsSpan(0, HandshakeSize);
        request.Clear();
        request[0] = 0x02;
        request[1] = 0xFF;
        request[8] = 0x01;
        if (!(SendData(HandshakeSize) && ReceiveResponse()))
        {
            return null;
        }

        var pm = (PanelType)(64 + (readBuffer[20] <= 3 ? 1 : readBuffer[20]));
        var sub = (byte)(readBuffer[22] + 1);
        return new DeviceInfo(pm, sub);
    }

    public bool DrawJpeg(ReadOnlySpan<byte> imageBytes)
    {
        var chunks = (imageBytes.Length / ChunkDataSize) + 1;
        var paddedChunks = (chunks + ChunkAlignment - 1) / ChunkAlignment * ChunkAlignment;

        for (var first = 0; first < paddedChunks; first += ChunksPerBurst)
        {
            var count = Math.Min(ChunksPerBurst, paddedChunks - first);
            var burst = writeBuffer.AsSpan(0, count * PacketSize);
            burst.Clear();

            // Padding chunks are left as zero
            for (var i = 0; (i < count) && (first + i < chunks); i++)
            {
                var index = first + i;
                var offset = index * ChunkDataSize;
                var length = Math.Min(ChunkDataSize, imageBytes.Length - offset);

                var chunk = burst.Slice(i * PacketSize, PacketSize);
                chunk[0] = 0x01;
                chunk[1] = 0xFF;
                BinaryPrimitives.WriteInt32LittleEndian(chunk[2..], imageBytes.Length);
                BinaryPrimitives.WriteUInt16LittleEndian(chunk[6..], (ushort)length);
                chunk[8] = 0x01;
                BinaryPrimitives.WriteUInt16LittleEndian(chunk[9..], (ushort)chunks);
                BinaryPrimitives.WriteUInt16LittleEndian(chunk[11..], (ushort)index);
                imageBytes.Slice(offset, length).CopyTo(chunk[ChunkHeaderSize..]);
            }

            if (!SendData(burst.Length))
            {
                return false;
            }
        }

        return ReceiveResponse();
    }
}
