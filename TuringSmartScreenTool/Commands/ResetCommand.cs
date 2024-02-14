namespace TuringSmartScreenTool.Commands;

using System.CommandLine;
using System.CommandLine.Invocation;

public sealed class ResetCommand : Command
{
    public ResetCommand()
        : base("reset", "Reset screen")
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
// TODO reset
//resetCommand.Handler = CommandHandler.Create((string revision, string port) =>
//    try
//    {
//        using var screen = ScreenFactory.Create(GetScreenType(revision), port);
//        screen.Reset();
//    }
//    catch (IOException)
//    {
//        // Do Nothing
//    }
