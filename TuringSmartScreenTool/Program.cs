using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

using SkiaSharp;

using TuringSmartScreenLib;
using TuringSmartScreenLib.Helpers.SkiaSharp;

// ReSharper disable UseObjectOrCollectionInitializer
#pragma warning disable IDE0017

#pragma warning disable CA1812

var rootCommand = new RootCommand("Turing Smart Screen tool");
rootCommand.AddGlobalOption(new Option<string>(new[] { "--revision", "-r" }, static () => "b", "Revision"));
rootCommand.AddGlobalOption(new Option<string>(new[] { "--port", "-p" }, "Port") { IsRequired = true });

static ScreenType GetScreenType(string revision) =>
    String.Equals(revision, "a", StringComparison.OrdinalIgnoreCase)
        ? ScreenType.RevisionA
        : ScreenType.RevisionB;

// Reset
var resetCommand = new Command("reset", "Reset screen");
resetCommand.Handler = CommandHandler.Create((string revision, string port) =>
{
    try
    {
        using var screen = ScreenFactory.Create(GetScreenType(revision), port);
        screen.Reset();
    }
    catch (IOException)
    {
        // Do Nothing
    }
});
rootCommand.Add(resetCommand);

// Clear
var clearCommand = new Command("clear", "Clear screen");
clearCommand.Handler = CommandHandler.Create((string revision, string port) =>
{
    // [MEMO] Type b not supported
    using var screen = ScreenFactory.Create(GetScreenType(revision), port);
    screen.Clear();
});
rootCommand.Add(clearCommand);

// ON
var onCommand = new Command("on", "Screen ON");
onCommand.Handler = CommandHandler.Create((string revision, string port) =>
{
    using var screen = ScreenFactory.Create(GetScreenType(revision), port);
    screen.ScreenOn();
});
rootCommand.Add(onCommand);

// Off
var offCommand = new Command("off", "Screen OFF");
offCommand.Handler = CommandHandler.Create((string revision, string port) =>
{
    using var screen = ScreenFactory.Create(GetScreenType(revision), port);
    screen.ScreenOff();
});
rootCommand.Add(offCommand);

// Brightness
var brightCommand = new Command("bright", "Set brightness");
brightCommand.AddOption(new Option<byte>(new[] { "--level", "-l" }, "Level") { IsRequired = true });
brightCommand.Handler = CommandHandler.Create((string revision, string port, byte level) =>
{
    using var screen = ScreenFactory.Create(GetScreenType(revision), port);
    screen.SetBrightness(level);
});
rootCommand.Add(brightCommand);

// Orientation
var orientationCommand = new Command("orientation", "Set orientation");
orientationCommand.AddOption(new Option<string>(new[] { "--mode", "-m" }, "Mode (l or p)"));
orientationCommand.Handler = CommandHandler.Create((string revision, string port, string mode) =>
{
    using var screen = ScreenFactory.Create(GetScreenType(revision), port);
    switch (mode)
    {
        case "l":
        case "landscape":
            screen.Orientation = ScreenOrientation.Landscape;
            break;
        case "p":
        case "portrait":
            screen.Orientation = ScreenOrientation.Portrait;
            break;
    }
});
rootCommand.Add(orientationCommand);

// Image
var imageCommand = new Command("image", "Display image");
imageCommand.AddOption(new Option<string>(new[] { "--file", "-f" }, "Filename") { IsRequired = true });
imageCommand.AddOption(new Option<int>(new[] { "-x" }, static () => 0, "Position x"));
imageCommand.AddOption(new Option<int>(new[] { "-y" }, static () => 0, "Position y"));
imageCommand.Handler = CommandHandler.Create((string revision, string port, string file, int x, int y) =>
{
    using var bitmap = SKBitmap.Decode(File.OpenRead(file));

    using var screen = ScreenFactory.Create(GetScreenType(revision), port);
    var buffer = screen.CreateBuffer(bitmap.Width, bitmap.Height);
    buffer.ReadFrom(bitmap);
    screen.DisplayBuffer(x, y, buffer);
});
rootCommand.Add(imageCommand);

// Fill
var fillCommand = new Command("fill", "Fill screen");
fillCommand.AddOption(new Option<string>(new[] { "--color", "-c" }, static () => "000000", "Color"));
fillCommand.Handler = CommandHandler.Create((string revision, string port, string color) =>
{
    var c = SKColor.Parse(color);

    // TODO fix size & orientation ?
    using var screen = ScreenFactory.Create(GetScreenType(revision), port);
    var buffer = screen.CreateBuffer(480, 320);
    buffer.Clear(c.Red, c.Green, c.Blue);
    screen.DisplayBuffer(buffer);
});
rootCommand.Add(fillCommand);

// Text
var textCommand = new Command("text", "Display text");
textCommand.AddOption(new Option<string>(new[] { "--text", "-t" }, "Text") { IsRequired = true });
textCommand.AddOption(new Option<int>(new[] { "-x" }, static () => 0, "Position x"));
textCommand.AddOption(new Option<int>(new[] { "-y" }, static () => 0, "Position y"));
textCommand.AddOption(new Option<int>(new[] { "--size", "-s" }, static () => 0, "Size"));
textCommand.AddOption(new Option<string>(new[] { "--font", "-f" }, static () => string.Empty, "Font"));
textCommand.AddOption(new Option<string>(new[] { "--color", "-c" }, static () => "FFFFFF", "Color"));
textCommand.AddOption(new Option<string>(new[] { "--background", "-b" }, static () => "000000", "Color"));
textCommand.Handler = CommandHandler.Create((string revision, string port, string text, int x, int y, int size, string font, string color, string background) =>
{
    using var paint = new SKPaint();
    paint.IsAntialias = true;
    if (size > 0)
    {
        paint.TextSize = size;
    }
    if (!String.IsNullOrEmpty(font))
    {
        paint.Typeface = SKTypeface.FromFamilyName(font);
    }
    paint.Color = SKColor.Parse(color);

    var rect = default(SKRect);
    paint.MeasureText(text, ref rect);

    using var bitmap = new SKBitmap((int)Math.Floor(rect.Width), (int)Math.Floor(rect.Height));
    using var canvas = new SKCanvas(bitmap);
    canvas.Clear(SKColor.Parse(background));
    canvas.DrawText(text, 0, rect.Height, paint);
    canvas.Flush();

    using var screen = ScreenFactory.Create(GetScreenType(revision), port);
    var buffer = screen.CreateBuffer(bitmap.Width, bitmap.Height);
    buffer.ReadFrom(bitmap);
    screen.DisplayBuffer(x, y, buffer);
});
rootCommand.Add(textCommand);

return rootCommand.Invoke(args);
