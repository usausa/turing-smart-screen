#pragma warning disable CA1303
namespace Example;

using System.Threading;

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
        commands.AddCommand<Tss8UsbCommand>(sub =>
        {
            sub.AddSubCommand<Tss8UsbCapacityCommand>();
            sub.AddSubCommand<Tss8UsbUploadCommand>();
            sub.AddSubCommand<Tss8UsbDeleteCommand>();
            sub.AddSubCommand<Tss8UsbPlayCommand>();
            sub.AddSubCommand<Tss8UsbStopCommand>();
            sub.AddSubCommand<Tss8UsbStreamCommand>();
        });
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
    }
}

[Command("capacity", "Query capacity")]
public sealed class Tss8UsbCapacityCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var finder = new UsbDeviceFinder(0x1CBE, 0x0088);
        using var device = UsbDevice.OpenUsbDevice(finder);
        if (device is null)
        {
            Console.WriteLine("Device not found.");
            return ValueTask.CompletedTask;
        }

        using var screen = new LcdDriver.TuringSmartScreen.ScreenDevice(device);

        if (!screen.Sync())
        {
            Console.WriteLine("Sync failed.");
        }

        var capacity = screen.RefreshStorage();
        if (capacity is null)
        {
            Console.WriteLine("Failed to read storage info.");
        }
        else
        {
                Console.WriteLine($"Total : {FormatKilobytes(capacity.Value.Total)}");
                    Console.WriteLine($"Used  : {FormatKilobytes(capacity.Value.Used)}");
                    Console.WriteLine($"Valid : {FormatKilobytes(capacity.Value.Valid)}");
                }
                return ValueTask.CompletedTask;
            }

            // Device capacity values are in KB
            private static string FormatKilobytes(uint kb) =>
                kb >= 1_048_576u ? $"{kb / 1_048_576.0:F2} GB" :
                kb >= 1_024u ? $"{kb / 1_024.0:F2} MB" :
                $"{kb} KB";
}

/// <summary>
/// Resolves the device-side storage path for a given local filename.
/// Rules confirmed by probing against a live device with files uploaded by the official tool:
///   .png / .jpg / .jpeg  →  /tmp/sdcard/mmcblk0p1/img/&lt;filename&gt;
///   .h264                →  /tmp/sdcard/mmcblk0p1/video/&lt;filename&gt;
/// </summary>
internal static class DevicePath
{
    private const string Root = "/tmp/sdcard/mmcblk0p1";

    public static string? Resolve(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToUpperInvariant();
        return ext switch
        {
            ".PNG" or ".JPG" or ".JPEG" => $"{Root}/img/{fileName}",
            ".H264" => $"{Root}/video/{fileName}",
            _ => null
        };
    }
}

[Command("upload", "Upload an image or H264 file to device storage")]
public sealed class Tss8UsbUploadCommand : ICommandHandler
{
    [Option<string>("--file", "-f", Description = "Local file path (.png / .jpg / .h264)", Required = true)]
    public string FilePath { get; set; } = default!;

    public ValueTask ExecuteAsync(CommandContext context)
    {
        if (!File.Exists(FilePath))
        {
            Console.WriteLine($"File not found: {FilePath}");
            return ValueTask.CompletedTask;
        }

        var devicePath = Example.DevicePath.Resolve(Path.GetFileName(FilePath));
        if (devicePath is null)
        {
            Console.WriteLine("Unsupported file type. Use .png, .jpg, or .h264.");
            return ValueTask.CompletedTask;
        }

        var finder = new UsbDeviceFinder(0x1CBE, 0x0088);
        using var device = UsbDevice.OpenUsbDevice(finder);
        if (device is null)
        {
            Console.WriteLine("Device not found.");
            return ValueTask.CompletedTask;
        }

        using var screen = new LcdDriver.TuringSmartScreen.ScreenDevice(device);
        screen.Sync();

        var fileSize = new FileInfo(FilePath).Length;
        Console.WriteLine($"Uploading {fileSize} bytes to: {devicePath}");

        using var fileStream = File.OpenRead(FilePath);
        var success = screen.WriteFile(fileStream, devicePath, (sent, total) =>
        {
            var pct = total > 0 ? $" ({100.0 * sent / total:F1}%)" : "";
            Console.WriteLine($"  {sent}/{total} bytes{pct}");
        });

        Console.WriteLine(success ? "Upload complete." : "Upload failed.");
        return ValueTask.CompletedTask;
    }
}

[Command("delete", "Delete a file from device storage")]
public sealed class Tss8UsbDeleteCommand : ICommandHandler
{
    [Option<string>("--file", "-f", Description = "File name on device (e.g. test.png or 8.8.h264)", Required = true)]
    public string FileName { get; set; } = default!;

    public ValueTask ExecuteAsync(CommandContext context)
    {
        var finder = new UsbDeviceFinder(0x1CBE, 0x0088);
        using var device = UsbDevice.OpenUsbDevice(finder);
        if (device is null)
        {
            Console.WriteLine("Device not found.");
            return ValueTask.CompletedTask;
        }

        using var screen = new LcdDriver.TuringSmartScreen.ScreenDevice(device);
        screen.Sync();

        var devicePath = Example.DevicePath.Resolve(FileName) ?? $"/tmp/sdcard/mmcblk0p1/{FileName}";

        if (!screen.DeleteFile(devicePath))
        {
            Console.WriteLine($"DeleteFile failed: {devicePath}");
        }
        else
        {
            Console.WriteLine($"Deleted: {devicePath}");
        }
        return ValueTask.CompletedTask;
    }
}

[Command("play", "Play a file stored on the device (.jpg / .h264)")]
public sealed class Tss8UsbPlayCommand : ICommandHandler
{
    [Option<string>("--file", "-f", Description = "File name on device (.jpg or .h264)", Required = true)]
    public string FileName { get; set; } = default!;

    public ValueTask ExecuteAsync(CommandContext context)
    {
        var finder = new UsbDeviceFinder(0x1CBE, 0x0088);
        using var device = UsbDevice.OpenUsbDevice(finder);
        if (device is null)
        {
            Console.WriteLine("Device not found.");
            return ValueTask.CompletedTask;
        }

        var ext = Path.GetExtension(FileName).ToUpperInvariant();
        if (ext is not (".JPG" or ".JPEG" or ".H264"))
        {
            Console.WriteLine($"Unsupported file type '{ext}'. Use .jpg or .h264.");
            return ValueTask.CompletedTask;
        }

        using var screen = new LcdDriver.TuringSmartScreen.ScreenDevice(device);
        screen.Sync();

        screen.StopPlayback();
        screen.ResetPlayback();
        screen.SetOrientation(LcdDriver.TuringSmartScreen.ScreenOrientation.Portrait);
        screen.SetBrightness(255);
        screen.PrepareStreamBuffer();

        var devicePath = Example.DevicePath.Resolve(FileName) ?? $"/tmp/sdcard/mmcblk0p1/{FileName}";

        // jpg  → cmd 113 (PlayFile3)
        // h264 → cmd 110 (PlayFile2)
        var result = ext is ".JPG" or ".JPEG"
            ? screen.PlayFile3(devicePath)
            : screen.PlayFile2(devicePath);

        Console.WriteLine(result ? $"Playback started: {devicePath}" : "PlayFile failed.");
        return ValueTask.CompletedTask;
    }
}

[Command("stop", "Stop running playback")]
public sealed class Tss8UsbStopCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var finder = new UsbDeviceFinder(0x1CBE, 0x0088);
        using var device = UsbDevice.OpenUsbDevice(finder);
        if (device is null)
        {
            Console.WriteLine("Device not found.");
            return ValueTask.CompletedTask;
        }

        using var screen = new LcdDriver.TuringSmartScreen.ScreenDevice(device);
        screen.Sync();
        screen.StopPlayback();
        screen.ResetPlayback();
        Console.WriteLine("Playback stopped.");
        return ValueTask.CompletedTask;
    }
}

[Command("stream", "Stream H264 bitstream to device (.h264)")]
public sealed class Tss8UsbStreamCommand : ICommandHandler
{
    // Pre-built 480×1920 all-black RGBA PNG for clearing the screen before H264 streaming.
    // Matches the hardcoded image used by the Python clear_image() function.
    private static readonly byte[] ClearScreenPng = BuildClearScreenPng();

    private static byte[] BuildClearScreenPng()
    {
        ReadOnlySpan<byte> header =
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x01, 0xE0, 0x00, 0x00, 0x07, 0x80, 0x08, 0x06, 0x00, 0x00, 0x00, 0x16, 0xF0, 0x84,
            0xF5, 0x00, 0x00, 0x00, 0x01, 0x73, 0x52, 0x47, 0x42, 0x00, 0xAE, 0xCE, 0x1C, 0xE9, 0x00, 0x00,
            0x00, 0x04, 0x67, 0x41, 0x4D, 0x41, 0x00, 0x00, 0xB1, 0x8F, 0x0B, 0xFC, 0x61, 0x05, 0x00, 0x00,
            0x00, 0x09, 0x70, 0x48, 0x59, 0x73, 0x00, 0x00, 0x0E, 0xC3, 0x00, 0x00, 0x0E, 0xC3, 0x01, 0xC7,
            0x6F, 0xA8, 0x64, 0x00, 0x00, 0x0E, 0x0C, 0x49, 0x44, 0x41, 0x54, 0x78, 0x5E, 0xED, 0xC1, 0x01,
            0x0D, 0x00, 0x00, 0x00, 0xC2, 0xA0, 0xF7, 0x4F, 0x6D, 0x0F, 0x07, 0x14, 0x00, 0x00, 0x00, 0x00
        ];
        ReadOnlySpan<byte> footer =
        [
            0x00, 0xF0, 0x66, 0x4A, 0xC8, 0x00, 0x01, 0x11, 0x9D, 0x82, 0x0A,
            0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82
        ];
        const int ZeroMiddle = 3568;
        var data = new byte[header.Length + ZeroMiddle + footer.Length];
        header.CopyTo(data);
        footer.CopyTo(data.AsSpan(header.Length + ZeroMiddle));
        return data;
    }

    [Option<string>("--file", "-f", Description = "Local H264 bitstream file (.h264)", Required = true)]
    public string FilePath { get; set; } = default!;

    [Option<int>("--loop", "-l", Description = "Loop: 0=once 1=loop until Ctrl+C (default 0)", Required = false)]
    public int Loop { get; set; }

    public async ValueTask ExecuteAsync(CommandContext context)
    {
        if (!File.Exists(FilePath))
        {
            Console.WriteLine($"File not found: {FilePath}");
            return;
        }

        var finder = new UsbDeviceFinder(0x1CBE, 0x0088);
        using var device = UsbDevice.OpenUsbDevice(finder);
        if (device is null)
        {
            Console.WriteLine("Device not found.");
            return;
        }

        using var screen = new LcdDriver.TuringSmartScreen.ScreenDevice(device);
        screen.Sync();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            // ReSharper disable once AccessToDisposedClosure
            cts.Cancel();
        };

        try
        {
            do
            {
                screen.StopPlayback();
                screen.ResetPlayback();
                screen.SetOrientation(LcdDriver.TuringSmartScreen.ScreenOrientation.Portrait);
                screen.SetBrightness(255);
                screen.PrepareStreamBuffer();
                screen.DrawPng(ClearScreenPng);
                screen.SetFrameRate(25);

                using var fileStream = File.OpenRead(FilePath);
                await screen.StreamH264Async(fileStream, cts.Token);
                Console.WriteLine("Stream completed.");
            }
            while (Loop != 0 && !cts.Token.IsCancellationRequested);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Stream interrupted.");
        }
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
