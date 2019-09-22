namespace GitVersion.Log
{
    public interface ILogAppender
    {
        void WriteTo(LogLevel level, string message);
    }
}
