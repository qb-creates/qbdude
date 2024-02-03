using System.CommandLine;
using System.IO.Ports;
using QBdude.Config;
using QBdude.Exceptions;
using QBdude.Invocation.Results;
using QBdude.Models;
using QBdude.UI;
using QBdude.Utilities;
using Serilog;
using static QBdude.Validators.OptionValidator;

namespace QBdude.Extensions;

/// <summary>
/// Holds a collection of extentions methods for the RootCommand class./>
/// </summary>
public static class RootCommandExtensions
{
	/// <summary>
	/// Adds a comport sub command. Running this command will print out a list of available com ports.
	/// </summary>
	/// <param name="rootCommand">Reference to the root command</param>
	/// <returns>The same instance RootCommand.</returns>
	public static RootCommand AddComPortsCommand(this RootCommand rootCommand)
	{
		var getComPortsCommand = new Command("comport", "Get a list of available com ports.");

		getComPortsCommand.SetHandler(() =>
		{
			ConsoleWrapper.WriteLine("\r\nAvailable Com Ports:");

			foreach (string serialPort in SerialPort.GetPortNames())
			{
				ConsoleWrapper.WriteLine(serialPort);
			}

			ConsoleWrapper.WriteLine();
		});

		rootCommand.AddCommand(getComPortsCommand);
		return rootCommand;
	}

	/// <summary>
	/// Adds a partnumber sub command. Running this command will print out the name and part number of all of the
	/// supported microcontrollers. 
	/// </summary>
	/// <param name="rootCommand">Reference to the root command</param>
	/// <returns>The same instance RootCommand.</returns>
	public static RootCommand AddPartNumbersCommand(this RootCommand rootCommand)
	{
		var getPartNumbersCommand = new Command("partnumber", "Get a list of supported avr microcontrollers and their part number.");

		getPartNumbersCommand.SetHandler(() =>
		{
			ConsoleWrapper.WriteLine($"\r\n{"Name",-15}{"Part Number",-15}{"Flash Size",-20}{"Signature",-10}");

			foreach (KeyValuePair<string, Microcontroller> kvp in AppConfig.DeviceDictionary)
			{
				var signature = string.Join("", kvp.Value.Signature.Select(b => b.ToString("X2")));
				ConsoleWrapper.WriteLine($"{kvp.Value.Name,-15}{kvp.Key,-15}{kvp.Value.FlashSize,-20}{signature,-20}");
			}

			ConsoleWrapper.WriteLine();
		});

		rootCommand.AddCommand(getPartNumbersCommand);
		return rootCommand;
	}

	/// <summary>
	/// Adds an upload sub command. Running th is command will start the upload process to the microcontroller.
	/// </summary>
	/// <param name="rootCommand">Reference to the root command</param>
	/// <returns>The same instance RootCommand.</returns>
	public static RootCommand AddUploadCommand(this RootCommand rootCommand)
	{
		var forceUploadOption = new Option<bool>("-f", "Will force upload for invalid signatures.");

		var partNumberOption = new Option<string>(name: "-p", description: "The Part Number of the microcontroller.", parseArgument: OnValidatePartNumber)
		{
			IsRequired = true,
			ArgumentHelpName = "PARTNUMBER"
		};

		var comportOption = new Option<string>(name: "-C", description: "The com port that will be opened.", parseArgument: OnValidateComPort)
		{
			IsRequired = true,
			ArgumentHelpName = "COMPORT"
		};

		var filePathOption = new Option<string>("-F", "The Filepath to the hex file.")
		{
			IsRequired = true,
			ArgumentHelpName = "FILEPATH"
		};

		var uploadCommand = new Command("upload", "This command will upload the program to the microcontroller")
		{
			partNumberOption,
			comportOption,
			filePathOption,
			forceUploadOption
		};

		uploadCommand.SetHandler(async (context) =>
		{
			var partNumber = context.ParseResult.GetValueForOption(partNumberOption);
			var com = context.ParseResult.GetValueForOption(comportOption);
			var filepath = context.ParseResult.GetValueForOption(filePathOption);
			var force = context.ParseResult.GetValueForOption(forceUploadOption);
			var token = context.GetCancellationToken();
			Exception exception = null;

			try
			{
				var selectedMCU = AppConfig.DeviceDictionary[partNumber!];
				var programData = await HexReaderUtility.ExtractProgramData(filepath!, token);

				await UploadUtility.UploadProgramData(com!, programData, selectedMCU, force, token);
			}
			catch (Exception e)
			{
				exception = e;

				switch (exception)
				{
					case CommandException:
						context.InvocationResult = (exception as CommandException)!.InvocationResult;
						break;
					case OperationCanceledException:
						context.InvocationResult = new CancellationResult(ExitCode.UploadCanceled);
						break;
					case Exception:
						context.InvocationResult = new ErrorResult();
						break;
				}

				Log.Error(e.ToString());
				ConsoleWrapper.WriteLine($"{e?.Message!}\r\n");
			}
			finally
			{
				var textColor = exception == null ? ConsoleColor.Green : ConsoleColor.Red;
				var successText = exception == null ? "SUCCESS" : "FAILURE";
				Log.Information($"Upload completed at {DateTime.Now}");
				ConsoleWrapper.WriteLine($"==============================[<c:{textColor}>{successText}</c:>]====================================\r\n");
			}
		});

		rootCommand.AddCommand(uploadCommand);
		return rootCommand;
	}
}