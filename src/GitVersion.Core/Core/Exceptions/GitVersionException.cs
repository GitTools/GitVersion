namespace GitVersion;

/// <summary>Base exception type for errors raised by GitVersion during normal operation.</summary>
public class GitVersionException : Exception
{
    /// <summary>Initializes a new instance with no message.</summary>
    public GitVersionException()
    {
    }

    /// <summary>Initializes a new instance with the given error message.</summary>
    public GitVersionException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with the given message and inner exception.</summary>
    public GitVersionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance using the formatted message string.</summary>
    public GitVersionException(string messageFormat, params object[] args) : base(string.Format(messageFormat, args))
    {
    }
}
