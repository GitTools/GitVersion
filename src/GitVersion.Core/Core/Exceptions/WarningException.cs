namespace GitVersion;

[Serializable]
public class WarningException : Exception
{
    public WarningException(string message)
        : base(message)
    {
    }

    public WarningException()
    {
    }

    public WarningException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
