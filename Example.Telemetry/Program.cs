using System.Diagnostics;

using Example.Telemetry;

using LcdDriver.TrofeoVisionLy;

using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;

using SkiaSharp;

const int interval = 100;
const int maxJpegSize = 450_000;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    // ReSharper disable once AccessToDisposedClosure
    cts.Cancel();
};

using var usbContext = new UsbContext();
var finder = new UsbDeviceFinder
{
    Vid = UsbIds.VendorId,
    Pid = UsbIds.ProductId
};
using var device = usbContext.Find(finder);
if (device is not UsbDevice usbDevice)
{
    Console.Error.WriteLine("Device not found.");
    return 1;
}

usbDevice.Open();
using var screen = new ScreenDevice(usbDevice);
var info = screen.Handshake();
if (info is null)
{
    Console.Error.WriteLine("Handshake failed.");
    return 1;
}

var quarterTurns = (int)info.Value.GetRotateOption(ScreenOrientation.Landscape);
Console.WriteLine($"PM={info.Value.Pm}, SUB={info.Value.Sub}, Rotate={quarterTurns * 90}");

var dashboard = new Dashboard();
using var surface = SKSurface.Create(new SKImageInfo(HudRenderer.Width, HudRenderer.Height));
var watch = Stopwatch.StartNew();
var last = TimeSpan.Zero;
while (!cts.IsCancellationRequested)
{
    var now = watch.Elapsed;
    dashboard.Advance((float)(now - last).TotalSeconds);
    last = now;

    dashboard.Render(surface.Canvas);
    using var image = surface.Snapshot();
    if (!screen.DrawJpeg(JpegEncoder.Encode(image, quarterTurns, maxJpegSize)))
    {
        Console.Error.WriteLine("Draw jpeg failed.");
    }

    var wait = interval - (watch.Elapsed - now).TotalMilliseconds;
    if ((wait > 0) && cts.Token.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(wait)))
    {
        break;
    }
}

return 0;
