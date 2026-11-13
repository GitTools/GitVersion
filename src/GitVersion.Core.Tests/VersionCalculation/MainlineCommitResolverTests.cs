using GitVersion.Configuration;
using GitVersion.Git;
using GitVersion.VersionCalculation.Mainline;

namespace GitVersion.Tests.VersionCalculation;

[TestFixture]
public class MainlineCommitResolverTests
{
    [Test]
    public void ConstructorRejectsNullIteration()
    {
        var exception = Should.Throw<ArgumentNullException>(() => new MainlineCommitResolver(null!));

        exception.ParamName.ShouldBe("iteration");
    }

    [Test]
    public void ConstructorRejectsChildIteration()
    {
        var root = Iteration("root", "main");
        var child = Iteration("child", "support/1.x", root);

        var exception = Should.Throw<ArgumentException>(() => new MainlineCommitResolver(child));

        exception.ParamName.ShouldBe("iteration");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ResolveRejectsNullIterationEvenWhenCommitIsNull(bool nullCommit)
    {
        IMainlineCommitResolver resolver = new MainlineCommitResolver(Iteration("root", "main"));

        var exception = Should.Throw<ArgumentNullException>(() => resolver.Resolve(null!, nullCommit ? null : Commit()));

        exception.ParamName.ShouldBe("iteration");
    }

    [Test]
    public void NullCommitDoesNotResolveToAnExistingBranchPoint()
    {
        var root = Iteration("root", "main");
        var branchPoint = root.CreateCommit(Commit(), root.BranchName, root.Configuration);
        var child = Iteration("child", "support/1.x", root, branchPoint.Value);
        IMainlineCommitResolver resolver = new MainlineCommitResolver(root);

        resolver.Resolve(child, null).ShouldBeNull();
        resolver.Resolve(child, child.BranchedFrom).ShouldBeSameAs(branchPoint);
    }

    [Test]
    public void RequestedCommitTakesPrecedenceOverTheIterationsBranchPoint()
    {
        var root = Iteration("root", "main");
        var branchPoint = root.CreateCommit(Commit(), root.BranchName, root.Configuration);
        var child = Iteration("child", "support/1.x", root, branchPoint.Value);
        var local = child.CreateCommit(Commit(), child.BranchName, child.Configuration);
        IMainlineCommitResolver resolver = new MainlineCommitResolver(root);

        resolver.Resolve(child, local.Value).ShouldBeSameAs(local);
        resolver.Resolve(child, branchPoint.Value).ShouldBeSameAs(branchPoint);
        resolver.Resolve(child, Commit()).ShouldBeNull();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ResolvePrefersLocalThenSameBranchNodesOverOtherBranches(bool hasLocal)
    {
        var root = Iteration("root", "main");
        var merge = root.CreateCommit(Commit(), root.BranchName, root.Configuration);
        var value = Commit();
        root.CreateCommit(value, root.BranchName, root.Configuration);
        var earlier = Iteration("earlier", "support/1.x", root);
        var sameBranch = earlier.CreateCommit(value, earlier.BranchName, earlier.Configuration);
        merge.AddChildIteration(earlier);
        var current = Iteration("current", "support/1.x", root);
        var local = hasLocal ? current.CreateCommit(value, current.BranchName, current.Configuration) : null;
        IMainlineCommitResolver resolver = new MainlineCommitResolver(root);

        resolver.Resolve(current, value).ShouldBeSameAs(local ?? sameBranch);
    }

    [Test]
    public void RootWithoutABranchPointRemainsUnresolved()
    {
        var root = Iteration("root", "main");
        var initial = root.CreateCommit(Commit(), root.BranchName, root.Configuration);
        var resolver = new MainlineCommitResolver(root);

        resolver.Resolve(root, root.BranchedFrom).ShouldBeNull();
        MainlineTestCalculation.SelectHistory(root, root).ShouldBe([initial]);
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
        var resolver = new MainlineCommitResolver(root);

        resolver.Resolve(second, second.BranchedFrom).ShouldBeNull();
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
        var resolver = new MainlineCommitResolver(root);

        resolver.Resolve(second, second.BranchedFrom).ShouldBeSameAs(d);
        resolver.Resolve(first, first.BranchedFrom).ShouldBeSameAs(b);
        var history = MainlineTestCalculation.SelectHistory(root, second).ToArray();
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
        var resolver = new MainlineCommitResolver(root);

        resolver.Resolve(source, source.BranchedFrom).ShouldBeSameAs(branchPoint);
        MainlineTestCalculation.SelectHistory(root, source).ShouldBe([branchPoint]);
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

        new MainlineCommitResolver(root).Resolve(source, source.BranchedFrom).ShouldBeNull();
        new MainlineCommitResolver(otherRoot).Resolve(otherRoot, foreign.Value).ShouldBeSameAs(foreign);
        root.Commits.ShouldBeEmpty();
    }

    private static MainlineIteration Iteration(string id, string branch, MainlineIteration? parent = null, ICommit? branchedFrom = null)
        => new(id, new($"refs/heads/{branch}"), new BranchConfiguration { Increment = IncrementStrategy.Patch },
            parent, branchedFrom ?? (parent is null ? null : Commit()), parent is null ? null : Commit());

    private static ICommit Commit()
    {
        var commit = GitRepositoryTestingExtensions.CreateMockCommit();
        commit.Equals(commit).Returns(true);
        return commit;
    }
}
