namespace GitVersion.Infrastructure;

public interface ILogger
{
    void LogTrace(string? message, params object?[] args);
    void LogDebug(string? message, params object?[] args);
    void LogInformation(string? message, params object?[] args);
    void LogWarning(string? message, params object?[] args);
    void LogError(string? message, params object?[] args);
    void LogCritical(string? message, params object?[] args);
}