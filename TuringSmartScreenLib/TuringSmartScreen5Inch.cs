namespace TuringSmartScreenLib;

using System;
using System.IO.Ports;

public sealed class TuringSmartScreen5Inch : IDisposable
{

    public static readonly byte[] GetDevice = { 0x01, 0xef, 0x69, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0xc5, 0xd3 };
    public static readonly byte[] UpdateIMG = { 0xcc, 0xef, 0x69, 0x00, 0x00 };
    public static readonly byte[] StopVideo = { 0x79, 0xef, 0x69, 0x00, 0x00, 0x00, 0x01 };
    public static readonly byte[] DisplayFullIMAGE = { 0xc8, 0xef, 0x69, 0x00, 0x17, 0x70 };
    public static readonly byte[] QueryRenderStatus = { 0xcf, 0xef, 0x69, 0x00, 0x00, 0x00, 0x01 };
    public static readonly byte[] PreImgCMD = { 0x2c };
    public static readonly byte[] MediaStop = { 0x96, 0xef, 0x69, 0x00, 0x00, 0x00, 0x01 };
    public static readonly byte[] PostImgCMD = { 0x86, 0xef, 0x69, 0x00, 0x00, 0x00, 0x01 };
    public static readonly byte[] OnExit = { 0x87, 0xef, 0x69, 0x00, 0x00, 0x00, 0x01 };

    public enum Orientation : byte
    {
        Portrait = 0,
        ReversePortrait = 1,
        Landscape = 2,
        ReverseLandscape = 3
    }

    private readonly SerialPort port;
    private Orientation currentOrientation;
    private string? currentResponse;
    private AutoResetEvent dataReceivedEvent = new(false);

    public TuringSmartScreen5Inch(string name)
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
            Console.WriteLine($"Received:'{currentResponse}'");
            dataReceivedEvent.Set();
        };
        Console.WriteLine("GetDevice");
        WriteCommand(GetDevice);
        var resp = ReadResponse();
        if (resp is null || !resp.StartsWith("chs_5inch"))
        {
            throw new Exception($"Invalid response '{resp}' received from 5 Inch Turing Screen on init");
        }
        Console.WriteLine("StopVideo");
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
        if (!received) throw new TimeoutException("No answer from device");
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
    public void DisplayBitmap(int x, int y, int width, int height, IScreenBuffer bitmap)
    {
        if (bitmap.Length == 0)
        {
            ClearScreen();
        }
        else
        {
            if (bitmap.Length < width * height * 2)
            {
                DisplayPartialImage(x, y, width, height, bitmap);
            }
            else
            {
                if (x != 0 || y != 0 || width != WIDTH || height != HEIGHT)
                {
                    throw new Exception("Invalid parameters for full screen image");
                }
                WriteCommand(PreImgCMD, 0x2c);
                WriteCommand(DisplayFullIMAGE);
                var blockSize = 249;
                var currentPosition = 0;
                while (currentPosition < bitmap.Length)
                {
                    var block = bitmap.Skip(currentPosition).Take(blockSize).ToArray();
                    WriteCommand(block);
                    currentPosition += blockSize;
                }
                WriteCommand(PostImgCMD);
            }
        }
    }

    private void ClearScreen() {
    }

    private void DisplayPartialImage(int x, int y, int width, int height, byte[] bitmap)
    {        
        var msg = new List<byte>();
        var bitmapPosition = 0;
        for (int h = 0; h < height; h++)
        {
            //MSG += f'{((x + h) * 800) + y:06x}'  + f'{width:04x}'   
            int v = ((x + h) * 800) +y;
            var array4 = BitConverter.GetBytes(v).Reverse();
            msg.Add(0);
            msg.Add(0);
            msg.AddRange(array4);
            msg.AddRange(BitConverter.GetBytes(width).Reverse());
            for (int  w = 0; w < width; w++)
            {
                msg.Add(bitmap[bitmapPosition++]);
                msg.Add(bitmap[bitmapPosition++]);
                msg.Add(bitmap[bitmapPosition++]);
            }
            //UPD_Size = f'{int((len(MSG) / 2) + 2):04x}' #The +2 is for the "ef69" that will be added later
            //if len(MSG) > 500: MSG = '00'.join(MSG[i:i + 498] for i in range(0, len(MSG), 498))
        }
        var updSize = (msg.Count / 2) + 2;
        if (msg.Count > 500)
        {
            for (int s = 0; s < (msg.Count / 500); s++)
            {
                msg.Insert((s + 1) * 498, 0);
            }
        }
        msg.Add(0xef);
        msg.Add(0x69);
        var cmd = new List<byte>(UpdateIMG);
        cmd.AddRange(BitConverter.GetBytes(updSize).Reverse());
        WriteCommand(cmd);
        WriteCommand(msg);

    }
}
