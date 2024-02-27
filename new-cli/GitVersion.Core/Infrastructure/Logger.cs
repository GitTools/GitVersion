using GitVersion.Extensions;
using Microsoft.Extensions.Logging;

namespace GitVersion.Infrastructure;

public sealed class Logger(Microsoft.Extensions.Logging.ILogger logger) : ILogger
{
    private readonly Microsoft.Extensions.Logging.ILogger logger = logger.NotNull();

    public void LogTrace(string? message, params object?[] args) => logger.LogTrace(message, args);
    public void LogDebug(string? message, params object?[] args) => logger.LogDebug(message, args);
    public void LogInformation(string? message, params object?[] args) => logger.LogInformation(message, args);
    public void LogWarning(string? message, params object?[] args) => logger.LogWarning(message, args);
    public void LogError(string? message, params object?[] args) => logger.LogError(message, args);
    public void LogCritical(string? message, params object?[] args) => logger.LogCritical(message, args);
}
