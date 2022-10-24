namespace TuringSmartScreenLib;

using System.IO.Ports;

public sealed class TuringSmartScreenRevisionA : IDisposable
{
    public enum Orientation : byte
    {
        Portrait = 0,
        ReversePortrait = 1,
        Landscape = 2,
        ReverseLandscape = 3
    }

    private readonly SerialPort port;

    public TuringSmartScreenRevisionA(string name)
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
        port.Write(buffer, 0, buffer.Length);
    }

    private void WriteCommand(byte command, int level)
    {
        var buffer = new byte[6];
        buffer[0] = (byte)(level >> 2);
        buffer[1] = (byte)((level & 3) << 6);
        buffer[5] = command;
        port.Write(buffer, 0, buffer.Length);
    }

    private void WriteCommand(byte command, byte orientation)
    {
        var buffer = new byte[7];
        buffer[5] = command;
        buffer[6] = orientation;
        port.Write(buffer, 0, buffer.Length);
    }

    private void WriteCommand(byte command, int x, int y, int width, int height, byte[] data)
    {
        var ex = x + width - 1;
        var ey = y + height - 1;
        var buffer = new byte[6];
        buffer[0] = (byte)(x >> 2);
        buffer[1] = (byte)(((x & 3) << 6) + (y >> 4));
        buffer[2] = (byte)(((y & 15) << 4) + (ex >> 6));
        buffer[3] = (byte)(((ex & 63) << 2) + (ey >> 8));
        buffer[4] = (byte)(ey & 255);
        buffer[5] = command;
        port.Write(buffer, 0, buffer.Length);
        port.Write(data, 0, width * height * 2);
    }

    public void Reset() => WriteCommand(101);

    public void Clear() => WriteCommand(102);

    public void ScreenOff() => WriteCommand(108);

    public void ScreenOn() => WriteCommand(109);

    public void SetBrightness(int level) => WriteCommand(110, level);

    public void SetOrientation(Orientation orientation) => WriteCommand(121, (byte)orientation);

    public void DisplayBitmap(int x, int y, int width, int height, byte[] bitmap) =>
        WriteCommand(197, x, y, width, height, bitmap);
}
