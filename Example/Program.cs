// ReSharper disable UseObjectOrCollectionInitializer
#pragma warning disable IDE0017
using Example;

using Smart.CommandLine.Hosting;

var builder = CommandHost.CreateBuilder(args);
builder.ConfigureCommands(commands =>
{
    commands.ConfigureRootCommand(root =>
    {
        root.WithDescription("TSS example");
    });

    commands.AddCommands();
});

var host = builder.Build();
return await host.RunAsync();
