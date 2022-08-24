using GitVersion.Core.Tests.Helpers;
using GitVersion.VersionCalculation;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.Core.Tests;

[TestFixture]
public class MinDateVersionFilterTests : TestBase
{
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
