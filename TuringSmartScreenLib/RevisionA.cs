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

    public void Close()
    {
        if (port.IsOpen)
        {
            port.Close();
        }
    }

    public void Open()
    {
        port.Open();
        port.DiscardInBuffer();
        port.DiscardOutBuffer();
    }

    private void WriteCommand(byte command)
    {
        const int commandLength = 6;
        using var buffer = new ByteBuffer(commandLength);
        var span = buffer.GetSpan();
        span[5] = command;
        buffer.Advance(commandLength);

        port.Write(buffer.Buffer, 0, buffer.WrittenCount);
    }

    private void WriteCommand(byte command, int level)
    {
        const int commandLength = 6;
        using var buffer = new ByteBuffer(commandLength);
        var span = buffer.GetSpan();
        span[0] = (byte)(level >> 2);
        span[1] = (byte)((level & 3) << 6);
        span[5] = command;
        buffer.Advance(commandLength);

        port.Write(buffer.Buffer, 0, buffer.WrittenCount);
    }

    private void WriteCommand(byte command, byte orientation, int width, int height)
    {
        const int commandLength = 11;
        using var buffer = new ByteBuffer(commandLength);
        var span = buffer.GetSpan();
        span[5] = command;
        span[6] = (byte)(orientation + 100);
        span[7] = (byte)(width >> 8);
        span[8] = (byte)(width & 255);
        span[9] = (byte)(height >> 8);
        span[10] = (byte)(height & 255);
        buffer.Advance(commandLength);

        port.Write(buffer.Buffer, 0, buffer.WrittenCount);
    }

    private void WriteCommand(byte command, int x, int y, int width, int height, byte[] data)
    {
        var ex = x + width - 1;
        var ey = y + height - 1;

        const int commandLength = 6;
        using var buffer = new ByteBuffer(commandLength);
        var span = buffer.GetSpan();
        span[0] = (byte)(x >> 2);
        span[1] = (byte)(((x & 3) << 6) + (y >> 4));
        span[2] = (byte)(((y & 15) << 4) + (ex >> 6));
        span[3] = (byte)(((ex & 63) << 2) + (ey >> 8));
        span[4] = (byte)(ey & 255);
        span[5] = command;
        buffer.Advance(commandLength);

        port.Write(buffer.Buffer, 0, buffer.WrittenCount);
        port.Write(data, 0, width * height * 2);
    }

    public void Reset() => WriteCommand(101);

    public void Clear() => WriteCommand(102);

    public void ScreenOff() => WriteCommand(108);

    public void ScreenOn() => WriteCommand(109);

    public void SetBrightness(int level) => WriteCommand(110, level);

    public void SetOrientation(Orientation orientation, int width, int height) =>
        WriteCommand(121, (byte)orientation, width, height);

    public void DisplayBitmap(int x, int y, int width, int height, byte[] bitmap) =>
        WriteCommand(197, x, y, width, height, bitmap);
}
