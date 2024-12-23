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

        screen.DisplayBuffer(0, 0, buffer);

        using var logoBuffer = screen.CreateBuffer(480, 16);
        logoBuffer.Clear(255, 255, 255);

        // TODO check
        Debug.WriteLine("*Partial");
        for (var i = 0; i < 600; i++)
        {
            screen.DisplayBuffer(0, i * 2, logoBuffer);
            //await Task.Delay(50, stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
}
