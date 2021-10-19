using Polly;
using Polly.Retry;

namespace GitVersion.Helpers;

public class RetryAction<T>
    : RetryAction<T, bool> where T : Exception
{
    public RetryAction(int maxRetries = 5)
        : base(maxRetries)
    {
    }

    public void Execute(Action operation) => base.Execute(() =>
    {
        operation();
        return false;
    });
}
public class RetryAction<T, Result> where T : Exception
{
    private readonly RetryPolicy<Result> retryPolicy;

    public RetryAction(int maxRetries = 5)
    {
        if (maxRetries < 0)
            throw new ArgumentOutOfRangeException(nameof(maxRetries));

        var linearBackoff = LinearBackoff(TimeSpan.FromMilliseconds(100), maxRetries);
        this.retryPolicy = Policy<Result>
            .Handle<T>()
            .WaitAndRetry(linearBackoff);
    }

    public Result Execute(Func<Result> operation) => this.retryPolicy.Execute(operation);

    private static IEnumerable<TimeSpan> LinearBackoff(TimeSpan initialDelay, int retryCount, double factor = 1.0, bool fastFirst = false)
    {
        if (initialDelay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(initialDelay), initialDelay, "should be >= 0ms");
        if (retryCount < 0) throw new ArgumentOutOfRangeException(nameof(retryCount), retryCount, "should be >= 0");
        if (factor < 0) throw new ArgumentOutOfRangeException(nameof(factor), factor, "should be >= 0");

        if (retryCount == 0)
            return Empty();

        return Enumerate(initialDelay, retryCount, fastFirst, factor);

        static IEnumerable<TimeSpan> Enumerate(TimeSpan initial, int retry, bool fast, double f)
        {
            int i = 0;
            if (fast)
            {
                i++;
                yield return TimeSpan.Zero;
            }

            double ms = initial.TotalMilliseconds;
            double ad = f * ms;

            for (; i < retry; i++, ms += ad)
            {
                yield return TimeSpan.FromMilliseconds(ms);
            }
        }
    }

    private static IEnumerable<TimeSpan> Empty()
    {
        yield break;
    }
}
