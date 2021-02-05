using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitVersion.Logging;

namespace GitVersion.Helpers
{
    public class OperationWithExponentialBackoff<T> : OperationWithExponentialBackoff<T, bool> where T : Exception
    {
        public OperationWithExponentialBackoff(IThreadSleep threadSleep, ILog log, Action operation, int maxRetries = 5)
            : base(threadSleep, log, () => { operation(); return false; }, maxRetries)
        {
        }

        public new Task ExecuteAsync()
        {
            return base.ExecuteAsync();
        }

    }
    public class OperationWithExponentialBackoff<T, Result> where T : Exception
    {
        private readonly IThreadSleep threadSleep;
        private readonly ILog log;
        private readonly Func<Result> operation;
        private readonly int maxRetries;

        public OperationWithExponentialBackoff(IThreadSleep threadSleep, ILog log, Func<Result> operation, int maxRetries = 5)
        {
            if (maxRetries < 0)
                throw new ArgumentOutOfRangeException(nameof(maxRetries));

            this.threadSleep = threadSleep ?? throw new ArgumentNullException(nameof(threadSleep));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.operation = operation;
            this.maxRetries = maxRetries;
        }

        public async Task<Result> ExecuteAsync()
        {
            var exceptions = new List<Exception>();

            var tries = 0;
            var sleepMSec = 500;

            while (tries <= maxRetries)
            {
                tries++;

                try
                {
                    return operation();
                }
                catch (T e)
                {
                    exceptions.Add(e);
                    if (tries > maxRetries)
                    {
                        throw new AggregateException("Operation failed after maximum number of retries were exceeded.", exceptions);
                    }
                }

                log.Info($"Operation failed, retrying in {sleepMSec} milliseconds.");
                await threadSleep.SleepAsync(sleepMSec);

                sleepMSec *= 2;
            }
            return default;
        }
    }
}
