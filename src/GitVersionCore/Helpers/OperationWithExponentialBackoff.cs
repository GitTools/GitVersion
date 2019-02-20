using System;
using System.Collections.Generic;

namespace GitVersion.Helpers
{
    internal class OperationWithExponentialBackoff<T> where T : Exception
    {
        private IThreadSleep ThreadSleep;
        private Action Operation;
        private int MaxRetries;

        public OperationWithExponentialBackoff(IThreadSleep threadSleep, Action operation, int maxRetries = 5)
        {
            if (threadSleep == null)
                throw new ArgumentNullException("threadSleep");
            if (maxRetries < 0)
                throw new ArgumentOutOfRangeException("maxRetries");

            this.ThreadSleep = threadSleep;
            this.Operation = operation;
            this.MaxRetries = maxRetries;
        }

        public void Execute()
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

                Logger.WriteInfo(string.Format("Operation failed, retrying in {0} milliseconds.", sleepMSec));
                ThreadSleep.Sleep(sleepMSec);
                sleepMSec *= 2;
            }
        }
    }
}
