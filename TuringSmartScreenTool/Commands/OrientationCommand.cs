namespace TuringSmartScreenTool.Commands;

using System.CommandLine;
using System.CommandLine.Invocation;

public sealed class OrientationCommand : Command
{
    public OrientationCommand()
        : base("orientation", "Set orientation")
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
// TODO Orientation reverse?
//orientationCommand.AddOption(new Option<string>(["--mode", "-m"], "Mode (l or p)"));
//orientationCommand.Handler = CommandHandler.Create((string revision, string port, string mode) =>
//    using var screen = ScreenFactory.Create(GetScreenType(revision), port);
//    switch (mode)
//        case "l":
//        case "landscape":
//            screen.Orientation = ScreenOrientation.Landscape;
//            break;
//        case "p":
//        case "portrait":
//            screen.Orientation = ScreenOrientation.Portrait;
//            break;
