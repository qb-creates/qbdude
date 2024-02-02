using System.CommandLine.Invocation;
using QBdude.Exceptions;

namespace QBdude.Invocation.Results;

/// <summary>
/// Cancellation invocation result. This result is returned when the app is cancelled via ctrl + c.
/// </summary>
public class CancellationResult : IInvocationResult
{
    private ExitCode _exitCode;

    /// <summary>
    /// Initializes a new instance of the CancellationResult class with a specified exit code.
    /// </summary>
    /// <param name="exitCode">Exit code that represents the reason the program was exited.</param>
    public CancellationResult(ExitCode exitCode)
    {
        _exitCode = exitCode;
    }

    public void Apply(InvocationContext context)
    {
        context.ExitCode = (int)_exitCode;
    }
}