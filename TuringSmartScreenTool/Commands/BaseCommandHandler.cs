namespace TuringSmartScreenTool.Commands;

public abstract class BaseCommandHandler : ICommandHandler
{
    public required string Revision { get; set; }

    public required string Port { get; set; }

    public int Invoke(InvocationContext context) => 0;

    public abstract Task<int> InvokeAsync(InvocationContext context);
}
