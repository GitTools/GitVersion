namespace GitVersion;

/// <summary>Raised when a file cannot be accessed because it is locked by another process.</summary>
public class LockedFileException : Exception
{
    /// <summary>Initializes a new instance wrapping the given inner exception and using its message.</summary>
    public LockedFileException(Exception inner) : base(inner.Message, inner)
    {
    }

    /// <summary>Initializes a new instance with no message.</summary>
    public LockedFileException()
    {
    }

    /// <summary>Initializes a new instance with the given error message.</summary>
    public LockedFileException(string? message) : base(message)
    {
    }

    /// <summary>Initializes a new instance with the given message and inner exception.</summary>
    public LockedFileException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
