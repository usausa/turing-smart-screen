namespace TuringSmartScreenLib;

using System;
using System.IO.Ports;

public sealed class TuringSmartScreen5Inch : IDisposable
{

    //see https://github.com/mathoudebine/turing-smart-screen-python/blob/main/library/lcd/lcd_comm_rev_c.py for reference
    public static readonly byte[] GetDevice = { 0x01, 0xef, 0x69, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0xc5, 0xd3 };
    public static readonly byte[] UpdateIMG = { 0xcc, 0xef, 0x69, 0x00, 0x00 };
    public static readonly byte[] StopVideo = { 0x79, 0xef, 0x69, 0x00, 0x00, 0x00, 0x01 };
    public static readonly byte[] DisplayFullIMAGE = { 0xc8, 0xef, 0x69, 0x00, 0x17, 0x70 };
    public static readonly byte[] QueryRenderStatus = { 0xcf, 0xef, 0x69, 0x00, 0x00, 0x00, 0x01 };
    public static readonly byte[] StartDisplay = { 0x2c };
    public static readonly byte[] MediaStop = { 0x96, 0xef, 0x69, 0x00, 0x00, 0x00, 0x01 };
    public static readonly byte[] PreUpdateBitmap = { 0x86, 0xef, 0x69, 0x00, 0x00, 0x00, 0x01 };
    public static readonly byte[] OnExit = { 0x87, 0xef, 0x69, 0x00, 0x00, 0x00, 0x01 };
    public static readonly byte[] Restart = { 0x84, 0xef, 0x69, 0x00, 0x00, 0x00, 0x01 };
    public enum Orientation : byte
    {
        Portrait = 0,
        ReversePortrait = 1,
        Landscape = 2,
        ReverseLandscape = 3
    }

    private readonly SerialPort port;
    private readonly bool debugOutput;
    private Orientation currentOrientation;
    private string? currentResponse;
    private AutoResetEvent dataReceivedEvent = new(false);

    public TuringSmartScreen5Inch(string name, bool debugOutput = false)
    {
        port = new SerialPort(name)
        {
            DtrEnable = true,
            RtsEnable = true,            
            ReadTimeout = 1000,
            WriteTimeout = 1000,
            BaudRate = 115200,
            StopBits = StopBits.One,
            Parity = Parity.None
        };
        this.debugOutput = debugOutput;
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
        port.DataReceived += (sender, e) =>
        {
            currentResponse = port.ReadExisting();
            if (debugOutput)
            {
                Console.WriteLine($"Received:'{currentResponse}'");
            }

            dataReceivedEvent.Set();
        };
        WriteCommand(GetDevice);
        var resp = ReadResponse();
        if (resp is null || !resp.StartsWith("chs_5inch"))
        {
            throw new Exception($"Invalid response '{resp}' received from 5 Inch Turing Screen on init");
        }
        WriteCommand(StopVideo);
    }

    public void Close()
    {
        if (port.IsOpen)
        {
            port.Close();
        }
    }

    private void WriteCommand(IEnumerable<byte> command, byte padValue = 0x00)
    {
        var msgLength = command.Count();
        if (msgLength % 250 != 0)
        {
            var l = command.ToList();
            for (var i = 0; i < ((250 * Math.Ceiling(1.0 * msgLength / 250)) - msgLength); i++)
            {
                l.Add(padValue);
            }
            command = l;
        }
        port.Write(command.ToArray(), 0, command.Count());
    }


    private string? ReadResponse()
    {
        var received = dataReceivedEvent.WaitOne(2000);
        if (!received)
        {
            throw new TimeoutException("No answer from device");
        }

        return currentResponse;
    }

    private void WriteCommand(byte command)
    {
        throw new NotImplementedException();

        var buffer = new byte[6];
        buffer[5] = command;
        port.Write(buffer, 0, buffer.Length);
    }
    public void Reset() => WriteCommand(101);

    public void Clear() => WriteCommand(102);

    public void ScreenOff() => WriteCommand(108);

    public void ScreenOn() => WriteCommand(109);

    public void SetBrightness(int level)
    {
        var cmd = new List<byte> { 0x7b, 0xef, 0x69, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00 };
        cmd.Add((byte)level);
        WriteCommand(cmd.ToArray());
    }

    public void SetOrientation(Orientation orientation, int width, int height)
    {
        currentOrientation = orientation;
    }

    public const int HEIGHT = 480;
    public const int WIDTH = 800;
    public void DisplayBitmap(int x, int y, int width, int height, IScreenBuffer buffer)
    {
        var cBuffer = (TuringSmartScreenBuffer5Inch)buffer;
        if (cBuffer.IsEmpty())
        {
            ClearScreen();
        }
        else
        {
            var isFullScreen = height == HEIGHT && width == WIDTH;
            var isRotated = width == HEIGHT && height == WIDTH;
            if (!isFullScreen)
            {
                DisplayPartialImage(x, y, width, height, cBuffer);
                WriteCommand(QueryRenderStatus);
                var resp = ReadResponse();
                if (resp?.Contains("needReSend:1") ??false)
                {
                    DisplayPartialImage(x, y, width, height, cBuffer);
                    WriteCommand(QueryRenderStatus);
                }
            }
            else
            {
                if (x != 0 || y != 0 || width != WIDTH || height != HEIGHT)
                {
                    throw new Exception("Invalid parameters for full screen image");
                }
                WriteCommand(StartDisplay, 0x2c);
                WriteCommand(DisplayFullIMAGE);
                var blockSize = 249;
                var currentPosition = 0;
                while (currentPosition < cBuffer.Length)
                {
                    var block = cBuffer.img_buffer.Skip(currentPosition).Take(blockSize).ToArray();
                    WriteCommand(block);
                    currentPosition += blockSize;
                }
                WriteCommand(PreUpdateBitmap);
                ReadResponse();
                WriteCommand(QueryRenderStatus);
                ReadResponse();

            }
        }
    }

    private void ClearScreen() {
    }

    private static byte[] ConvertAndPad(int number, int fixedLength)
    {
        byte[] byteArray = BitConverter.GetBytes(number);
        // Apply zero padding if necessary
        Array.Resize(ref byteArray, fixedLength);
        Array.Reverse(byteArray);
        return byteArray;
    }

    internal static (byte[], byte[]) GeneratePartialUpdateFromBuffer(int height, int width, int x, int y, byte[] image, int channelCount = 4)
    {
        var data = new List<byte>();

        for (int h = 0; h < height; h++)
        {
            data.AddRange(ConvertAndPad(((x + h) * 800) + y, 3));
            data.AddRange(ConvertAndPad(width, 2));
            for (int w = 0; w < width; w++)
            {
                int indexR = ((h * width) + w) * channelCount;
                data.Add(image[indexR]);
                int indexG = ((h * width) + w) * channelCount + 1;
                data.Add(image[indexG]);
                int indexB = ((h * width) + w) * channelCount + 2;
                data.Add(image[indexB]);

            }
        }
        var updSize = ConvertAndPad(data.Count + 2, 2);
        if (data.Count > 250)
        {
            var newMsg = new List<byte> { };
            for (var i = 0; i <= data.Count; i++)
            {
                if (i % 249 == 0)
                {
                    newMsg.AddRange(data.GetRange(i, Math.Min(249, data.Count - i)));
                    newMsg.Add(0);
                }
            }
            //remove last padding 0
            newMsg.RemoveAt(newMsg.Count - 1);
            data = newMsg;
        }

        data.Add(0xef);
        data.Add(0x69);
        return (data.ToArray(), updSize);
    }

    private void DisplayPartialImage(int x, int y, int width, int height, TuringSmartScreenBuffer5Inch buffer)
    {
        var (data, updSize) = GeneratePartialUpdateFromBuffer(height, width, x, y, buffer.img_buffer);
        var cmd = new List<byte>(UpdateIMG);
        cmd.AddRange(updSize);
        WriteCommand(cmd);
        WriteCommand(data);

    }
}
