using System;
using System.IO;
using GitVersion.Helpers;
using NUnit.Framework;
using Shouldly;
using System.Threading.Tasks;

[TestFixture]
public class OperationWithExponentialBackoffTests
{
    [Test]
    public void RetryOperationThrowsWhenNegativeMaxRetries()
    {
        Action action = () => new OperationWithExponentialBackoff<IOException>(new MockThreadSleep(), () => { }, -1);
        action.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Test]
    public void RetryOperationThrowsWhenThreadSleepIsNull()
    {
        Action action = () => new OperationWithExponentialBackoff<IOException>(null, () => { });
        action.ShouldThrow<ArgumentNullException>();
    }

    [Test]
    public async Task OperationIsNotRetriedOnInvalidException()
    {
        Action operation = () =>
        {
            throw new Exception();
        };

        var retryOperation = new OperationWithExponentialBackoff<IOException>(new MockThreadSleep(), operation);
        Task action = retryOperation.ExecuteAsync();
        await action.ShouldThrowAsync<Exception>();
    }

    [Test]
    public async Task OperationIsRetriedOnIOException()
    {
        var operationCount = 0;

        Action operation = () =>
        {
            operationCount++;
            if (operationCount < 2)
            {
                throw new IOException();
            }
        };

        var retryOperation = new OperationWithExponentialBackoff<IOException>(new MockThreadSleep(), operation);
        await retryOperation.ExecuteAsync();

        operationCount.ShouldBe(2);
    }

    [Test]
    public async Task OperationIsRetriedAMaximumNumberOfTimesAsync()
    {
        const int numberOfRetries = 3;
        var operationCount = 0;

        Action operation = () =>
        {
            operationCount++;
            throw new IOException();
        };

        var retryOperation = new OperationWithExponentialBackoff<IOException>(new MockThreadSleep(), operation, numberOfRetries);
        Task action = retryOperation.ExecuteAsync();
        await action.ShouldThrowAsync<AggregateException>();

        operationCount.ShouldBe(numberOfRetries + 1);
    }

    [Test]
    public async Task OperationDelayDoublesBetweenRetries()
    {
        const int numberOfRetries = 3;
        var expectedSleepMSec = 500;
        var sleepCount = 0;

        Action operation = () =>
        {
            throw new IOException();
        };

        Func<int, Task> validator = (u) =>
        {
            return Task.Run(() =>
                               {
                                   sleepCount++;
                                   u.ShouldBe(expectedSleepMSec);
                                   expectedSleepMSec *= 2;
                               });

        };

        var retryOperation = new OperationWithExponentialBackoff<IOException>(new MockThreadSleep(validator), operation, numberOfRetries);
        Task action = retryOperation.ExecuteAsync();
        await action.ShouldThrowAsync<AggregateException>();

        // action.ShouldThrow<AggregateException>();

        sleepCount.ShouldBe(numberOfRetries);
    }

    [Test]
    public async Task TotalSleepTimeForSixRetriesIsAboutThirtySecondsAsync()
    {
        const int numberOfRetries = 6;
        int totalSleep = 0;

        Action operation = () =>
        {
            throw new IOException();
        };

        Func<int, Task> validator = (u) =>
        {
            return Task.Run(() =>
            {
                totalSleep += u;
            });

        };

        var retryOperation = new OperationWithExponentialBackoff<IOException>(new MockThreadSleep(validator), operation, numberOfRetries);

        Task action = retryOperation.ExecuteAsync();
        await action.ShouldThrowAsync<AggregateException>();
        // Action action = () => retryOperation.ExecuteAsync();
        // action.ShouldThrow<AggregateException>();

        // Exact number is 31,5 seconds
        totalSleep.ShouldBe(31500);
    }
}
