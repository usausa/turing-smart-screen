#pragma warning disable CA1303
namespace Example;

using HidSharp;

using LibUsbDotNet;
using LibUsbDotNet.Main;

using SkiaSharp;

using Smart.CommandLine.Hosting;

using TuringSmartScreenLib;
using TuringSmartScreenLib.Helpers.SkiaSharp;

public static class CommandBuilderExtensions
{
    public static void AddCommands(this ICommandBuilder commands)
    {
        commands.AddCommand<Tss35Command>();
        commands.AddCommand<Tss5Command>();
        commands.AddCommand<Tss8Command>(tss8 =>
        {
            tss8.AddSubCommand<Tss8StorageCommand>();
        });
        commands.AddCommand<Tss8UsbCommand>();
        commands.AddCommand<TrofeoCommand>();
    }
}

//--------------------------------------------------------------------------------
// Trofeo Vision
//--------------------------------------------------------------------------------
[Command("trofeo", "Trofeo Vision")]
public sealed class TrofeoCommand : ICommandHandler
{
    public async ValueTask ExecuteAsync(CommandContext context)
    {
        var device = DeviceList.Local
            .GetHidDevices(LcdDriver.TrofeoVision.ScreenDevice.VendorId, LcdDriver.TrofeoVision.ScreenDevice.ProductId)
            .FirstOrDefault();
        if (device is null)
        {
            Console.WriteLine("Device not found.");
            return;
        }

        using var screen = new LcdDriver.TrofeoVision.ScreenDevice(device);

        var jpegBytes = await File.ReadAllBytesAsync("image-1280x480.jpg");

        var interval = TimeSpan.FromSeconds(1);
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            // ReSharper disable once AccessToDisposedClosure
            cts.Cancel();
        };

        while (!cts.Token.IsCancellationRequested)
        {
            screen.DrawJpeg(jpegBytes);

            try
            {
                await Task.Delay(interval, cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}

//--------------------------------------------------------------------------------
// Turing Smart Screen 8.8 USB revision
//--------------------------------------------------------------------------------
[Command("tss8usb", "Turing Smart Screen 8.8inch USB")]
public sealed class Tss8UsbCommand : ICommandHandler
{
    public async ValueTask ExecuteAsync(CommandContext context)
    {
        var finder = new UsbDeviceFinder(0x1CBE, 0x0088);
        using var device = UsbDevice.OpenUsbDevice(finder);
        if (device is null)
        {
            Console.WriteLine("Device not found.");
            return;
        }

        using var screen = new LcdDriver.TuringSmartScreen.ScreenDevice(device);

        if (!screen.Sync())
        {
            Console.WriteLine("Sync failed.");
        }

        if (!screen.SetOrientation(LcdDriver.TuringSmartScreen.ScreenOrientation.Portrait))
        {
            Console.WriteLine("Set orientation failed.");
        }

        if (!screen.DrawJpeg(await File.ReadAllBytesAsync("image-480x1920.jpg")))
        {
            Console.WriteLine("Draw jpeg failed.");
        }

        for (var i = 0; i <= 100; i++)
        {
            var ret = screen.SetBrightness((byte)i);
            if (!ret)
            {
                Console.WriteLine("Set brightness failed.");
            }
            await Task.Delay(1);
        }

        UsbDevice.Exit();
    }
}

//--------------------------------------------------------------------------------
// Turing Smart Screen 8.8
//--------------------------------------------------------------------------------
[Command("tss8", "8.8inch")]
public sealed class Tss8Command : ICommandHandler
{
    [Option<string>("--port", "-p", Description = "COM Port", Required = true)]
    public string Port { get; set; } = default!;

    public async ValueTask ExecuteAsync(CommandContext context)
    {
        using var screen = ScreenFactory.Create(ScreenType.RevisionE, Port);
        screen.Orientation = ScreenOrientation.Landscape;
        screen.Clear();
        screen.SetBrightness(100);

        using var bitmap = SKBitmap.Decode("image-1280x480.jpg");
        using var buffer = screen.CreateBufferFrom(bitmap);

        using var bitmap2 = SKBitmap.Decode("image-logo.png");
        using var buffer2 = screen.CreateBufferFrom(bitmap2);

        screen.DisplayBuffer(0, 0, buffer);

        for (var i = 0; i < screen.Height - bitmap2.Height; i++)
        {
            screen.DisplayBuffer(i * 3, i, buffer2);
            await Task.Delay(0);
        }
    }
}

[Command("storage", "Storage operation")]
public sealed class Tss8StorageCommand : ICommandHandler
{
    private const string ImagePath = "/mnt/UDISK/img/";

    [Option<string>("--port", "-p", Description = "COM Port", Required = true)]
    public string Port { get; set; } = default!;

    public async ValueTask ExecuteAsync(CommandContext context)
    {
        using var screen = new TuringSmartScreenRevisionE(Port);
        screen.Open();

        // Storage information
        Console.WriteLine(screen.QueryStorageInfo());

        screen.DeleteFile($"{ImagePath}image-1280x480.jpg");
        screen.DeleteFile($"{ImagePath}image-logo.png");

        screen.UploadFile($"{ImagePath}image-1280x480.jpg", await File.ReadAllBytesAsync("image-1280x480.jpg"));
        // TODO timeout ?
        screen.UploadFile($"{ImagePath}image-logo.png", await File.ReadAllBytesAsync("image-logo.png"));

        var files = screen.ListFiles(ImagePath);
        foreach (var file in files)
        {
            Console.WriteLine(file);
        }

        //screen.StartMedia();
        //screen.StopMedia();
    }
}

//--------------------------------------------------------------------------------
// Turing Smart Screen 5
//--------------------------------------------------------------------------------
[Command("tss5", "5inch")]
public sealed class Tss5Command : ICommandHandler
{
    [Option<string>("--port", "-p", Description = "COM Port", Required = true)]
    public string Port { get; set; } = default!;

    public async ValueTask ExecuteAsync(CommandContext context)
    {
        // Create screen
        using var screen = ScreenFactory.Create(ScreenType.RevisionC, Port);

        for (var i = 100; i >= 0; i--)
        {
            screen.SetBrightness((byte)i);
            await Task.Delay(10);
        }
        screen.SetBrightness(100);

        screen.Clear();

        using var bitmap1 = SKBitmap.Decode("image-800x480.png");
        using var buffer1 = screen.CreateBufferFrom(bitmap1);

        using var bitmap2 = SKBitmap.Decode("image-logo.png");
        using var buffer2 = screen.CreateBufferFrom(bitmap2);

        screen.DisplayBuffer(0, 0, buffer1);

        // loop
        for (var y = 0; y <= screen.Height - bitmap2.Height; y += 2)
        {
            screen.DisplayBuffer(0, y, buffer2);
        }
        for (var x = 0; x <= screen.Width - bitmap2.Width; x += 2)
        {
            screen.DisplayBuffer(x, screen.Height - bitmap2.Height, buffer2);
        }
        for (var y = screen.Height - bitmap2.Height; y >= 0; y -= 2)
        {
            screen.DisplayBuffer(screen.Width - bitmap2.Width, y, buffer2);
        }
        for (var x = screen.Width - bitmap2.Width; x >= 0; x -= 2)
        {
            screen.DisplayBuffer(x, 0, buffer2);
        }

        screen.DisplayBuffer(0, 0, buffer1);

        // corner
        screen.DisplayBuffer(0, 0, buffer2);
        screen.DisplayBuffer(0, screen.Height - bitmap2.Height, buffer2);
        screen.DisplayBuffer(screen.Width - bitmap2.Width, 0, buffer2);
        screen.DisplayBuffer(screen.Width - bitmap2.Width, screen.Height - bitmap2.Height, buffer2);
    }
}

//--------------------------------------------------------------------------------
// Turing Smart Screen 3.5 type B
//--------------------------------------------------------------------------------
[Command("tss35", "3.5inch")]
public sealed class Tss35Command : ICommandHandler
{
    private const int Margin = 2;

    private const int Digits = 6;

    [Option<string>("--port", "-p", Description = "COM Port", Required = true)]
    public string Port { get; set; } = default!;

    public async ValueTask ExecuteAsync(CommandContext context)
    {
        // Create screen
        using var screen = ScreenFactory.Create(ScreenType.RevisionB, Port);

        screen.SetBrightness(100);
        screen.Orientation = ScreenOrientation.ReverseLandscape;

        screen.Clear();

        // Clear
        using var clearBuffer = screen.CreateBuffer();
        clearBuffer.Clear(255, 255, 255);
        screen.DisplayBuffer(clearBuffer);

        // Paint
        using var paint = new SKPaint();
        paint.IsAntialias = true;
        paint.Color = SKColors.Red;
        using var font = new SKFont();
        font.Size = 96;

        // Calc image size
        var imageWidth = 0;
        var imageHeight = 0;
        for (var i = 0; i < 10; i++)
        {
            font.MeasureText($"{i}", out var rect);
            imageWidth = Math.Max(imageWidth, (int)Math.Floor(rect.Width));
            imageHeight = Math.Max(imageHeight, (int)Math.Floor(rect.Height));
        }

        imageWidth += Margin * 2;
        imageHeight += Margin * 2;

        // Create digit image
        var digitImages = new IScreenBuffer[10];
        for (var i = 0; i < 10; i++)
        {
            using var bitmap = new SKBitmap(imageWidth, imageHeight);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.White);
            canvas.DrawText($"{i}", Margin, imageHeight - Margin, font, paint);
            canvas.Flush();

            var buffer = screen.CreateBuffer(imageWidth, imageHeight);
            buffer.ReadFrom(bitmap, 0, 0, imageWidth, imageHeight);
            digitImages[i] = buffer;
        }

        // Prepare display setting
        var baseX = (screen.Width - (imageWidth * Digits)) / 2;
        var baseY = (screen.Height / 2) - (imageHeight / 2);

        var previousValues = new int[Digits];
        for (var i = 0; i < previousValues.Length; i++)
        {
            previousValues[i] = Int32.MinValue;
        }

        // Display loop
        var max = Math.Pow(10, Digits);
        var counter = 0;
        while (true)
        {
            var value = counter;
            for (var i = Digits - 1; i >= 0; i--)
            {
                var number = value % 10;
                if (previousValues[i] != number)
                {
                    screen.DisplayBuffer(baseX + (imageWidth * i), baseY, digitImages[number]);
                    previousValues[i] = number;
                }

                value /= 10;
            }

            counter++;
            if (counter >= max)
            {
                counter = 0;
            }

            await Task.Delay(50);
        }
        // ReSharper disable once FunctionNeverReturns
    }
}
