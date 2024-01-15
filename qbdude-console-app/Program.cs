using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Reflection;
using qbdude.exceptions;
using qbdude.extensions;
using qbdude.invocation.results;
using Console = qbdude.ui.Console;

namespace qbdude;

class Program
{
    public static FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
    
    static async Task<int> Main(string[] args)
    {
        Console.ResizeConsoleWindow(110);
        Console.DisableResizeMenuOptions();

        var rootCommand = new RootCommand("Uploader for qb.creates' bootloaders");
        rootCommand.AddUploadCommand()
                   .AddComPortsCommand()
                   .AddPartNumbersCommand();

        var parser = new CommandLineBuilder(rootCommand)
                    .UseHelp("-h")
                    .PrintHeaderForCommands()
                    .AddParseErrorReport(ExitCode.ParseError)
                    .CancelOnProcessTermination()
                    .UseExceptionHandler((e, ctx) =>
                    {
                        var ex = (e as CommandException);

                        ctx.InvocationResult = ex != null ? ex.InvocationResult : new ErrorResult();
                        Console.WriteLine($"{e?.Message!}\n\r");
                    })
                    .Build();

        var exitCode = await parser.InvokeAsync(args);

        Console.WriteLine($"qbdude version: {fileVersionInfo.ProductVersion}, http://sdfsdf");
        Console.ResetConsoleMenu();
        Console.WriteLine(exitCode.ToString());
        return exitCode;
    }
}
