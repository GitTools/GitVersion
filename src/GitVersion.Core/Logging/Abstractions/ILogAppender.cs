namespace GitVersion.Logging
{
    public interface ILogAppender
    {
        void WriteTo(LogLevel level, string message);
    }
}
