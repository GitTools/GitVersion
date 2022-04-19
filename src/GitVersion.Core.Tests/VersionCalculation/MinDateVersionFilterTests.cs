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
        var version = new BaseVersion("dummy", false, new SemanticVersion(1), commit, string.Empty);
        var futureDate = DateTimeOffset.UtcNow.AddYears(1);
        var sut = new MinDateVersionFilter(futureDate);

        sut.Exclude(version, out var reason).ShouldBeTrue();
        reason.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void WhenShaMismatchShouldNotExclude()
    {
        var commit = GitToolsTestingExtensions.CreateMockCommit();
        var version = new BaseVersion("dummy", false, new SemanticVersion(1), commit, string.Empty);
        var pastDate = DateTimeOffset.UtcNow.AddYears(-1);
        var sut = new MinDateVersionFilter(pastDate);

        sut.Exclude(version, out var reason).ShouldBeFalse();
        reason.ShouldBeNull();
    }

    [Test]
    public void ExcludeShouldAcceptVersionWithNullCommit()
    {
        var version = new BaseVersion("dummy", false, new SemanticVersion(1), null, string.Empty);
        var futureDate = DateTimeOffset.UtcNow.AddYears(1);
        var sut = new MinDateVersionFilter(futureDate);

        sut.Exclude(version, out var reason).ShouldBeFalse();
        reason.ShouldBeNull();
    }
}
