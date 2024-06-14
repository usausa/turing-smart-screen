namespace TuringSmartScreenTool.Commands;

public sealed class BrightCommand : Command
{
    public BrightCommand()
        : base("bright", "Set brightness")
    {
        AddOption(new Option<byte>(["--level", "-l"], "Level") { IsRequired = true });
    }

    public sealed class CommandHandler : BaseCommandHandler
    {
        private readonly IScreenResolver screenResolver;

        public byte Level { get; set; }

        public CommandHandler(IScreenResolver screenResolver)
        {
            this.screenResolver = screenResolver;
        }

        public override Task<int> InvokeAsync(InvocationContext context)
        {
            using var screen = screenResolver.Resolve(Revision, Port);
            screen.SetBrightness(Level);

            return Task.FromResult(0);
        }
    }
}
