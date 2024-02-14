namespace TuringSmartScreenTool.Commands;

using System.CommandLine;
using System.CommandLine.Invocation;

public sealed class FillCommand : Command
{
    public FillCommand()
        : base("fill", "Fill screen")
    {
    }

    public sealed class CommandHandler : BaseCommandHandler
    {
        public override Task<int> InvokeAsync(InvocationContext context)
        {
            return Task.FromResult(0);
        }
    }
}
// TODO Text
//textCommand.AddOption(new Option<string>(["--text", "-t"], "Text") { IsRequired = true });
//textCommand.AddOption(new Option<int>(["-x"], static () => 0, "Position x"));
//textCommand.AddOption(new Option<int>(["-y"], static () => 0, "Position y"));
//textCommand.AddOption(new Option<int>(["--size", "-s"], static () => 0, "Size"));
//textCommand.AddOption(new Option<string>(["--font", "-f"], static () => string.Empty, "Font"));
//textCommand.AddOption(new Option<string>(["--color", "-c"], static () => "FFFFFF", "Color"));
//textCommand.AddOption(new Option<string>(["--background", "-b"], static () => "000000", "Color"));
//textCommand.Handler = CommandHandler.Create((string revision, string port, string text, int x, int y, int size, string font, string color, string background) =>
//{
//    using var paint = new SKPaint();
//    paint.IsAntialias = true;
//    if (size > 0)
//    {
//        paint.TextSize = size;
//    }
//    if (!String.IsNullOrEmpty(font))
//    {
//        paint.Typeface = SKTypeface.FromFamilyName(font);
//    }
//    paint.Color = SKColor.Parse(color);

//    var rect = default(SKRect);
//    paint.MeasureText(text, ref rect);

//    using var bitmap = new SKBitmap((int)Math.Floor(rect.Width), (int)Math.Floor(rect.Height));
//    using var canvas = new SKCanvas(bitmap);
//    canvas.Clear(SKColor.Parse(background));
//    canvas.DrawText(text, 0, rect.Height, paint);
//    canvas.Flush();

//    using var screen = ScreenFactory.Create(GetScreenType(revision), port);
//    var buffer = screen.CreateBuffer(bitmap.Width, bitmap.Height);
//    buffer.ReadFrom(bitmap);
//    screen.DisplayBuffer(x, y, buffer);
