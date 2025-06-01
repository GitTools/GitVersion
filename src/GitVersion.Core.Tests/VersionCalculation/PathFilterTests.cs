using GitVersion.Core.Tests.Helpers;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests;

[TestFixture]
public class PathFilterTests : TestBase
{
    [Test]
    public void VerifyNullGuard()
    {
        var sut = new PathFilter([]);

        Should.Throw<ArgumentNullException>(() => sut.Exclude((IBaseVersion)null!, out _));
    }

    [Test]
    public void WhenPathMatchShouldExcludeWithReason()
    {
        var commit = GitRepositoryTestingExtensions.CreateMockCommit(["/path"]);
        BaseVersion version = new("dummy", new SemanticVersion(1), commit);
        var sut = new PathFilter(commit.DiffPaths);

        sut.Exclude(version, out var reason).ShouldBeTrue();
        reason.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void WhenPathMismatchShouldNotExclude()
    {
        var commit = GitRepositoryTestingExtensions.CreateMockCommit(["/path"]);
        BaseVersion version = new("dummy", new SemanticVersion(1), commit);
        var sut = new PathFilter(["/another_path"]);

        sut.Exclude(version, out var reason).ShouldBeFalse();
        reason.ShouldBeNull();
    }

    [Test]
    public void ExcludeShouldAcceptVersionWithNullCommit()
    {
        BaseVersion version = new("dummy", new SemanticVersion(1));
        var sut = new PathFilter(["/path"]);

        sut.Exclude(version, out var reason).ShouldBeFalse();
        reason.ShouldBeNull();
    }
}
