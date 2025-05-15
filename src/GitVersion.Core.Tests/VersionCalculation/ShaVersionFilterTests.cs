using GitVersion.Core.Tests.Helpers;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests;

[TestFixture]
public class ShaVersionFilterTests : TestBase
{
    [Test]
    public void VerifyNullGuard()
    {
        var commit = GitRepositoryTestingExtensions.CreateMockCommit();
        var sut = new ShaVersionFilter([commit.Sha]);

        Should.Throw<ArgumentNullException>(() => sut.Exclude((IBaseVersion)null!, out _));
    }

    [Test]
    public void WhenShaMatchShouldExcludeWithReason()
    {
        var commit = GitRepositoryTestingExtensions.CreateMockCommit();
        BaseVersion version = new("dummy", new SemanticVersion(1), commit);
        var sut = new ShaVersionFilter([commit.Sha]);

        sut.Exclude(version, out var reason).ShouldBeTrue();
        reason.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void WhenShaMismatchShouldNotExclude()
    {
        var commit = GitRepositoryTestingExtensions.CreateMockCommit();
        BaseVersion version = new("dummy", new SemanticVersion(1), commit);
        var sut = new ShaVersionFilter(["mismatched"]);

        sut.Exclude(version, out var reason).ShouldBeFalse();
        reason.ShouldBeNull();
    }

    [Test]
    public void ExcludeShouldAcceptVersionWithNullCommit()
    {
        BaseVersion version = new("dummy", new SemanticVersion(1));
        var sut = new ShaVersionFilter(["mismatched"]);

        sut.Exclude(version, out var reason).ShouldBeFalse();
        reason.ShouldBeNull();
    }
}
