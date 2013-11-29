namespace GitFlowVersion.Integration
{
    public interface ILogger
    {
        void LogWarning(string message);
        void LogInfo(string message);
        void LogError(string message, string file = null);
    }
}
