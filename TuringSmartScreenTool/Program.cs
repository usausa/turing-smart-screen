using Microsoft.Extensions.DependencyInjection;

using Smart.CommandLine.Hosting;

using TuringSmartScreenTool;

var builder = CommandHost.CreateBuilder(args);

builder.Services.AddSingleton<IScreenResolver, ScreenResolver>();

builder.ConfigureCommands(commands =>
{
    commands.ConfigureRootCommand(root =>
    {
        root.WithDescription("Turing Smart Screen tool");
    });

    commands.AddCommands();
});

var host = builder.Build();
return await host.RunAsync();
