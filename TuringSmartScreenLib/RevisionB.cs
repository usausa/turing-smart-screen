namespace TuringSmartScreenLib;

using System.IO.Ports;

public sealed class TuringSmartScreenRevisionB : IDisposable
{
    public enum Orientation : byte
    {
        Portrait = 0,
        Landscape = 1
    }

    private readonly SerialPort port;

    public byte Version { get; private set; }

    public TuringSmartScreenRevisionB(string name)
    {
        port = new SerialPort(name)
        {
            DtrEnable = true,
            RtsEnable = true,
            ReadTimeout = 1000,
            BaudRate = 115200,
            DataBits = 8,
            StopBits = StopBits.One,
            Parity = Parity.None
        };
    }

    public void Close()
    {
        if (port.IsOpen)
        {
            port.Close();
        }
    }

    public void Dispose()
    {
        Close();
    }

    public void Open()
    {
        port.Open();
        port.DiscardInBuffer();
        port.DiscardOutBuffer();

        var command = (Span<byte>)[0xCA, (byte)'H', (byte)'E', (byte)'L', (byte)'L', (byte)'O', 0, 0, 0, 0xCA];
        using var request = new ByteBuffer(command.Length);
        command.CopyTo(request.GetSpan());
        request.Advance(command.Length);

        port.Write(request.Buffer, 0, request.WrittenCount);

        using var response = new ByteBuffer(10);
        var read = ReadResponse(response.Buffer, 10);
        var buffer = response.Buffer;
        if ((read == 10) &&
            (buffer[0] == 0xCA) &&
            (buffer[1] == (byte)'H') &&
            (buffer[2] == (byte)'E') &&
            (buffer[3] == (byte)'L') &&
            (buffer[4] == (byte)'L') &&
            (buffer[5] == (byte)'O') &&
            (buffer[9] == 0xCA))
        {
            if (buffer[6] == 0x0A)
            {
                Version = buffer[7];
            }
        }

        port.DiscardInBuffer();
    }

    private int ReadResponse(byte[] response, int length)
    {
        var offset = 0;
        while (offset < length)
        {
            var read = port.Read(response, offset, length - offset);
            if (read <= 0)
            {
                break;
            }

            offset += read;
        }

        return offset;
    }

    private void WriteCommand(byte command, byte value)
    {
        const int commandSize = 10;
        using var buffer = new ByteBuffer(commandSize);
        var span = buffer.GetSpan();
        span[0] = command;
        span[1] = value;
        span[9] = command;
        buffer.Advance(commandSize);

        port.Write(buffer.Buffer, 0, buffer.WrittenCount);
    }

    private void WriteCommand(byte command, int x, int y, int width, int height, byte[] data)
    {
        var ex = x + width - 1;
        var ey = y + height - 1;

        const int commandSize = 10;
        using var buffer = new ByteBuffer(commandSize);
        var span = buffer.GetSpan();
        span[0] = command;
        span[1] = (byte)((x >> 8) & 0xFF);
        span[2] = (byte)(x & 0xFF);
        span[3] = (byte)((y >> 8) & 0xFF);
        span[4] = (byte)(y & 0xFF);
        span[5] = (byte)((ex >> 8) & 0xFF);
        span[6] = (byte)(ex & 0xFF);
        span[7] = (byte)((ey >> 8) & 0xFF);
        span[8] = (byte)(ey & 0xFF);
        span[9] = command;
        buffer.Advance(commandSize);

        port.Write(buffer.Buffer, 0, buffer.WrittenCount);
        port.Write(data, 0, width * height * 2);
    }

    public void SetBrightness(byte level) => WriteCommand(0xCE, level);

    public void SetOrientation(Orientation orientation) => WriteCommand(0xCB, (byte)orientation);

    public void DisplayBitmap(int x, int y, int width, int height, byte[] bitmap) =>
        WriteCommand(0xCC, x, y, width, height, bitmap);
}
