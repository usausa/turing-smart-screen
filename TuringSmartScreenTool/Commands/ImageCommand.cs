namespace TuringSmartScreenTool.Commands;

using System.CommandLine;
using System.CommandLine.Invocation;

public sealed class ImageCommand : Command
{
    public ImageCommand()
        : base("image", "Display image")
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
// TODO Image sx, sy等も！
//imageCommand.AddOption(new Option<string>(["--file", "-f"], "Filename") { IsRequired = true });
//imageCommand.AddOption(new Option<int>(["-x"], static () => 0, "Position x"));
//imageCommand.AddOption(new Option<int>(["-y"], static () => 0, "Position y"));
//imageCommand.Handler = CommandHandler.Create((string revision, string port, string file, int x, int y) =>
//{
//    using var bitmap = SKBitmap.Decode(File.OpenRead(file));

//    using var screen = ScreenFactory.Create(GetScreenType(revision), port);
//    var buffer = screen.CreateBuffer(bitmap.Width, bitmap.Height);
//    buffer.ReadFrom(bitmap);
//    screen.DisplayBuffer(x, y, buffer);
