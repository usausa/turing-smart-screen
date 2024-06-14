namespace TuringSmartScreenTool.Commands;

public sealed class ResetCommand : Command
{
    public ResetCommand()
        : base("reset", "Reset screen")
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

            screen.Reset();

            return Task.FromResult(0);
        }
    }
}
