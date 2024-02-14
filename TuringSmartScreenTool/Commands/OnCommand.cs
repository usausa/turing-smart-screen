namespace TuringSmartScreenTool.Commands;

using System.CommandLine;
using System.CommandLine.Invocation;

public sealed class OffCommand : Command
{
    public OffCommand()
        : base("off", "Screen OFF")
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
// TODO components
//static ScreenType GetScreenType(string revision) =>
//    String.Equals(revision, "a", StringComparison.OrdinalIgnoreCase)
//        ? ScreenType.RevisionA
//        : ScreenType.RevisionB;

// TODO ON
//onCommand.Handler = CommandHandler.Create((string revision, string port) =>
//    using var screen = ScreenFactory.Create(GetScreenType(revision), port);
//    screen.ScreenOn();
