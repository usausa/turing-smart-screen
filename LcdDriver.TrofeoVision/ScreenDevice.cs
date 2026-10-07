namespace LcdDriver.TrofeoVision;

using System.Buffers.Binary;

using HidSharp;

public sealed class ScreenDevice : IDisposable
{
    // HID report: Report ID (1 byte) + Data (512 bytes)
    private const int HidReportSize = 513;
    private const int DataPerPacket = 512;
    private const int HeaderSize = 20;
    private const byte ReportId = 0x00;

    // Protocol command/compression type
    private const byte CommandHandshake = 0x01;
    private const byte CommandImage = 0x02;
    private const byte CompressionJpeg = 0x02;

    private const int MaxResponseReports = 8;

    // Protocol header magic bytes
    private static readonly byte[] HeaderMagic = [0xDA, 0xDB, 0xDC, 0xDD];

    private readonly HidStream stream;

    public int Width { get; }

    public int Height { get; }

    // --------------------------------------------------------------------------------
    // Constructor
    // --------------------------------------------------------------------------------

    public ScreenDevice(HidDevice hidDevice, int width = 1280, int height = 480)
    {
        stream = hidDevice.Open();
        stream.WriteTimeout = 5000;
        stream.ReadTimeout = 5000;

        Width = width;
        Height = height;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        stream.Dispose();
    }

    // --------------------------------------------------------------------------------
    // Command
    // --------------------------------------------------------------------------------

    public DeviceInfo? Handshake()
    {
        Span<byte> packet = stackalloc byte[HidReportSize];

        packet.Clear();
        packet[0] = ReportId;

        var header = packet.Slice(1, HeaderSize);
        HeaderMagic.CopyTo(header);
        header[12] = CommandHandshake;

        stream.Write(packet);

        try
        {
            for (var i = 0; i < MaxResponseReports; i++)
            {
                var read = stream.Read(packet);
                if (read <= HeaderSize)
                {
                    continue;
                }

                var response = packet[1..read];
                if (response[..4].SequenceEqual(HeaderMagic) && (response[12] == CommandHandshake))
                {
                    return new DeviceInfo((PanelType)response[5], response[4]);
                }
            }
        }
        catch (TimeoutException)
        {
            return null;
        }

        return null;
    }

    public void DrawJpeg(ReadOnlySpan<byte> imageBytes) =>
        SendImageData(CompressionJpeg, imageBytes);

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
        BinaryPrimitives.WriteUInt16LittleEndian(header[8..], (ushort)Width);
        BinaryPrimitives.WriteUInt16LittleEndian(header[10..], (ushort)Height);
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
