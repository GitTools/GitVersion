using GitVersion.Core.Tests.Helpers;
using GitVersion.Git;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests;

[TestFixture]
public class PathFilterTests : TestBase
{
    [Test]
    public void VerifyNullGuard() => Should.Throw<ArgumentNullException>(() => new PathFilter(null!, null!, null!));

    [Test]
    public void VerifyNullGuard2()
    {
        var sut = new PathFilter(null!, null!, [""]);

        Should.Throw<ArgumentNullException>(() => sut.Exclude((ICommit?)null, out _));
    }
}
