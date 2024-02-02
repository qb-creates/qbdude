using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Reflection;
using qbdude.exceptions;
using qbdude.invocation.results;
using qbdude.ui;
using Spectre.Console;

namespace qbdude.extensions;

/// <summary>
/// Holds a collection of extention methods for the CommandLineBuilder class./>
/// </summary>
public static class CommandLineBuilderExtensions
{
    /// <summary>
    /// Prints the header when commands are triggered. 
    /// </summary>
    /// <param name="commandLineBuilder">A command line builder.</param>
    /// <returns></returns>
    public static CommandLineBuilder PrintHeaderForCommands(this CommandLineBuilder commandLineBuilder)
    {
        commandLineBuilder.AddMiddleware(async delegate (InvocationContext context, Func<InvocationContext, Task> next)
         {
             PrintHeader();
             await next(context);
         }, (MiddlewareOrder)(-1500));

        return commandLineBuilder;
    }

    /// <summary>
    /// Configures the command line to write error information to standard error when
    /// there are errors parsing command line input. Errors will be printed after the help section
    /// </summary>
    /// <param name="commandLineBuilder">A command line builder.</param>
    /// <param name="errorExitCode">The exit code to use when parser errors occur.</param>
    /// <returns>The same instance of CommandLineBuilder.</returns>
    public static CommandLineBuilder AddParseErrorReport(this CommandLineBuilder commandLineBuilder, ExitCode errorExitCode)
    {
        commandLineBuilder.AddMiddleware(async delegate (InvocationContext context, Func<InvocationContext, Task> next)
        {
            if (context.ParseResult.Errors.Count > 0)
            {
                var parser = new CommandLineBuilder(context.ParseResult.CommandResult.Command).UseHelp("-h").Build();
                await parser.InvokeAsync("-h");

                context.InvocationResult = new ParseErrorResult(errorExitCode);

                if (context.ParseResult.CommandResult.Command.Name != Program.fileVersionInfo.ProductName)
                {
                    PrintError(context);
                }

                return;
            }

            await next(context);
        });

        return commandLineBuilder;
    }

    private static void PrintHeader()
    {
        var fontPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location);
        var font = FigletFont.Load($"{fontPath}\\Assets\\small.flf");

        ConsoleWrapper.WriteLine();
        AnsiConsole.Write(new FigletText(font, "QB.DUDE").Color(Color.Green1));
    }

    private static void PrintError(InvocationContext context)
    {
        foreach (var error in context.ParseResult.Errors)
        {
            ConsoleWrapper.WriteLine($"<c:red>{error}</c:>");
        }

        ConsoleWrapper.WriteLine();
    }
}