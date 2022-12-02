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

    public void Dispose()
    {
        Close();
    }

    public void Open()
    {
        port.Open();
        port.DiscardInBuffer();
        port.DiscardOutBuffer();

        var hello = new byte[] { 0xCA, (byte)'H', (byte)'E', (byte)'L', (byte)'L', (byte)'O', 0, 0, 0, 0xCA };
        port.Write(hello, 0, hello.Length);

        var response = new byte[10];
        var read = port.Read(response, 0, response.Length);
        if ((read == 10) &&
            (response[0] == 0xCA) &&
            (response[1] == (byte)'H') &&
            (response[2] == (byte)'E') &&
            (response[3] == (byte)'L') &&
            (response[4] == (byte)'L') &&
            (response[5] == (byte)'O') &&
            (response[9] == 0xCA))
        {
            if (response[6] == 0x0A)
            {
                Version = response[7];
            }
        }
    }

    public void Close()
    {
        if (port.IsOpen)
        {
            port.Close();
        }
    }

    private void WriteCommand(byte command, byte orientation, int width, int height)
    {
        int x = 0;
        int y = 0;
        int ex = 0;
        int ey = 0;
        var buffer = new byte[11];
        buffer[0] = (byte)(x >> 2);
        buffer[1] = (byte)(((x & 3) << 6) + (y >> 4));
        buffer[2] = (byte)(((y & 15) << 4) + (ex >> 6));
        buffer[3] = (byte)(((ex & 63) << 2) + (ey >> 8));
        buffer[4] = (byte)(ey & 255);
        buffer[5] = command;
        buffer[6] = orientation;
        buffer[6] = (byte)(orientation + 100);
        buffer[7] = (byte)(width >> 8);
        buffer[8] = (byte)(width & 255);
        buffer[9] = (byte)(height >> 8);
        buffer[10] = (byte)(height & 255);
        port.Write(buffer, 0, buffer.Length);
    }

    private void WriteCommand(byte command, int x, int y, int width, int height, byte[] data)
    {
        var ex = x + width - 1;
        var ey = y + height - 1;
        var buffer = new byte[10];
        buffer[0] = command;
        buffer[1] = (byte)((x >> 8) & 0xFF);
        buffer[2] = (byte)(x & 0xFF);
        buffer[3] = (byte)((y >> 8) & 0xFF);
        buffer[4] = (byte)(y & 0xFF);
        buffer[5] = (byte)((ex >> 8) & 0xFF);
        buffer[6] = (byte)(ex & 0xFF);
        buffer[7] = (byte)((ey >> 8) & 0xFF);
        buffer[8] = (byte)(ey & 0xFF);
        buffer[9] = command;
        port.Write(buffer, 0, buffer.Length);
        port.Write(data, 0, width * height * 2);
    }

    public void SetBrightness(byte level) => WriteCommand(0xCE, level);

    public void SetOrientation(Orientation orientation, int width, int height) => WriteCommand(0xCB, (byte)orientation, width, height);

    public void DisplayBitmap(int x, int y, int width, int height, byte[] bitmap) =>
        WriteCommand(0xCC, x, y, width, height, bitmap);
}
