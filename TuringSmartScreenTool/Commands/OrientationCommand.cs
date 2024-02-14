namespace TuringSmartScreenTool.Commands;

using System.CommandLine;
using System.CommandLine.Invocation;

using TuringSmartScreenLib;

using TuringSmartScreenTool.Components;

public sealed class OrientationCommand : Command
{
    public OrientationCommand()
        : base("orientation", "Set orientation")
    {
        AddOption(new Option<string>(["--mode", "-m"], "Mode (l|p)"));
    }

    public sealed class CommandHandler : BaseCommandHandler
    {
        private readonly IScreenResolver screenResolver;

        public string Mode { get; set; } = default!;

        public CommandHandler(IScreenResolver screenResolver)
        {
            this.screenResolver = screenResolver;
        }

        public override Task<int> InvokeAsync(InvocationContext context)
        {
            using var screen = screenResolver.Resolve(Revision, Port);
            switch (Mode)
            {
                case "l":
                case "landscape":
                    screen.Orientation = ScreenOrientation.Landscape;
                    break;
                case "p":
                case "portrait":
                    screen.Orientation = ScreenOrientation.Portrait;
                    break;
                case "rl":
                    screen.Orientation = ScreenOrientation.ReverseLandscape;
                    break;
                case "rp":
                    screen.Orientation = ScreenOrientation.ReversePortrait;
                    break;
            }

            return Task.FromResult(0);
        }
    }
}
