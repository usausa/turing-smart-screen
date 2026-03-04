namespace LcdDriver.TrofeoVision;

using System.Buffers.Binary;

using HidSharp;

public sealed class ScreenDevice : IDisposable
{
    public const ushort VendorId = 0x0416;
    public const ushort ProductId = 0x5302;

    public const int Width = 1280;
    public const int Height = 480;

    // HID report: Report ID (1 byte) + Data (512 bytes)
    private const int HidReportSize = 513;
    private const int DataPerPacket = 512;
    private const int HeaderSize = 20;
    private const byte ReportId = 0x00;

    // Protocol header magic bytes
    private static readonly byte[] HeaderMagic = [0xDA, 0xDB, 0xDC, 0xDD];

    // Protocol command/compression type
    private const byte CommandImage = 0x02;
    private const byte CompressionJpeg = 0x02;
    private const byte CompressionRgb565 = 0x01;

    private readonly HidStream stream;

    // --------------------------------------------------------------------------------
    // Constructor
    // --------------------------------------------------------------------------------

    public ScreenDevice(HidDevice hidDevice)
    {
        stream = hidDevice.Open();
        stream.WriteTimeout = 5000;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        stream.Dispose();
    }

    // --------------------------------------------------------------------------------
    // Command
    // --------------------------------------------------------------------------------

    public void DrawJpeg(ReadOnlySpan<byte> jpegData) =>
        SendImageData(CompressionJpeg, jpegData);

    public void DrawRgb565(ReadOnlySpan<byte> rgb565Data) =>
        SendImageData(CompressionRgb565, rgb565Data);

    // --------------------------------------------------------------------------------
    // Helper
    // --------------------------------------------------------------------------------

    private void SendImageData(byte compressionType, ReadOnlySpan<byte> imageBytes)
    {
        Span<byte> packet = stackalloc byte[HidReportSize];

        packet.Clear();
        packet[0] = ReportId;

        // Build protocol header directly in packet
        var header = packet.Slice(1, HeaderSize);
        HeaderMagic.CopyTo(header);
        header[4] = CommandImage;
        BinaryPrimitives.WriteUInt16LittleEndian(header[8..], Width);
        BinaryPrimitives.WriteUInt16LittleEndian(header[10..], Height);
        header[12] = compressionType;
        BinaryPrimitives.WriteInt32LittleEndian(header[16..], imageBytes.Length);

        // Send data in chunks of 512 bytes (HID report size - 1 byte for Report ID)
        var offset = 0;
        var length = Math.Min(imageBytes.Length, DataPerPacket - HeaderSize);
        if (length > 0)
        {
            imageBytes[..length].CopyTo(packet[(1 + HeaderSize)..]);
            offset = length;
        }

        stream.Write(packet);

        // Send left data
        while (offset < imageBytes.Length)
        {
            packet.Clear();
            packet[0] = ReportId;

            length = Math.Min(imageBytes.Length - offset, DataPerPacket);
            imageBytes.Slice(offset, length).CopyTo(packet[1..]);

            stream.Write(packet);
            offset += length;
        }
    }
}
