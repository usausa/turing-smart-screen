namespace TuringSmartScreenTool.Commands;

using System.CommandLine;
using System.CommandLine.Invocation;

public sealed class TextCommand : Command
{
    public TextCommand()
        : base("text", "Display text")
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
// TODO Fill
//fillCommand.AddOption(new Option<string>(["--color", "-c"], static () => "000000", "Color"));
//fillCommand.Handler = CommandHandler.Create((string revision, string port, string color) =>
//{
//    var c = SKColor.Parse(color);

//    // TODO fix size & orientation ?
//    using var screen = ScreenFactory.Create(GetScreenType(revision), port);
//    var buffer = screen.CreateBuffer(480, 320);
//    buffer.Clear(c.Red, c.Green, c.Blue);
//    screen.DisplayBuffer(buffer);
