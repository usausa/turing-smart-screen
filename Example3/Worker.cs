namespace Example3;

using SkiaSharp;

using TuringSmartScreenLib;
using TuringSmartScreenLib.Helpers.SkiaSharp;

internal sealed class Worker : BackgroundService
{
    private readonly IHostApplicationLifetime appLifetime;

    public Worker(IHostApplicationLifetime appLifetime)
    {
        this.appLifetime = appLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await ExecuteCoreAsync(stoppingToken);
        }
        finally
        {
            appLifetime.StopApplication();
        }
    }

    private static async ValueTask ExecuteCoreAsync(CancellationToken stoppingToken)
    {
        using var screen = ScreenFactory.Create(ScreenType.RevisionE, "COM12");
        screen.Orientation = ScreenOrientation.Landscape;
        screen.Clear();
        screen.SetBrightness(100);

        using var bitmap = SKBitmap.Decode("space.jpg");
        using var buffer = screen.CreateBufferFrom(bitmap);

        using var bitmap2 = SKBitmap.Decode("test.png");
        using var buffer2 = screen.CreateBufferFrom(bitmap2);

        screen.DisplayBuffer(0, 0, buffer);

        for (var i = 0; i < screen.Height - bitmap2.Height; i++)
        {
            screen.DisplayBuffer(i * 3, i, buffer2);
            await Task.Delay(0, stoppingToken);
        }

        //while (!stoppingToken.IsCancellationRequested)
        //{
        //    await Task.Delay(1000, stoppingToken);
        //}
    }
}
