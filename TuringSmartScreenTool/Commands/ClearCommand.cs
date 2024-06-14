namespace TuringSmartScreenTool.Commands;

public sealed class ClearCommand : Command
{
    public ClearCommand()
        : base("clear", "Clear screen")
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
            screen.Clear();

            return Task.FromResult(0);
        }
    }
}
