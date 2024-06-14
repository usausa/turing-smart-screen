namespace TuringSmartScreenTool.Commands;

using System.CommandLine.Hosting;

using Microsoft.Extensions.Hosting;

public static class CommandExtensions
{
    public static RootCommand Setup(this RootCommand command)
    {
        command.AddGlobalOption(new Option<string>(["--revision", "-r"], "Revision") { IsRequired = true });
        command.AddGlobalOption(new Option<string>(["--port", "-p"], "Port") { IsRequired = true });

        command.Add(new OnCommand());
        command.Add(new OffCommand());
        command.Add(new BrightCommand());
        command.Add(new OrientationCommand());
        command.Add(new ResetCommand());
        command.Add(new ClearCommand());
        command.Add(new ImageCommand());
        command.Add(new FillCommand());
        command.Add(new TextCommand());

        return command;
    }

    public static void UseCommandHandlers(this IHostBuilder host)
    {
        host.UseCommandHandler<OnCommand, OnCommand.CommandHandler>();
        host.UseCommandHandler<OffCommand, OffCommand.CommandHandler>();
        host.UseCommandHandler<BrightCommand, BrightCommand.CommandHandler>();
        host.UseCommandHandler<OrientationCommand, OrientationCommand.CommandHandler>();
        host.UseCommandHandler<ResetCommand, ResetCommand.CommandHandler>();
        host.UseCommandHandler<ClearCommand, ClearCommand.CommandHandler>();
        host.UseCommandHandler<ImageCommand, ImageCommand.CommandHandler>();
        host.UseCommandHandler<FillCommand, FillCommand.CommandHandler>();
        host.UseCommandHandler<TextCommand, TextCommand.CommandHandler>();
    }
}
