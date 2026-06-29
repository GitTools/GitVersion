namespace GitVersion;

/// <summary>Raised to report a recoverable warning condition that should be presented to the user rather than treated as a hard error.</summary>
public class WarningException : Exception
{
    /// <summary>Initializes a new instance with the given warning message.</summary>
    public WarningException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with no message.</summary>
    public WarningException()
    {
    }

    /// <summary>Initializes a new instance with the given message and inner exception.</summary>
    public WarningException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
