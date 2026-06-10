namespace GitVersion;

/// <summary>Indicates an internal logic error that should never occur in normal usage; represents a bug in GitVersion itself.</summary>
public class BugException : Exception
{
    /// <summary>Initializes a new instance with the given error message.</summary>
    public BugException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance with no message.</summary>
    public BugException()
    {
    }

    /// <summary>Initializes a new instance with the given message and inner exception.</summary>
    public BugException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
