namespace TuringSmartScreenTool.Commands;

public sealed class OnCommand : Command
{
    public OnCommand()
        : base("on", "Screen ON")
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
            screen.ScreenOn();

            return Task.FromResult(0);
        }
    }
}
