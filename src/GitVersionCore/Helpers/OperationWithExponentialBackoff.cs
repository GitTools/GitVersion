using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GitVersion.Helpers
{
    internal class OperationWithExponentialBackoff<T> where T : Exception
    {
        private IThreadSleep ThreadSleep;
        private Action Operation;
        private int MaxRetries;

        public OperationWithExponentialBackoff(IThreadSleep threadSleep, Action operation, int maxRetries = 5)
        {
            if (maxRetries < 0)
                throw new ArgumentOutOfRangeException(nameof(maxRetries));

            ThreadSleep = threadSleep ?? throw new ArgumentNullException(nameof(threadSleep));
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

                Logger.WriteInfo($"Operation failed, retrying in {sleepMSec} milliseconds.");
                await ThreadSleep.SleepAsync(sleepMSec);

                sleepMSec *= 2;
            }
        }
    }
}
