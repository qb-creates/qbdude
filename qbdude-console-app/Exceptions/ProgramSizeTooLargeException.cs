using System.CommandLine.Invocation;

namespace qbdude.exceptions;

/// <summary>
/// The exception that is thrown when the program size is too large to upload.
/// </summary>
public class ProgramSizeTooLargeException : CommandException
{
    private const string DEFAULT_MESSAGE = "Selected MCU does not have enough space for this program.";

    /// <summary>
    /// Initializes a new instance of the ProgramSizeTooLargeException class with a specified error
    /// message, exit code, and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="invocationResult">The result of a command handler invocation.</param>
    public ProgramSizeTooLargeException(IInvocationResult invocationResult) : base(DEFAULT_MESSAGE, invocationResult)
    {

    }

    /// <summary>
    /// Initializes a new instance of the ProgramSizeTooLargeException class with a specified error
    /// message, exit code, and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="invocationResult">The result of a command handler invocation.</param>
    public ProgramSizeTooLargeException(string message, IInvocationResult invocationResult) : base(message, invocationResult)
    {

    }

    /// <summary>
    /// Initializes a new instance of the ProgramSizeTooLargeException class with a specified error
    /// message, exit code, and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="invocationResult">The result of a command handler invocation.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public ProgramSizeTooLargeException(string message, IInvocationResult invocationResult, Exception innerException) : base(message, invocationResult, innerException)
    {

    }
}