namespace TuringSmartScreenTool.Commands;

public sealed class FillCommand : Command
{
    public FillCommand()
        : base("fill", "Fill screen")
    {
        AddOption(new Option<string>(["--color", "-c"], static () => "000000", "Color"));
    }

    public sealed class CommandHandler : BaseCommandHandler
    {
        private readonly IScreenResolver screenResolver;

        public string Color { get; set; } = default!;

        public CommandHandler(IScreenResolver screenResolver)
        {
            this.screenResolver = screenResolver;
        }

        public override Task<int> InvokeAsync(InvocationContext context)
        {
            using var screen = screenResolver.Resolve(Revision, Port);

            var c = SKColor.Parse(Color);
            using var buffer = screen.CreateBuffer(screen.Width, screen.Height);
            buffer.Clear(c.Red, c.Green, c.Blue);
            screen.DisplayBuffer(0, 0, buffer);

            return Task.FromResult(0);
        }
    }
}
