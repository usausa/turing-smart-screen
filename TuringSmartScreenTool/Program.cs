using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;

using Microsoft.Extensions.DependencyInjection;

using TuringSmartScreenTool.Commands;

var rootCommand = new RootCommand("Turing Smart Screen tool");
rootCommand.Setup();

var builder = new CommandLineBuilder(rootCommand)
    .UseDefaults()
    .UseHost(host =>
    {
        host.ConfigureServices((_, service) =>
        {
            service.AddSingleton<IScreenResolver, ScreenResolver>();
        });

        host.UseCommandHandlers();
    });

return await builder.Build().InvokeAsync(args);
