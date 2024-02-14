namespace TuringSmartScreenTool.Commands;

using System.CommandLine;
using System.CommandLine.Invocation;

public sealed class BrightCommand : Command
{
    public BrightCommand()
        : base("bright", "Set brightness")
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
// TODO Brightness
//brightCommand.AddOption(new Option<byte>(["--level", "-l"], "Level") { IsRequired = true });
//brightCommand.Handler = CommandHandler.Create((string revision, string port, byte level) =>
//    using var screen = ScreenFactory.Create(GetScreenType(revision), port);
//    screen.SetBrightness(level);
