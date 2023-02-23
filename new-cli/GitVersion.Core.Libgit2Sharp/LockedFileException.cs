namespace GitVersion;

public class LockedFileException : Exception
{
    public LockedFileException(Exception inner) : base(inner.Message, inner)
    {
    }
}
