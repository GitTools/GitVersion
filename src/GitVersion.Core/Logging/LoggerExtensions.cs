using GitVersion.Helpers;

namespace GitVersion.Logging;

#pragma warning disable S2325, S1144
/// <summary>
/// Extension methods for <see cref="ILogger"/> to provide GitVersion-specific logging functionality.
/// </summary>
internal static class LoggerExtensions
{
    /// <summary>
    /// Provides extension methods for <see cref="ILogger"/> instances.
    /// </summary>
    extension(ILogger logger)
    {
        /// <summary>
        /// Begins a timed operation scope that logs the start and end times with duration.
        /// </summary>
        /// <param name="operationDescription">A description of the operation being timed.</param>
        /// <returns>An <see cref="IDisposable"/> that logs the end of the operation when disposed.</returns>
        public IDisposable BeginTimedOperation(string operationDescription)
        {
            ArgumentNullException.ThrowIfNull(logger);

            var start = TimeProvider.System.GetTimestamp();
            logger.LogInformation("-< Begin: {OperationDescription} >-", operationDescription);

            return Disposable.Create(() =>
            {
                var end = TimeProvider.System.GetTimestamp();
                var duration = TimeProvider.System.GetElapsedTime(start, end).TotalMilliseconds;
                logger.LogInformation("-< End: {OperationDescription} (Took: {Duration:N}ms) >-", operationDescription, duration);
            });
        }

        /// <summary>
        /// Logs a separator line for visual distinction in log output.
        /// </summary>
        public void LogSeparator()
        {
            ArgumentNullException.ThrowIfNull(logger);
            logger.LogInformation("-------------------------------------------------------");
        }
    }
}
#pragma warning restore S2325, S1144

