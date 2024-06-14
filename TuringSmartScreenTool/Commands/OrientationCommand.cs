namespace TuringSmartScreenTool.Commands;

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
            screen.Orientation = Mode switch
            {
                "l" or "landscape" => ScreenOrientation.Landscape,
                "p" or "portrait" => ScreenOrientation.Portrait,
                "rl" => ScreenOrientation.ReverseLandscape,
                "rp" => ScreenOrientation.ReversePortrait,
                _ => screen.Orientation
            };

            return Task.FromResult(0);
        }
    }
}
