using System.CommandLine.Invocation;
using QBdude.Exceptions;

namespace QBdude.Invocation.Results;

/// <summary>
/// Generic error invocation result
/// </summary>
public class ErrorResult : IInvocationResult
{
    private ExitCode _exitCode;

    /// <summary>
    /// Initializes a new instance of the ErrorResult
    /// </summary>
    public ErrorResult()
    {
        _exitCode = ExitCode.Error;
    }

    public void Apply(InvocationContext context)
    {
        context.ExitCode = (int)_exitCode;
    }
}