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
        using var buffer = new ByteBuffer(6);
        buffer.Skip(5);
        buffer.Write(command);
        port.Write(buffer.Buffer, 0, buffer.WrittenCount);
    }

    private void WriteCommand(byte command, int level)
    {
        using var buffer = new ByteBuffer(6);
        buffer.Write((byte)(level >> 2));
        buffer.Write((byte)((level & 3) << 6));
        buffer.Skip(3);
        buffer.Write(command);
        port.Write(buffer.Buffer, 0, buffer.WrittenCount);
    }

    private void WriteCommand(byte command, byte orientation, int width, int height)
    {
        using var buffer = new ByteBuffer(11);
        buffer.Skip(5);
        buffer.Write(command);
        buffer.Write((byte)(orientation + 100));
        buffer.Write((byte)(width >> 8));
        buffer.Write((byte)(width & 255));
        buffer.Write((byte)(height >> 8));
        buffer.Write((byte)(height & 255));
        port.Write(buffer.Buffer, 0, buffer.WrittenCount);
    }

    private void WriteCommand(byte command, int x, int y, int width, int height, byte[] data)
    {
        var ex = x + width - 1;
        var ey = y + height - 1;

        using var buffer = new ByteBuffer(6);
        buffer.Write((byte)(x >> 2));
        buffer.Write((byte)(((x & 3) << 6) + (y >> 4)));
        buffer.Write((byte)(((y & 15) << 4) + (ex >> 6)));
        buffer.Write((byte)(((ex & 63) << 2) + (ey >> 8)));
        buffer.Write((byte)(ey & 255));
        buffer.Write(command);
        port.Write(buffer.Buffer, 0, buffer.WrittenCount);
        port.Write(data, 0, width * height * 2);
    }

    public void Reset() => WriteCommand(101);

    public void Clear() => WriteCommand(102);

    public void ScreenOff() => WriteCommand(108);

    public void ScreenOn() => WriteCommand(109);

    public void SetBrightness(int level) => WriteCommand(110, 255 - level);

    public void SetOrientation(Orientation orientation, int width, int height) =>
        WriteCommand(121, (byte)orientation, width, height);

    public void DisplayBitmap(int x, int y, int width, int height, byte[] bitmap) =>
        WriteCommand(197, x, y, width, height, bitmap);
}
