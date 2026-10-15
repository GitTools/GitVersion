using GitVersion.Configuration;
using GitVersion.Git;
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

    [Test]
    public void IncludedCommitsAreAnImmutableSnapshot()
    {
        var included = Substitute.For<ICommit>();
        var excluded = Substitute.For<ICommit>();
        HashSet<ICommit> selection = [included];
        var iteration = new MainlineIteration("id", new("main"), new BranchConfiguration(), null, null, selection);

        selection.Clear();
        selection.Add(excluded);

        iteration.IncludedCommits.ShouldNotBeNull();
        iteration.IncludedCommits.Count.ShouldBe(1);
        iteration.IncludedCommits.Single().ShouldBeSameAs(included);
    }

    [Test]
    public void IndependentHistoryDoesNotInheritParentCommitSelection()
    {
        var configuration = new BranchConfiguration();
        var parent = new MainlineIteration("parent", new("main"), configuration, null, null,
            new HashSet<ICommit> { Substitute.For<ICommit>() });

        var independent = new MainlineIteration("independent", new("support"), configuration, parent, null);

        independent.IncludedCommits.ShouldBeNull();
        parent.IncludedCommits.ShouldNotBeNull();
    }
}
