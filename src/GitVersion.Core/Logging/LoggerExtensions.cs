using System.Diagnostics;
using GitVersion.Helpers;
using Microsoft.Extensions.Logging;

namespace GitVersion.Logging;

public static class LoggerExtensions
{
    public static IDisposable IndentLog(this ILogger logger, string operationDescription)
    {
        var start = Stopwatch.GetTimestamp();
        logger.LogInformation("-< Begin: {Operation} >-", operationDescription);

        return Disposable.Create(() =>
        {
            var duration = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            logger.LogInformation("-< End: {Operation} (Took: {Duration:N}ms) >-", operationDescription, duration);
        });
    }

    public static void Separator(this ILogger logger)
        => logger.LogInformation("-------------------------------------------------------");
}
