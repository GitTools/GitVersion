using GitVersion.Configuration;
using GitVersion.VersionCalculation.Mainline;

namespace GitVersion.Tests.VersionCalculation;

[TestFixture]
public class MainlineIterationTests
{
    [Test]
    public void ChildIsDeeperThanParent()
    {
        var configuration = new BranchConfiguration();
        var parent = new MainlineIteration("id0", new("canonical0"), configuration, null, null);
        var child = new MainlineIteration("id1", new("canonical1"), configuration, parent, null);

        parent.Depth.ShouldBe(1);
        child.Depth.ShouldBe(2);
    }
}
