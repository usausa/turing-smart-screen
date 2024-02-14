namespace TuringSmartScreenTool.Commands;

using System.CommandLine.Invocation;

public abstract class BaseCommandHandler : ICommandHandler
{
    public required string Host { get; set; }

    public required string Port { get; set; }

    public int Invoke(InvocationContext context) => 0;

    public abstract Task<int> InvokeAsync(InvocationContext context);
}
