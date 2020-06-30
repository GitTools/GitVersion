using System;
using System.IO;
using System.Threading.Tasks;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersionCore.Tests.Helpers;
using GitVersionCore.Tests.Mocks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace GitVersionCore.Tests
{
    [TestFixture]
    public class OperationWithExponentialBackoffTests : TestBase
    {
        private ILog log;

        public OperationWithExponentialBackoffTests()
        {
            var sp = ConfigureServices();
            log = sp.GetService<ILog>();
        }

        [Test]
        public void RetryOperationThrowsWhenNegativeMaxRetries()
        {
            Action action = () => new OperationWithExponentialBackoff<IOException>(new MockThreadSleep(), log, () => { }, -1);
            action.ShouldThrow<ArgumentOutOfRangeException>();
        }

        [Test]
        public void RetryOperationThrowsWhenThreadSleepIsNull()
        {
            Action action = () => new OperationWithExponentialBackoff<IOException>(null, log, () => { });
            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public async Task OperationIsNotRetriedOnInvalidException()
        {
            void Operation()
            {
                throw new Exception();
            }

            var retryOperation = new OperationWithExponentialBackoff<IOException>(new MockThreadSleep(), log, Operation);
            var action = retryOperation.ExecuteAsync();
            await action.ShouldThrowAsync<Exception>();
        }

        [Test]
        public async Task OperationIsRetriedOnIoException()
        {
            var operationCount = 0;

            void Operation()
            {
                operationCount++;
                if (operationCount < 2)
                {
                    throw new IOException();
                }
            }

            var retryOperation = new OperationWithExponentialBackoff<IOException>(new MockThreadSleep(), log, Operation);
            await retryOperation.ExecuteAsync();

            operationCount.ShouldBe(2);
        }

        [Test]
        public async Task OperationIsRetriedAMaximumNumberOfTimesAsync()
        {
            const int numberOfRetries = 3;
            var operationCount = 0;

            void Operation()
            {
                operationCount++;
                throw new IOException();
            }

            var retryOperation = new OperationWithExponentialBackoff<IOException>(new MockThreadSleep(), log, Operation, numberOfRetries);
            var action = retryOperation.ExecuteAsync();
            await action.ShouldThrowAsync<AggregateException>();

            operationCount.ShouldBe(numberOfRetries + 1);
        }

        [Test]
        public async Task OperationDelayDoublesBetweenRetries()
        {
            const int numberOfRetries = 3;
            var expectedSleepMSec = 500;
            var sleepCount = 0;

            void Operation() => throw new IOException();

            Task Validator(int u)
            {
                return Task.Run(() =>
                {
                    sleepCount++;
                    u.ShouldBe(expectedSleepMSec);
                    expectedSleepMSec *= 2;
                });
            }

            var retryOperation = new OperationWithExponentialBackoff<IOException>(new MockThreadSleep(Validator), log, Operation, numberOfRetries);
            var action = retryOperation.ExecuteAsync();
            await action.ShouldThrowAsync<AggregateException>();

            // action.ShouldThrow<AggregateException>();

            sleepCount.ShouldBe(numberOfRetries);
        }

        [Test]
        public async Task TotalSleepTimeForSixRetriesIsAboutThirtySecondsAsync()
        {
            const int numberOfRetries = 6;
            var totalSleep = 0;

            void Operation()
            {
                throw new IOException();
            }

            Task Validator(int u)
            {
                return Task.Run(() => { totalSleep += u; });
            }

            var retryOperation = new OperationWithExponentialBackoff<IOException>(new MockThreadSleep(Validator), log, Operation, numberOfRetries);

            var action = retryOperation.ExecuteAsync();
            await action.ShouldThrowAsync<AggregateException>();
            // Action action = () => retryOperation.ExecuteAsync();
            // action.ShouldThrow<AggregateException>();

            // Exact number is 31,5 seconds
            totalSleep.ShouldBe(31500);
        }
    }
}
