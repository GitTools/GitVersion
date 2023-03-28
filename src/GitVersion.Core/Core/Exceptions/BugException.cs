namespace GitVersion;

public class BugException : Exception
{
    public BugException(string message) : base(message)
    {
    }

    public BugException()
    {
    }

    public BugException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
