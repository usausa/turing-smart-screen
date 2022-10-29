using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

using SkiaSharp;

using TuringSmartScreenLib;
using TuringSmartScreenLib.Helpers.SkiaSharp;

#pragma warning disable CA1812

var revisionOption = new Option<string>(new[] { "--revision", "-r" }, static () => "b", "Revision");
var portOption = new Option<string>(new[] { "--port", "-p" }, "Port") { IsRequired = true };

var rootCommand = new RootCommand("Turing Smart Screen tool");

static ScreenType GetScreenType(string revision) =>
    String.Equals(revision, "a", StringComparison.OrdinalIgnoreCase)
        ? ScreenType.RevisionA
        : ScreenType.RevisionB;

// Reset
var resetCommand = new Command("reset", "Reset screen");
resetCommand.AddOption(revisionOption);
resetCommand.AddOption(portOption);
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
clearCommand.AddOption(revisionOption);
clearCommand.AddOption(portOption);
clearCommand.Handler = CommandHandler.Create((string revision, string port) =>
{
    // [MEMO] Type b not supported
    using var screen = ScreenFactory.Create(GetScreenType(revision), port);
    screen.Clear();
});
rootCommand.Add(clearCommand);

// ON
var onCommand = new Command("on", "Screen ON");
onCommand.AddOption(revisionOption);
onCommand.AddOption(portOption);
onCommand.Handler = CommandHandler.Create((string revision, string port) =>
{
    using var screen = ScreenFactory.Create(GetScreenType(revision), port);
    screen.ScreenOn();
});
rootCommand.Add(onCommand);

// Off
var offCommand = new Command("off", "Screen OFF");
offCommand.AddOption(revisionOption);
offCommand.AddOption(portOption);
offCommand.Handler = CommandHandler.Create((string revision, string port) =>
{
    using var screen = ScreenFactory.Create(GetScreenType(revision), port);
    screen.ScreenOff();
});
rootCommand.Add(offCommand);

// Brightness
var brightCommand = new Command("bright", "Set brightness");
brightCommand.AddOption(revisionOption);
brightCommand.AddOption(portOption);
brightCommand.AddOption(new Option<byte>(new[] { "--level", "-l" }, "Level") { IsRequired = true });
brightCommand.Handler = CommandHandler.Create((string revision, string port, byte level) =>
{
    using var screen = ScreenFactory.Create(GetScreenType(revision), port);
    screen.SetBrightness(level);
});
rootCommand.Add(brightCommand);

// Orientation
var orientationCommand = new Command("orientation", "Set orientation");
orientationCommand.AddOption(revisionOption);
orientationCommand.AddOption(portOption);
orientationCommand.AddOption(new Option<string>(new[] { "--mode", "-m" }, "Mode (l or p)"));
orientationCommand.Handler = CommandHandler.Create((string revision, string port, string mode) =>
{
    using var screen = ScreenFactory.Create(GetScreenType(revision), port);
    switch (mode)
    {
        case "l":
        case "landscape":
            screen.SetOrientation(ScreenOrientation.Landscape);
            break;
        case "p":
        case "portrait":
            screen.SetOrientation(ScreenOrientation.Portrait);
            break;
    }
});
rootCommand.Add(orientationCommand);

// Image
var imageCommand = new Command("image", "Display image");
imageCommand.AddOption(revisionOption);
imageCommand.AddOption(portOption);
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

var fillCommand = new Command("fill", "Fill screen");
fillCommand.AddOption(revisionOption);
fillCommand.AddOption(portOption);
fillCommand.AddOption(new Option<string>(new[] { "--color", "-c" }, static () => "000000", "Color"));
fillCommand.Handler = CommandHandler.Create((string revision, string port, string color) =>
{
    var c = Convert.ToInt32(color, 16);
    var r = (byte)((c >> 16) & 0xff);
    var g = (byte)((c >> 8) & 0xff);
    var b = (byte)(c & 0xff);

    using var screen = ScreenFactory.Create(GetScreenType(revision), port);
    var buffer = screen.CreateBuffer(480, 320);
    buffer.Clear(r, g, b);
    screen.DisplayBuffer(buffer);
});
rootCommand.Add(fillCommand);

// TODO text

return rootCommand.Invoke(args);
