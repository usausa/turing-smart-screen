namespace Example3;

using System.Diagnostics;

using SkiaSharp;

using TuringSmartScreenLib;
using TuringSmartScreenLib.Helpers.SkiaSharp;

internal sealed class Worker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var screen = ScreenFactory.Create(ScreenType.RevisionE, "COM12");
        screen.SetBrightness(100);
        screen.Clear();

        using var bitmap = SKBitmap.Decode("space.jpg");
        using var buffer = screen.CreateBufferFrom(bitmap);

        using var logoBitmap = SKBitmap.Decode("logo.png");
        using var logoBuffer = screen.CreateBufferFrom(logoBitmap);

        screen.DisplayBuffer(0, 0, buffer);

        // TODO check
        Debug.WriteLine("*Partial");
        for (var i = 0; i < 10; i++)
        {
            Debug.WriteLine(screen.DisplayBuffer(0, i, logoBuffer));
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
}
