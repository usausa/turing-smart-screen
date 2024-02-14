namespace TuringSmartScreenTool.Commands;

using System.CommandLine;
using System.CommandLine.Invocation;

public sealed class OnCommand : Command
{
    public OnCommand()
        : base("on", "Screen ON")
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
// TODO OFF
//offCommand.Handler = CommandHandler.Create((string revision, string port) =>
//    using var screen = ScreenFactory.Create(GetScreenType(revision), port);
//    screen.ScreenOff();
