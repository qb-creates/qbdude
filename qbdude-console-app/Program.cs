using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Reflection;
using qbdude.config;
using qbdude.exceptions;
using qbdude.extensions;
using qbdude.invocation.results;
using qbdude.ui;
using Serilog;

namespace qbdude;

class Program
{
    public static FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);

    static async Task<int> Main(string[] args)
    {
        ConsoleWrapper.ResizeConsoleWindow(110);
        ConsoleWrapper.DisableResizeMenuOptions();

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
                Log.Error(e.ToString());
                ConsoleWrapper.WriteLine($"{e?.Message!}\n\r");
            })
            .Build();
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File($"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/.qbdude/logs/qbdude.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        
        Console.WriteLine(AppConfig.DeviceDictionary["m128"].Name);

        var exitCode = await parser.InvokeAsync(args);

        ConsoleWrapper.WriteLine($"qbdude version: {fileVersionInfo.ProductVersion}, http://thisistest");
        ConsoleWrapper.ResetConsoleMenu();

        return exitCode;
    }
}
