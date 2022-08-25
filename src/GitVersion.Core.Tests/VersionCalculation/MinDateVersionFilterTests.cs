using GitVersion.Core.Tests.Helpers;
using GitVersion.VersionCalculation;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.Core.Tests;

[TestFixture]
public class MinDateVersionFilterTests : TestBase
{
    [Test]
    public void VerifyNullGuard()
    {
        var dummy = DateTimeOffset.UtcNow.AddSeconds(1.0);
        var sut = new MinDateVersionFilter(dummy);

        Should.Throw<ArgumentNullException>(() => sut.Exclude(null, out _));
    }

    [Test]
    public void WhenCommitShouldExcludeWithReason()
    {
        var commit = GitToolsTestingExtensions.CreateMockCommit();
        var futureDate = DateTimeOffset.UtcNow.AddYears(1);
        var sut = new MinDateVersionFilter(futureDate);

        sut.Exclude(commit, out var reason).ShouldBeTrue();
        reason.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void WhenShaMismatchShouldNotExclude()
    {
        var commit = GitToolsTestingExtensions.CreateMockCommit();
        var pastDate = DateTimeOffset.UtcNow.AddYears(-1);
        var sut = new MinDateVersionFilter(pastDate);

        sut.Exclude(commit, out var reason).ShouldBeFalse();
        reason.ShouldBeNull();
    }
}
