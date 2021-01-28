using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitVersion.Logging;

namespace GitVersion.Helpers
{
    internal class OperationWithExponentialBackoff<T> where T : Exception
    {
        private readonly IThreadSleep threadSleep;
        private readonly ILog log;
        private readonly Action operation;
        private readonly int maxRetries;

        public OperationWithExponentialBackoff(IThreadSleep threadSleep, ILog log, Action operation, int maxRetries = 5)
        {
            if (maxRetries < 0)
                throw new ArgumentOutOfRangeException(nameof(maxRetries));

            this.threadSleep = threadSleep ?? throw new ArgumentNullException(nameof(threadSleep));
            this.log = log;
            this.operation = operation;
            this.maxRetries = maxRetries;
        }

        public async Task ExecuteAsync()
        {
            var exceptions = new List<Exception>();

            var tries = 0;
            var sleepMSec = 500;

            while (tries <= maxRetries)
            {
                tries++;

                try
                {
                    operation();
                    break;
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
        }
    }
}
