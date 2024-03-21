namespace GitVersion;

[Serializable]
public class GitVersionException : Exception
{
    public GitVersionException()
    {
    }

    public GitVersionException(string message)
        : base(message)
    {
    }

    public GitVersionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public GitVersionException(string messageFormat, params object[] args) : base(string.Format(messageFormat, args))
    {
    }
}
