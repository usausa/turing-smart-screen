using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;

using TuringSmartScreenTool.Commands;

var rootCommand = new RootCommand("Turing Smart Screen tool");
rootCommand.Setup();

var builder = new CommandLineBuilder(rootCommand)
    .UseDefaults()
    .UseHost(host =>
    {
        host.UseCommandHandlers();
    });

return await builder.Build().InvokeAsync(args);
