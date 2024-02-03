using System.CommandLine.Parsing;
using RJCP.IO.Ports;
using QBdude.Config;

namespace QBdude.Validators;

/// <summary>
/// This class holds methoads that will be used to validate option arguments.
/// </summary>
public static class OptionValidator
{

    /// <summary>
    /// Will validate the argument passed in for the Part Number option.
    /// </summary>
    /// <param name="result">The argument result passed in for the Part Number option.</param>
    /// <returns>Returns null if the validation fails.</returns>
    public static string OnValidatePartNumber(ArgumentResult result)
    {
        var partNumber = result.Tokens.Single().Value;

        if (!AppConfig.DeviceDictionary.ContainsKey(partNumber))
        {
            result.ErrorMessage = "Please enter a valid part number. Use \"qbdude partnumber\" to view supported devices.";
            return null!;
        }

        return partNumber;
    }

    /// <summary>
    /// Will validate the argument passed in for the Com Port option.
    /// </summary>
    /// <param name="result">The argument result passed in for the Com Port option.</param>
    /// <returns>Returns null if the validation fails.</returns>
    public static string OnValidateComPort(ArgumentResult result)
    {
        var serialPortDescriptions = SerialPortStream.GetPortDescriptions();
        var comPort = result.Tokens.Single().Value;
        
        if (!serialPortDescriptions.Any(portDescription => portDescription.Port.Equals(comPort, StringComparison.OrdinalIgnoreCase)))
        {
            result.ErrorMessage = "Please enter a valid vomport. Use \"qbdude comport\" to view available comports on your system.";
            return null!;
        }

        return comPort;
    }
}