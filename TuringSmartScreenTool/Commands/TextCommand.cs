namespace TuringSmartScreenTool.Commands;

using System.CommandLine;
using System.CommandLine.Invocation;

using SkiaSharp;

using TuringSmartScreenLib.Helpers.SkiaSharp;

using TuringSmartScreenTool.Components;

public sealed class TextCommand : Command
{
    public TextCommand()
        : base("text", "Display text")
    {
        AddOption(new Option<string>(["--text", "-t"], "Text") { IsRequired = true });
        AddOption(new Option<int>(["-x"], static () => 0, "Position x"));
        AddOption(new Option<int>(["-y"], static () => 0, "Position y"));
        AddOption(new Option<int>(["--size", "-s"], static () => 0, "Size"));
        AddOption(new Option<string>(["--font", "-f"], static () => string.Empty, "Font"));
        AddOption(new Option<string>(["--color", "-c"], static () => "FFFFFF", "Color"));
        AddOption(new Option<string>(["--background", "-b"], static () => "000000", "Color"));
    }

    public sealed class CommandHandler : BaseCommandHandler
    {
        private readonly IScreenResolver screenResolver;

        public string Text { get; set; } = default!;

        public int X { get; set; }

        public int Y { get; set; }

        public int Size { get; set; }

        public string Font { get; set; } = default!;

        public string Color { get; set; } = default!;

        public string Background { get; set; } = default!;

        public CommandHandler(IScreenResolver screenResolver)
        {
            this.screenResolver = screenResolver;
        }

        public override Task<int> InvokeAsync(InvocationContext context)
        {
            using var screen = screenResolver.Resolve(Revision, Port);

            using var paint = new SKPaint();
            paint.IsAntialias = true;
            if (Size > 0)
            {
                paint.TextSize = Size;
            }
            if (!String.IsNullOrEmpty(Font))
            {
                paint.Typeface = SKTypeface.FromFamilyName(Font);
            }
            paint.Color = SKColor.Parse(Color);

            var rect = default(SKRect);
            paint.MeasureText(Text, ref rect);

            using var bitmap = new SKBitmap((int)Math.Floor(rect.Width), (int)Math.Floor(rect.Height));
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColor.Parse(Background));
            canvas.DrawText(Text, 0, rect.Height, paint);
            canvas.Flush();

            using var buffer = screen.CreateBufferFrom(bitmap);
            screen.DisplayBuffer(X, Y, buffer);

            return Task.FromResult(0);
        }
    }
}
