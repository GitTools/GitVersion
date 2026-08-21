using GitVersion.Configuration;
using GitVersion.Git;
using GitVersion.VersionCalculation;

namespace GitVersion.Tests.VersionCalculation;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class IncrementStrategyFinderTests
{
    [Test]
    public void ParsesCachedCommitWithTheRequestedConfiguration()
    {
        var commit = Substitute.For<ICommit>();
        commit.Sha.Returns("0123456789012345678901234567890123456789");
        commit.Message.Returns("feature: custom increment");

        var finder = new IncrementStrategyFinder(
            new(() => throw new InvalidOperationException()),
            Substitute.For<IRepositoryStore>(),
            new StubTaggedSemanticVersionRepository(),
            Substitute.For<IEffectiveBranchConfigurationFinder>(),
            Substitute.For<IEnvironment>());
        var nonMatchingConfiguration = GitFlowConfigurationBuilder.New
            .WithMinorVersionBumpMessage("^minor:")
            .Build();
        var matchingConfiguration = GitFlowConfigurationBuilder.New
            .WithMinorVersionBumpMessage("^feature:")
            .Build();

        finder.GetIncrementForcedByCommit(commit, nonMatchingConfiguration).Increment
            .ShouldBe(VersionField.None);
        finder.GetIncrementForcedByCommit(commit, matchingConfiguration).Increment
            .ShouldBe(VersionField.Minor);
    }

    private sealed class StubTaggedSemanticVersionRepository : ITaggedSemanticVersionRepository
    {
        public ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersionsOfBranch(
            IBranch branch, string? tagPrefix, SemanticVersionFormat format, IIgnoreConfiguration ignore) =>
            throw new NotSupportedException();

        public ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersionsOfMergeTarget(
            IBranch branch, string? tagPrefix, SemanticVersionFormat format, IIgnoreConfiguration ignore) =>
            throw new NotSupportedException();

        public ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersions(
            string? tagPrefix, SemanticVersionFormat format, IIgnoreConfiguration ignore) =>
            throw new NotSupportedException();
    }
}
