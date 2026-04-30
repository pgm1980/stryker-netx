using System;

namespace Stryker.Abstractions.Exceptions;

/// <summary>
/// Represents error when no test projects are found in the solution or configured for stryker.
/// </summary>
public class NoTestProjectsException : Exception
{
    private const string DefaultMessage = "No test projects found. Please add a test project to your solution or fix your stryker config.";

    public NoTestProjectsException()
        : base(DefaultMessage)
    {
    }

    public NoTestProjectsException(string message)
        : base(message)
    {
    }

    public NoTestProjectsException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
