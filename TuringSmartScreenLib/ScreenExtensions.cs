namespace TuringSmartScreenLib;

public static class ScreenExtensions
{
    public static IScreenBuffer CreateBuffer(this IScreen screen) =>
        screen.CreateBuffer(screen.Width, screen.Height);

    public static bool DisplayBuffer(this IScreen screen, IScreenBuffer buffer) =>
        screen.DisplayBuffer(0, 0, buffer);
}
