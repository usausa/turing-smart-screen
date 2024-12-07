namespace Example3;

using SkiaSharp;

using TuringSmartScreenLib;
using TuringSmartScreenLib.Helpers.SkiaSharp;

internal sealed class Worker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // TODO
        using var screen = ScreenFactory.Create(ScreenType.RevisionE, "COM12");
        screen.SetBrightness(100);
        screen.Clear();

        using var bitmap = SKBitmap.Decode("space.jpg");
        using var buffer = screen.CreateBufferFrom(bitmap);

        screen.DisplayBuffer(0, 0, buffer);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
}
