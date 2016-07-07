using System;
using System.IO;
using GitVersion.Helpers;
using NUnit.Framework;
using Shouldly;

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
    public void OperationIsNotRetriedOnInvalidException()
    {
        Action operation = () =>
        {
            throw new Exception();
        };

        var retryOperation = new OperationWithExponentialBackoff<IOException>(new MockThreadSleep(), operation);
        Action action = () => retryOperation.Execute();
        action.ShouldThrow<Exception>();
    }

    [Test]
    public void OperationIsRetriedOnIOException()
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
        retryOperation.Execute();

        operationCount.ShouldBe(2);
    }

    [Test]
    public void OperationIsRetriedAMaximumNumberOfTimes()
    {
        const int numberOfRetries = 3;
        var operationCount = 0;

        Action operation = () =>
        {
            operationCount++;
            throw new IOException();
        };

        var retryOperation = new OperationWithExponentialBackoff<IOException>(new MockThreadSleep(), operation, numberOfRetries);
        Action action = () => retryOperation.Execute();
        action.ShouldThrow<AggregateException>();

        operationCount.ShouldBe(numberOfRetries + 1);
    }

    [Test]
    public void OperationDelayDoublesBetweenRetries()
    {
        const int numberOfRetries = 3;
        var expectedSleepMSec = 500;
        var sleepCount = 0;

        Action operation = () =>
        {
            throw new IOException();
        };

        Action<int> validator = u =>
        {
            sleepCount++;
            u.ShouldBe(expectedSleepMSec);
            expectedSleepMSec *= 2;
        };

        var retryOperation = new OperationWithExponentialBackoff<IOException>(new MockThreadSleep(validator), operation, numberOfRetries);
        Action action = () => retryOperation.Execute();
        action.ShouldThrow<AggregateException>();

        sleepCount.ShouldBe(numberOfRetries);
    }

    [Test]
    public void TotalSleepTimeForSixRetriesIsAboutThirtySeconds()
    {
        const int numberOfRetries = 6;
        int totalSleep = 0;

        Action operation = () =>
        {
            throw new IOException();
        };

        Action<int> validator = u =>
        {
            totalSleep += u;
        };

        var retryOperation = new OperationWithExponentialBackoff<IOException>(new MockThreadSleep(validator), operation, numberOfRetries);
        Action action = () => retryOperation.Execute();
        action.ShouldThrow<AggregateException>();

        // Exact number is 31,5 seconds
        totalSleep.ShouldBe(31500);
    }
}
