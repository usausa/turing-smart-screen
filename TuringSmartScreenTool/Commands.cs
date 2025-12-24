// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace TuringSmartScreenTool;

using Smart.CommandLine.Hosting;

public static class CommandBuilderExtensions
{
    public static void AddCommands(this ICommandBuilder commands)
    {
        commands.AddCommand<OnCommand>();
        commands.AddCommand<OffCommand>();
        commands.AddCommand<BrightCommand>();
        commands.AddCommand<OrientationCommand>();
        commands.AddCommand<ResetCommand>();
        commands.AddCommand<ClearCommand>();
        commands.AddCommand<ImageCommand>();
        commands.AddCommand<FillCommand>();
        commands.AddCommand<TextCommand>();
    }
}

public abstract class CommandBase
{
    [Option<string>("--revision", "-r", Description = "Revision", IsRequired = true)]
    public string Revision { get; set; } = default!;

    [Option<string>("--port", "-p", Description = "Port", IsRequired = true)]
    public string Port { get; set; } = default!;
}

// On
[Command("on", Description = "Screen ON")]
public sealed class OnCommand : CommandBase, ICommandHandler
{
    private readonly IScreenResolver screenResolver;

    public OnCommand(IScreenResolver screenResolver)
    {
        this.screenResolver = screenResolver;
    }

    public ValueTask ExecuteAsync(CommandContext context)
    {
        using var screen = screenResolver.Resolve(Revision, Port);
        screen.ScreenOn();

        return ValueTask.CompletedTask;
    }
}

// Off
[Command("off", Description = "Screen OFF")]
public sealed class OffCommand : CommandBase, ICommandHandler
{
    private readonly IScreenResolver screenResolver;

    public OffCommand(IScreenResolver screenResolver)
    {
        this.screenResolver = screenResolver;
    }

    public ValueTask ExecuteAsync(CommandContext context)
    {
        using var screen = screenResolver.Resolve(Revision, Port);
        screen.ScreenOff();

        return ValueTask.CompletedTask;
    }
}

// Bright
[Command("bright", Description = "Set brightness")]
public sealed class BrightCommand : CommandBase, ICommandHandler
{
    private readonly IScreenResolver screenResolver;

    [Option<byte>("--level", "-l", Description = "Level", IsRequired = true)]
    public byte Level { get; set; } = default!;

    public BrightCommand(IScreenResolver screenResolver)
    {
        this.screenResolver = screenResolver;
    }

    public ValueTask ExecuteAsync(CommandContext context)
    {
        using var screen = screenResolver.Resolve(Revision, Port);
        screen.SetBrightness(Level);

        return ValueTask.CompletedTask;
    }
}

// Orientation
[Command("orientation", Description = "Set orientation")]
public sealed class OrientationCommand : CommandBase, ICommandHandler
{
    private readonly IScreenResolver screenResolver;

    [Option<string>("--mode", "-m", Description = "Mode (l|p|rl|rp)")]
    public string Mode { get; set; } = default!;

    public OrientationCommand(IScreenResolver screenResolver)
    {
        this.screenResolver = screenResolver;
    }

    public ValueTask ExecuteAsync(CommandContext context)
    {
        using var screen = screenResolver.Resolve(Revision, Port);
        screen.Orientation = Mode switch
        {
            "l" or "landscape" => ScreenOrientation.Landscape,
            "p" or "portrait" => ScreenOrientation.Portrait,
            "rl" => ScreenOrientation.ReverseLandscape,
            "rp" => ScreenOrientation.ReversePortrait,
            _ => screen.Orientation
        };

        return ValueTask.CompletedTask;
    }
}

// Reset
[Command("reset", Description = "Reset screen")]
public sealed class ResetCommand : CommandBase, ICommandHandler
{
    private readonly IScreenResolver screenResolver;

    public ResetCommand(IScreenResolver screenResolver)
    {
        this.screenResolver = screenResolver;
    }

    public ValueTask ExecuteAsync(CommandContext context)
    {
        using var screen = screenResolver.Resolve(Revision, Port);
        screen.Reset();

        return ValueTask.CompletedTask;
    }
}

// Clear
[Command("clear", Description = "Clear screen")]
public sealed class ClearCommand : CommandBase, ICommandHandler
{
    private readonly IScreenResolver screenResolver;

    public ClearCommand(IScreenResolver screenResolver)
    {
        this.screenResolver = screenResolver;
    }

    public ValueTask ExecuteAsync(CommandContext context)
    {
        using var screen = screenResolver.Resolve(Revision, Port);
        screen.Clear();

        return ValueTask.CompletedTask;
    }
}

// Image
[Command("image", Description = "Display image")]
public sealed class ImageCommand : CommandBase, ICommandHandler
{
    private readonly IScreenResolver screenResolver;

    [Option<string>("--file", "-f", Description = "Filename", IsRequired = true)]
    public string File { get; set; } = default!;

    [Option<int>("-x", Description = "Position x", DefaultValue = 0)]
    public int X { get; set; }

    [Option<int>("-y", Description = "Position y", DefaultValue = 0)]
    public int Y { get; set; }

    public ImageCommand(IScreenResolver screenResolver)
    {
        this.screenResolver = screenResolver;
    }

    public ValueTask ExecuteAsync(CommandContext context)
    {
        using var screen = screenResolver.Resolve(Revision, Port);

        using var stream = System.IO.File.OpenRead(File);
        using var bitmap = SKBitmap.Decode(stream);
        using var buffer = screen.CreateBufferFrom(bitmap);

        screen.DisplayBuffer(X, Y, buffer);

        return ValueTask.CompletedTask;
    }
}

// Fill
[Command("fill", Description = "Fill screen")]
public sealed class FillCommand : CommandBase, ICommandHandler
{
    private readonly IScreenResolver screenResolver;

    [Option<string>("--color", "-c", Description = "Color", DefaultValue = "000000")]
    public string Color { get; set; } = default!;

    public FillCommand(IScreenResolver screenResolver)
    {
        this.screenResolver = screenResolver;
    }

    public ValueTask ExecuteAsync(CommandContext context)
    {
        using var screen = screenResolver.Resolve(Revision, Port);

        var c = SKColor.Parse(Color);
        using var buffer = screen.CreateBuffer(screen.Width, screen.Height);
        buffer.Clear(c.Red, c.Green, c.Blue);
        screen.DisplayBuffer(0, 0, buffer);

        return ValueTask.CompletedTask;
    }
}

// Text
[Command("text", Description = "Display text")]
public sealed class TextCommand : CommandBase, ICommandHandler
{
    private readonly IScreenResolver screenResolver;

    [Option<string>("--text", "-t", Description = "Text", IsRequired = true)]
    public string Text { get; set; } = default!;

    [Option<int>("-x", Description = "Position x", DefaultValue = 0)]
    public int X { get; set; }

    [Option<int>("-y", Description = "Position y", DefaultValue = 0)]
    public int Y { get; set; }

    [Option<int>("--size", "-s", Description = "Size", DefaultValue = 0)]
    public int Size { get; set; }

    [Option<string>("--font", "-f", Description = "Font", DefaultValue = "")]
    public string Font { get; set; } = default!;

    [Option<string>("--color", "-c", Description = "Color", DefaultValue = "FFFFFF")]
    public string Color { get; set; } = default!;

    [Option<string>("--background", "-b", Description = "Background", DefaultValue = "000000")]
    public string Background { get; set; } = default!;

    public TextCommand(IScreenResolver screenResolver)
    {
        this.screenResolver = screenResolver;
    }

    public ValueTask ExecuteAsync(CommandContext context)
    {
        using var screen = screenResolver.Resolve(Revision, Port);

        using var paint = new SKPaint();
        using var font = new SKFont();
        paint.IsAntialias = true;
        if (!String.IsNullOrEmpty(Font))
        {
            font.Typeface = SKTypeface.FromFamilyName(Font);
        }
        if (Size > 0)
        {
            font.Size = Size;
        }
        paint.Color = SKColor.Parse(Color);

        font.MeasureText(Text, out var rect);

        using var bitmap = new SKBitmap((int)Math.Floor(rect.Width), (int)Math.Floor(rect.Height));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColor.Parse(Background));
        canvas.DrawText(Text, 0, rect.Height, font, paint);
        canvas.Flush();

        using var buffer = screen.CreateBufferFrom(bitmap);
        screen.DisplayBuffer(X, Y, buffer);

        return ValueTask.CompletedTask;
    }
}
