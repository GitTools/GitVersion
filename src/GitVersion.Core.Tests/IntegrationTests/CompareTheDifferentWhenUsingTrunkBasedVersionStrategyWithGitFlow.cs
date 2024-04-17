using GitVersion.Configuration;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class CompareTheDifferentWhenUsingTrunkBasedVersionStrategyWithGitFlow
{
    private static GitFlowConfigurationBuilder configurationBuilder => GitFlowConfigurationBuilder.New;

    /// <summary>
    /// GitFlow - Feature branch (Increment patch on main and inherit on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeature1(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder.Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.2");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-foo.1+0", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-foo.1+1", configuration);

        fixture.ApplyTag("0.0.3-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-foo.2+0", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-foo.2+1", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-foo.2+2", configuration);

        fixture.MergeTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-4", configuration);
    }

    /// <summary>
    /// GitFlow - Merge main to feature branch (Increment patch on main and inherit on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureMergeMainToFeature2(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder.Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.2");
        fixture.MergeTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-foo.1+3", configuration);

        fixture.ApplyTag("0.0.3-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-foo.2+1", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-foo.2+2", configuration);

        fixture.MergeTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-6", configuration);
    }

    /// <summary>
    /// GitFlow - Pull requests (increment patch on main and inherit on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsurePullRequest(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder.Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-PullRequest2.2", configuration);

        fixture.Checkout("main");
        fixture.Remove("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-2", configuration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EnsureReleaseAndFeatureBranch1(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder.Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.BranchTo("release/2.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+3", configuration);

        fixture.ApplyTag("2.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.2+1", configuration);

        fixture.Checkout("release/2.0.0");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo("release/2.0.0", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-1", configuration);
    }

    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.0.2")]
    [TestCase(true, "release/0.0.2")]
    [TestCase(true, "release/0.1.0")]
    public void EnsureReleaseAndFeatureBranch2(bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder.Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+3", configuration);

        fixture.ApplyTag("0.1.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.2+1", configuration);

        fixture.Checkout(releaseBranch);
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo(releaseBranch, removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.1-1", configuration);
    }

    /// <summary>
    /// GitFlow - Release branch (Increment patch on main)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureReleaseBranch1(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder.Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.BranchTo("release/2.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.2");
        fixture.MergeTo("release/2.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+3", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+4", configuration);

        fixture.ApplyTag("2.0.0-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.2+0", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.2+1", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-1", configuration);
    }

    /// <summary>
    /// GitFlow - Release branch (Increment patch on main and none on release)
    /// </summary>
    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.0.2")]
    [TestCase(true, "release/0.0.2")]
    [TestCase(true, "release/0.1.0")]
    public void EnsureReleaseBranch2(bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder.Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.2");
        fixture.MergeTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+3", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+4", configuration);

        fixture.ApplyTag("0.1.0-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.2+0", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.2+1", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.1-1", configuration);
    }
}
