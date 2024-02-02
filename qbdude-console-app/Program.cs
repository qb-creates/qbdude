using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Reflection;
using QBdude.Config;
using QBdude.Exceptions;
using QBdude.Extensions;
using QBdude.Invocation.Results;
using QBdude.UI;
using Serilog;

namespace QBdude;

/// <summary>
/// QBdude can be used to upload program data to avr microcontrollers running a QBcreate's bootloader.
/// QBcreate's bootloaders and supported microcontrollers can be found here https://github.com/qb-creates/avr-bootloaders.
/// 
/// This application utilizes the System.CommandLine nuget package. System.CommandLine is currently in PREVIEW.
/// For more information see https://learn.microsoft.com/en-us/dotnet/standard/commandline/
/// This application has built in help. Run qbdude.exe with the -h parameter to get help.
/// </summary>
class Program
{
    public static readonly FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);

    static async Task<int> Main(string[] args)
    {
        // Set the default window size and disable the resizing of the window
        ConsoleWrapper.ResizeConsoleWindow(110);
        ConsoleWrapper.DisableResizeMenuOptions();

        // Build our commands
        var rootCommand = new RootCommand("Uploader for qb.creates' bootloaders");
        rootCommand.AddUploadCommand()
            .AddComPortsCommand()
            .AddPartNumbersCommand();

        // Configure our command line
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

        // Configure logger
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File($"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/.qbdude/log.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // Configure the app
        AppConfig.Configure();

        // Invoke the parser
        var exitCode = await parser.InvokeAsync(args);

        ConsoleWrapper.WriteLine($"qbdude version: {fileVersionInfo.ProductVersion}, https://github.com/qb-creates/qbdude");
        ConsoleWrapper.ResetConsoleMenu();

        return exitCode;
    }
}
