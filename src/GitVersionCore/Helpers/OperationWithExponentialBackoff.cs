using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitVersion.Common;
using GitVersion.Log;

namespace GitVersion.Helpers
{
    internal class OperationWithExponentialBackoff<T> where T : Exception
    {
        private IThreadSleep ThreadSleep;
        private readonly ILog log;
        private Action Operation;
        private int MaxRetries;

        public OperationWithExponentialBackoff(IThreadSleep threadSleep, ILog log, Action operation, int maxRetries = 5)
        {
            if (maxRetries < 0)
                throw new ArgumentOutOfRangeException(nameof(maxRetries));

            ThreadSleep = threadSleep ?? throw new ArgumentNullException(nameof(threadSleep));
            this.log = log;
            Operation = operation;
            MaxRetries = maxRetries;
        }

        public async Task ExecuteAsync()
        {
            var exceptions = new List<Exception>();

            int tries = 0;
            int sleepMSec = 500;

            while (tries <= MaxRetries)
            {
                tries++;

                try
                {
                    Operation();
                    break;
                }
                catch (T e)
                {
                    exceptions.Add(e);
                    if (tries > MaxRetries)
                    {
                        throw new AggregateException("Operation failed after maximum number of retries were exceeded.", exceptions);
                    }
                }

                log.Info($"Operation failed, retrying in {sleepMSec} milliseconds.");
                await ThreadSleep.SleepAsync(sleepMSec);

                sleepMSec *= 2;
            }
        }
    }
}
