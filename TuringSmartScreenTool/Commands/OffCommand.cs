namespace TuringSmartScreenTool.Commands;

public sealed class OffCommand : Command
{
    public OffCommand()
        : base("off", "Screen OFF")
    {
    }

    public sealed class CommandHandler : BaseCommandHandler
    {
        private readonly IScreenResolver screenResolver;

        public CommandHandler(IScreenResolver screenResolver)
        {
            this.screenResolver = screenResolver;
        }

        public override Task<int> InvokeAsync(InvocationContext context)
        {
            using var screen = screenResolver.Resolve(Revision, Port);
            screen.ScreenOff();

            return Task.FromResult(0);
        }
    }
}
