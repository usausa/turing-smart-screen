namespace TuringSmartScreenLib;

using System.IO.Ports;

public sealed class TuringSmartScreen : IDisposable
{
    private readonly SerialPort port;

    public TuringSmartScreen(string name)
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
    }

    public void Close()
    {
        if (port.IsOpen)
        {
            port.Close();
        }
    }

    private void WriteCommand(byte command)
    {
        var buffer = new byte[6];
        buffer[5] = command;
        port.Write(buffer, 0, 6);
    }

    private void WriteCommand(byte command, int level)
    {
        var buffer = new byte[6];
        buffer[0] = (byte)(level >> 2);
        buffer[1] = (byte)((level & 3) << 6);
        buffer[5] = command;
        port.Write(buffer, 0, 6);
    }

    private void WriteCommand(byte command, int x, int y, int ex, int ey, byte[] data)
    {
        var buffer = new byte[6];
        buffer[0] = (byte)(x >> 2);
        buffer[1] = (byte)(((x & 3) << 6) + (y >> 4));
        buffer[2] = (byte)(((y & 15) << 4) + (ex >> 6));
        buffer[3] = (byte)(((ex & 63) << 2) + (ey >> 8));
        buffer[4] = (byte)(ey & 255);
        buffer[5] = command;
        port.Write(buffer, 0, 6);
        port.Write(data, 0, data.Length);
    }

    public void Reset() => WriteCommand(101);

    public void Clear() => WriteCommand(102);

    public void ScreenOff() => WriteCommand(108);

    public void ScreenOn() => WriteCommand(109);

    public void SetBrightness(int level) => WriteCommand(110, level);

    public void DisplayBitmap(int x, int y, int width, int height, byte[] bitmap) =>
        WriteCommand(197, x, y, x + width - 1, y + height - 1, bitmap);
}
