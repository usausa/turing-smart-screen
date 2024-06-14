namespace TuringSmartScreenTool.Commands;

public sealed class ImageCommand : Command
{
    public ImageCommand()
        : base("image", "Display image")
    {
        AddOption(new Option<string>(["--file", "-f"], "Filename") { IsRequired = true });
        AddOption(new Option<int>(["-x"], static () => 0, "Position x"));
        AddOption(new Option<int>(["-y"], static () => 0, "Position y"));
    }

    public sealed class CommandHandler : BaseCommandHandler
    {
        private readonly IScreenResolver screenResolver;

        public string File { get; set; } = default!;

        public int X { get; set; }

        public int Y { get; set; }

        public CommandHandler(IScreenResolver screenResolver)
        {
            this.screenResolver = screenResolver;
        }

        public override Task<int> InvokeAsync(InvocationContext context)
        {
            using var screen = screenResolver.Resolve(Revision, Port);

            using var stream = System.IO.File.OpenRead(File);
            using var bitmap = SKBitmap.Decode(stream);
            using var buffer = screen.CreateBufferFrom(bitmap);

            screen.DisplayBuffer(X, Y, buffer);

            return Task.FromResult(0);
        }
    }
}
