using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using qbdude.exceptions;
using qbdude.extensions;
using qbdude.invocation.results;
using Console = qbdude.ui.Console;

namespace qbdude;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.ResizeConsoleWindow(110);
        Console.DisableResizeMenuOptions();

        var rootCommand = new RootCommand("Uploader for qb.creates' bootloaders");
        rootCommand.AddUploadCommand()
                   .AddComPortsCommand()
                   .AddPartNumbersCommand();

        var parser = new CommandLineBuilder(rootCommand)
                    .ConfigureHelp("-h")
                    .AddParseErrorReport(ExitCode.ParseError)
                    .PrintHeaderForCommands()
                    .CancelOnProcessTermination()
                    .UseExceptionHandler((e, ctx) =>
                    {
                        var ex = (e as CommandException);

                        ctx.InvocationResult = ex != null ? ex.InvocationResult : new ErrorResult();
                        Console.WriteLine($"{e?.Message!}\n\r");
                    })
                    .Build();

        var exitCode = await parser.InvokeAsync(args);
        var textColor = (ExitCode)exitCode == ExitCode.Success ? ConsoleColor.Green : ConsoleColor.Red;
        var successText = (ExitCode)exitCode == ExitCode.Success ? "SUCCESS" : "FAILURE";

        Console.Write($"==============================[");
        Console.Write($"{successText}", textColor: textColor);
        Console.WriteLine($"]====================================\r\n");

        Console.ResetConsoleMenu();

        return exitCode;
    }
}
