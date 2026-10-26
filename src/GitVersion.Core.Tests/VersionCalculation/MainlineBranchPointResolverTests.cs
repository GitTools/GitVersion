using GitVersion.Configuration;
using GitVersion.Git;
using GitVersion.VersionCalculation.Mainline;

namespace GitVersion.Tests.VersionCalculation;

[TestFixture]
public class MainlineBranchPointResolverTests
{
    [Test]
    public void RootWithoutABranchPointRemainsUnresolved()
    {
        var root = Iteration("root", "main");
        var initial = root.CreateCommit(Commit(), root.BranchName, root.Configuration);
        var resolver = new MainlineBranchPointResolver(root);

        resolver.Resolve(root).ShouldBeNull();
        resolver.GetHistory(root).ShouldBe([initial]);
        root.Commits.ShouldBe([initial]);
    }

    [Test]
    public void MissingBranchPointDoesNotResolveToAnotherCommitOnTheSameBranch()
    {
        var root = Iteration("root", "main");
        var merge = root.CreateCommit(Commit(), root.BranchName, root.Configuration);
        var first = Iteration("first", "support/1.x", root);
        var recorded = first.CreateCommit(Commit(), first.BranchName, first.Configuration);
        merge.AddChildIteration(first);
        var second = Iteration("second", "support/1.x", root, Commit());
        var resolver = new MainlineBranchPointResolver(root);

        resolver.Resolve(second).ShouldBeNull();
        second.Commits.ShouldBeEmpty();
        first.Commits.ShouldBe([recorded]);
    }

    [Test]
    public void RepeatedBranchIterationsResolveTheExactEarlierCommitAndBoundTheirHistory()
    {
        var root = Iteration("root", "main");
        var secondMerge = root.CreateCommit(Commit(), root.BranchName, root.Configuration);
        var firstMerge = root.CreateCommit(Commit(), root.BranchName, root.Configuration);
        var b = root.CreateCommit(Commit(), root.BranchName, root.Configuration);
        var a = root.CreateCommit(Commit(), root.BranchName, root.Configuration);
        var first = Iteration("first", "support/1.x", root, b.Value);
        var d = first.CreateCommit(Commit(), first.BranchName, first.Configuration);
        var c = first.CreateCommit(Commit(), first.BranchName, first.Configuration);
        firstMerge.AddChildIteration(first);
        var second = Iteration("second", "support/1.x", root, d.Value);
        var e = second.CreateCommit(Commit(), second.BranchName, second.Configuration);
        secondMerge.AddChildIteration(second);
        var resolver = new MainlineBranchPointResolver(second);

        resolver.Resolve(second).ShouldBeSameAs(d);
        resolver.Resolve(first).ShouldBeSameAs(b);
        var history = resolver.GetHistory(second).ToArray();
        history.ShouldBe([a, b, c, d, e]);
        history[3].ShouldBeSameAs(d);
        history.ShouldNotContain(firstMerge);
        history.ShouldNotContain(secondMerge);
        second.Commits.ShouldBe([e]);
        first.Commits.ShouldBe([c, d]);
        d.Iteration.ShouldBeSameAs(first);
        b.Successor.ShouldBeSameAs(firstMerge);
        e.Predecessor.ShouldBeNull();
    }

    [Test]
    public void BranchPointCanBelongToANestedIterationOnAnotherBranch()
    {
        var root = Iteration("root", "main");
        var outerMerge = root.CreateCommit(Commit(), root.BranchName, root.Configuration);
        var earlier = Iteration("earlier", "support/2.x", root);
        var innerMerge = earlier.CreateCommit(Commit(), earlier.BranchName, earlier.Configuration);
        outerMerge.AddChildIteration(earlier);
        var nested = Iteration("nested", "feature/old", earlier);
        var branchPoint = nested.CreateCommit(Commit(), nested.BranchName, nested.Configuration);
        innerMerge.AddChildIteration(nested);
        var source = Iteration("source", "support/1.x", root, branchPoint.Value);
        var resolver = new MainlineBranchPointResolver(root);

        resolver.Resolve(source).ShouldBeSameAs(branchPoint);
        resolver.GetHistory(source).ShouldBe([branchPoint]);
        source.Commits.ShouldBeEmpty();
        branchPoint.Iteration.ShouldBeSameAs(nested);
    }

    [Test]
    public void ResolverDoesNotFindNodesFromAnotherCalculationGraph()
    {
        var root = Iteration("root", "main");
        var otherRoot = Iteration("other", "main");
        var foreign = otherRoot.CreateCommit(Commit(), otherRoot.BranchName, otherRoot.Configuration);
        var source = Iteration("source", "support/1.x", root, foreign.Value);

        new MainlineBranchPointResolver(root).Resolve(source).ShouldBeNull();
        new MainlineBranchPointResolver(otherRoot).FindCommit(otherRoot, foreign.Value).ShouldBeSameAs(foreign);
        root.Commits.ShouldBeEmpty();
    }

    private static MainlineIteration Iteration(string id, string branch, MainlineIteration? parent = null, ICommit? branchedFrom = null)
        => new(id, new($"refs/heads/{branch}"), new BranchConfiguration { Increment = IncrementStrategy.Patch },
            parent, branchedFrom, null);

    private static ICommit Commit()
    {
        var commit = GitRepositoryTestingExtensions.CreateMockCommit();
        commit.Equals(commit).Returns(true);
        return commit;
    }
}
