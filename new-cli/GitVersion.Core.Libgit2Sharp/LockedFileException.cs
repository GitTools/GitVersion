namespace GitVersion;

public class LockedFileException : Exception
{
    public LockedFileException(Exception inner) : base(inner.Message, inner)
    {
    }

    public LockedFileException()
    {
    }

    public LockedFileException(string? message) : base(message)
    {
    }

    public LockedFileException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
