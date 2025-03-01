using GitVersion.Core.Tests.Helpers;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests;

[TestFixture]
public class PathFilterTests : TestBase
{
    [Test]
    public void VerifyNullGuard() => Should.Throw<ArgumentNullException>(() => new PathFilter(null!, null!, null!));
}
