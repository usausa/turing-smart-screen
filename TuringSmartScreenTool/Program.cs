using System;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.IO;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using TuringSmartScreenLib;

#pragma warning disable CA1812

var portOption = new Option<string>(new[] { "--port", "-p" }, "Port") { IsRequired = true };

var rootCommand = new rootCommandCommand("Turing Smart Screen tool");

// Reset
var resetCommand = new Command("reset", "Reset screen");
resetCommand.AddOption(portOption);
resetCommand.Handler = CommandHandler.Create((string port) =>
{
    try
    {
        using var screen = new TuringSmartScreen(port);
        screen.Open();
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
clearCommand.AddOption(portOption);
clearCommand.Handler = CommandHandler.Create((string port) =>
{
    using var screen = new TuringSmartScreen(port);
    screen.Open();
    screen.Clear();
});
rootCommand.Add(clearCommand);

// ON
var onCommand = new Command("on", "Screen ON");
onCommand.AddOption(portOption);
onCommand.Handler = CommandHandler.Create((string port) =>
{
    using var screen = new TuringSmartScreen(port);
    screen.Open();
    screen.ScreenOn();
});
rootCommand.Add(onCommand);

// Off
var offCommand = new Command("off", "Screen OFF");
offCommand.AddOption(portOption);
offCommand.Handler = CommandHandler.Create((string port) =>
{
    using var screen = new TuringSmartScreen(port);
    screen.Open();
    screen.ScreenOff();
});
rootCommand.Add(offCommand);

// Brightness
var brightCommand = new Command("bright", "Set brightness");
brightCommand.AddOption(portOption);
brightCommand.AddOption(new Option<int>(new[] { "--level", "-l" }, "Level") { IsRequired = true });
brightCommand.Handler = CommandHandler.Create((string port, int level) =>
{
    using var screen = new TuringSmartScreen(port);
    screen.Open();
    screen.SetBrightness(level);
});
rootCommand.Add(brightCommand);

// Display
var displayCommand = new Command("display", "Display image");
displayCommand.AddOption(portOption);
displayCommand.AddOption(new Option<string>(new[] { "--file", "-f" }, "Filename") { IsRequired = true });
displayCommand.AddOption(new Option<int>(new[] { "-sx" }, "Source x"));
displayCommand.AddOption(new Option<int>(new[] { "-sy" }, "Source y"));
displayCommand.AddOption(new Option<int>(new[] { "-sw" }, () => 320, "Source width"));
displayCommand.AddOption(new Option<int>(new[] { "-sh" }, () => 480, "Source height"));
displayCommand.AddOption(new Option<int>(new[] { "-x" }, "Screen x"));
displayCommand.AddOption(new Option<int>(new[] { "-y" }, "Screen y"));
displayCommand.Handler = CommandHandler.Create((string port, string file, int sx, int sy, int sw, int sh, int x, int y) =>
{
    using var image = Image.Load<Rgb24>(File.OpenRead(file));

    var width = Math.Min(Math.Min(sw, image.Width - sx), 320 - x);
    var height = Math.Min(Math.Min(sh, image.Height - sy), 480 - y);

    var bytes = new byte[width * height * 2];
    for (var i = 0; i < height; i++)
    {
        for (var j = 0; j < width; j++)
        {
            var color = image[sx + j, sy + i];
            var rgb = ((color.R >> 3) << 11) | ((color.G >> 2) << 5) | (color.B >> 3);
            var offset = ((i * width) + j) * 2;
            bytes[offset] = (byte)(rgb & 0xFF);
            bytes[offset + 1] = (byte)((rgb >> 8) & 0xFF);
        }
    }

    using var screen = new TuringSmartScreen(port);
    screen.Open();
    screen.DisplayBitmap(x, y, width, height, bytes);
});
rootCommand.Add(displayCommand);

return rootCommand.Invoke(args);
