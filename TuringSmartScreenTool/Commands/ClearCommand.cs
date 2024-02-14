namespace TuringSmartScreenTool.Commands;

using System.CommandLine;
using System.CommandLine.Invocation;

public sealed class ClearCommand : Command
{
    public ClearCommand()
        : base("clear", "Clear screen")
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
// TODO Clear
//clearCommand.Handler = CommandHandler.Create((string revision, string port) =>
//    using var screen = ScreenFactory.Create(GetScreenType(revision), port);
//    screen.Clear();
