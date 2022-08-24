using GitVersion.Core.Tests.Helpers;
using GitVersion.VersionCalculation;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.Core.Tests;

[TestFixture]
public class ShaVersionFilterTests : TestBase
{
    [Test]
    public void WhenShaMatchShouldExcludeWithReason()
    {
        var commit = GitToolsTestingExtensions.CreateMockCommit();
        var sut = new ShaVersionFilter(new[] { commit.Sha });

        sut.Exclude(commit, out var reason).ShouldBeTrue();
        reason.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void WhenShaMismatchShouldNotExclude()
    {
        var commit = GitToolsTestingExtensions.CreateMockCommit();
        var sut = new ShaVersionFilter(new[] { "mismatched" });

        sut.Exclude(commit, out var reason).ShouldBeFalse();
        reason.ShouldBeNull();
    }
}
