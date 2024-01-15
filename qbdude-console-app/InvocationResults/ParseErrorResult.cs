using System.CommandLine.Invocation;
using qbdude.exceptions;

namespace qbdude.invocation.results;

/// <summary>
/// Hex file parse error invocation result. This result is returned when there is an error when reading or opening the hex file.
/// </summary>
public class ParseErrorResult : IInvocationResult
{
    private ExitCode _exitCode;

    /// <summary>
    /// Initializes a new instance of the ParseErrorResult class with a specified exit code.
    /// </summary>
    /// <param name="exitCode">Exit code that represents the reason the program was exited.</param>
    public ParseErrorResult(ExitCode exitCode)
    {
        _exitCode = exitCode;
    }

    public void Apply(InvocationContext context)
    {
        context.ExitCode = (int)_exitCode;
    }
}