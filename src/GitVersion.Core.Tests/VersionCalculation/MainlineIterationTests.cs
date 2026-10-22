using GitVersion.Configuration;
using GitVersion.Git;
using GitVersion.VersionCalculation;
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
    public void CopiedHistoryRetainsNewParentsWithoutCalculatedState()
    {
        var configuration = new BranchConfiguration { Increment = IncrementStrategy.Patch };
        var original = new MainlineIteration("original", new("refs/heads/main"), configuration, null, null);
        var merge = original.CreateCommit(Substitute.For<ICommit>(), original.BranchName, configuration);
        var history = new MainlineIteration("history", new("refs/heads/support/1.x"), configuration, original, merge);
        var source = history.CreateCommit(Substitute.For<ICommit>(), history.BranchName, configuration);
        var tag = new SemanticVersion(1, 0, 0);
        source.AddSemanticVersions(tag);
        source.Increment = VersionField.Major;
        var nested = new MainlineIteration("nested", new("refs/heads/feature/foo"), configuration, history, source);
        nested.CreateCommit(Substitute.For<ICommit>(), nested.BranchName, configuration);
        source.AddChildIteration(nested);

        var receiver = new MainlineIteration("receiver", original.BranchName, configuration, null, null);
        var newMerge = receiver.CreateCommit(Substitute.For<ICommit>(), receiver.BranchName, configuration);
        var copy = new MainlineIteration("copy", history.BranchName, configuration, receiver, newMerge);

        copy.CopyHistoryFrom(history);

        copy.HistoryIdentity.ShouldBeSameAs(history.HistoryIdentity);
        var copiedCommit = copy.Commits.Single();
        copiedCommit.ParentIteration.ShouldBeSameAs(receiver);
        copiedCommit.ParentCommit.ShouldBeSameAs(newMerge);
        copiedCommit.Value.ShouldBeSameAs(source.Value);
        copiedCommit.SemanticVersions.ShouldBe([tag]);
        copiedCommit.Increment.ShouldBe(VersionField.None);
        var copiedChild = copiedCommit.ChildIteration.ShouldNotBeNull();
        copiedChild.ParentIteration.ShouldBeSameAs(copy);
        copiedChild.ParentCommit.ShouldBeSameAs(copiedCommit);
        copiedChild.Commits.Single().ParentIteration.ShouldBeSameAs(copy);
        copiedChild.Commits.Single().ParentCommit.ShouldBeSameAs(copiedCommit);
        source.ParentCommit.ShouldBeSameAs(merge);
        nested.ParentIteration.ShouldBeSameAs(history);
    }

    [Test]
    public void VersionCacheDistinguishesRecordedTagHistoriesAtTheSameTip()
    {
        var branch = new BranchConfiguration { Increment = IncrementStrategy.Patch, IsMainBranch = true, Label = "" };
        var configuration = GitFlowConfigurationBuilder.New.WithVersionStrategies(VersionStrategies.Mainline).Build();
        var tip = GitRepositoryTestingExtensions.CreateMockCommit();
        var receiverTip = GitRepositoryTestingExtensions.CreateMockCommit();
        var mergeValue = GitRepositoryTestingExtensions.CreateMockCommit();
        mergeValue.Parents.Returns([receiverTip, tip]);
        var receiver = new MainlineIteration("receiver", new("refs/heads/main"), branch, null, null);
        var merge = receiver.CreateCommit(mergeValue, receiver.BranchName, branch);
        var first = new MainlineIteration("first", new("refs/heads/support/1.x"), branch, receiver, merge);
        first.CreateCommit(tip, first.BranchName, branch).AddSemanticVersions(new SemanticVersion(2, 0, 0));
        var second = new MainlineIteration("second", first.BranchName, branch, receiver, merge);
        second.CreateCommit(tip, second.BranchName, branch).AddSemanticVersions(new SemanticVersion(3, 0, 0));
        var context = new MainlineContext(Substitute.For<IIncrementStrategyFinder>(), configuration, new TestEnvironment(), tip);

        var firstVersion = MainlineVersionStrategy.GetMergedMainlineVersion(first, context);
        var secondVersion = MainlineVersionStrategy.GetMergedMainlineVersion(second, context);

        firstVersion.GetIncrementedVersion().Major.ShouldBe(2);
        secondVersion.GetIncrementedVersion().Major.ShouldBe(3);
        context.MainlineVersions.Count.ShouldBe(2);
        var copy = new MainlineIteration("copy", first.BranchName, branch, receiver, merge);
        copy.CopyHistoryFrom(first);
        MainlineVersionStrategy.GetMergedMainlineVersion(copy, context).ShouldBeSameAs(firstVersion);
        context.MainlineVersions.Count.ShouldBe(2);
    }
}
