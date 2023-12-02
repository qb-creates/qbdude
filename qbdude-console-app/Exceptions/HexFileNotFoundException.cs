using System.CommandLine.Invocation;

namespace qbdude.exceptions;

/// <summary>
/// The exception that is thrown when an attempt to access a hex file that does not exist on disk fails.
/// </summary>
public class HexFileNotFoundException : CommandException
{
    /// <summary>
    /// Initializes a new instance of the HexFileNotFoundException class with a specified error
    /// message, exit code, and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="invocationResult">The result of a command handler invocation.</param>
    public HexFileNotFoundException(string message, IInvocationResult invocationResult) : base(message, invocationResult)
    {

    }

    /// <summary>
    /// Initializes a new instance of the HexFileNotFoundException class with a specified error
    /// message, exit code, and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="invocationResult">The result of a command handler invocation.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public HexFileNotFoundException(string message, IInvocationResult invocationResult, Exception innerException) : base(message, invocationResult, innerException)
    {

    }
}