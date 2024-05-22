using GitVersion.Configuration;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class CompareTheDifferentWhenUsingMainlineVersionStrategyWithGitFlow
{
    private static GitFlowConfigurationBuilder configurationBuilder => GitFlowConfigurationBuilder.New;

    /// <summary>
    /// GitFlow - Feature branch (Increment patch on main and inherit on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeature(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
            : configurationBuilder;
        var configuration = builder.Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1", configuration);

        if (!useMainline) fixture.ApplyTag("0.0.1");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-1", configuration);

        if (!useMainline) fixture.ApplyTag("0.0.2");
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
    public void EnsureMergeMainToFeature(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
            : configurationBuilder;
        var configuration = builder.Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1", configuration);

        if (!useMainline) fixture.ApplyTag("0.0.1");
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

        if (!useMainline) fixture.ApplyTag("0.0.2");
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
    public void EnsurePullRequest(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
            : configurationBuilder;
        var configuration = builder.Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1", configuration);

        if (!useMainline) fixture.ApplyTag("0.0.1");
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
    public void EnsureReleaseAndFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
            : configurationBuilder;
        var configuration = builder.Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1", configuration);

        if (!useMainline) fixture.ApplyTag("0.0.1");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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
    public void EnsureReleaseAndFeatureBranch(bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
            : configurationBuilder;
        var configuration = builder.Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1", configuration);

        if (!useMainline) fixture.ApplyTag("0.0.1");
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.1-1", configuration);
    }

    /// <summary>
    /// GitFlow - Release branch (Increment patch on main)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureReleaseBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
            : configurationBuilder;
        var configuration = builder.Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1", configuration);

        if (!useMainline) fixture.ApplyTag("0.0.1");
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

        if (!useMainline) fixture.ApplyTag("0.0.2");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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
    public void EnsureReleaseBranch(bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
            : configurationBuilder;
        var configuration = builder.Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1", configuration);

        if (!useMainline) fixture.ApplyTag("0.0.1");
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

        if (!useMainline) fixture.ApplyTag("0.0.2");
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.1-1", configuration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeatureDevelopmentWithDevelopBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
            : configurationBuilder;
        var configuration = builder.Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1", configuration);

        if (!useMainline) fixture.ApplyTag("0.0.1");
        fixture.BranchTo("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.1", configuration);

        fixture.ApplyTag("0.1.0-alpha.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.2", configuration);

        fixture.BranchTo("feature/foo");

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

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.2+2", configuration);

        fixture.Checkout("develop");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-PullRequest2.6", configuration);

        fixture.Checkout("feature/foo");
        fixture.Repository.Branches.Remove("pull/2/merge");
        fixture.MergeTo("develop", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.6", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/3/merge");
        fixture.MergeNoFF("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-PullRequest3.7", configuration);

        fixture.Checkout("develop");
        fixture.Repository.Branches.Remove("pull/3/merge");
        fixture.MergeTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-7", configuration);

        if (!useMainline) fixture.ApplyTag("0.1.0");
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.1-1", configuration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeatureDevelopmentWithDevelopBranchFast(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
            : configurationBuilder;
        var configuration = builder.Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1", configuration);

        if (!useMainline) fixture.ApplyTag("0.0.1");
        fixture.BranchTo("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.0", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+1", configuration);

        fixture.ApplyTag("0.1.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.2+0", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.2+1", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.2+2", configuration);

        fixture.Checkout("develop");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-PullRequest2.4", configuration);

        fixture.Checkout("feature/foo");
        fixture.Repository.Branches.Remove("pull/2/merge");
        fixture.MergeTo("develop", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.4", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/3/merge");
        fixture.MergeNoFF("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-PullRequest3.5", configuration);

        fixture.Checkout("develop");
        fixture.Repository.Branches.Remove("pull/3/merge");
        fixture.MergeTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-5", configuration);

        if (!useMainline) fixture.ApplyTag("0.1.0");
        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.1-1", configuration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EnsureDevelopmentWithMainBranchFast(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
            : configurationBuilder;
        var configuration = builder.Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1", configuration);

        if (!useMainline) fixture.ApplyTag("0.0.1");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.1+1", configuration);

        fixture.ApplyTag("0.0.2-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.2+0", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.2+1", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.2+2", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-PullRequest2.4", configuration);

        fixture.Checkout("feature/foo");
        fixture.Repository.Branches.Remove("pull/2/merge");
        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-4", configuration);

        if (!useMainline) fixture.ApplyTag("0.0.2");
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-1", configuration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EnsureBugFixWithMainBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
            : configurationBuilder;
        var configuration = builder.Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1", configuration);

        if (!useMainline) fixture.ApplyTag("0.0.1");
        fixture.BranchTo("hotfix/bar");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.1+1", configuration);

        fixture.ApplyTag("0.0.2-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.2+0", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.2+1", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.2+2", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("hotfix/bar");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-PullRequest2.4", configuration);

        fixture.Checkout("hotfix/bar");
        fixture.Repository.Branches.Remove("pull/2/merge");
        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-4", configuration);

        if (!useMainline) fixture.ApplyTag("0.0.2");
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-1", configuration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EnsureBugFixWithDevelopBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
            : configurationBuilder;
        var configuration = builder.Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1", configuration);

        if (!useMainline) fixture.ApplyTag("0.0.1");
        fixture.BranchTo("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.1", configuration);

        fixture.BranchTo("hotfix/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.1+2", configuration);

        fixture.ApplyTag("0.0.2-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.2+0", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.2+1", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.2+2", configuration);

        fixture.Checkout("develop");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("hotfix/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-PullRequest2.5", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/3/merge");
        fixture.MergeNoFF("hotfix/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-PullRequest3.5", configuration);

        fixture.Checkout("hotfix/foo");
        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-5", configuration);

        if (!useMainline) fixture.ApplyTag("0.0.2");
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-1", configuration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EnsureBugFixWithDevelopBranchFast(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
            : configurationBuilder;
        var configuration = builder.Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1", configuration);

        if (!useMainline) fixture.ApplyTag("0.0.1");
        fixture.BranchTo("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.0", configuration);

        fixture.BranchTo("hotfix/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.1+1", configuration);

        fixture.ApplyTag("0.0.2-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.2+0", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.2+1", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.2+2", configuration);

        fixture.Checkout("develop");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("hotfix/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-PullRequest2.4", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/3/merge");
        fixture.MergeNoFF("hotfix/foo");

        // ❔ expected: "0.0.2-PullRequest3.4"
        fixture.AssertFullSemver("0.1.0-PullRequest3.4", configuration);

        fixture.Checkout("hotfix/foo");
        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-4", configuration);

        if (!useMainline) fixture.ApplyTag("0.0.2");
        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-1", configuration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeatureDevelopmentWithReleaseNextBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
            : configurationBuilder;
        var configuration = builder.Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1", configuration);

        if (!useMainline) fixture.ApplyTag("0.0.1");
        fixture.BranchTo("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.1", configuration);

        fixture.BranchTo("release/next");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+1", configuration);

        fixture.Checkout("develop");
        fixture.MakeACommit();
        fixture.Checkout("release/next");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+1", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+2", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+3", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+3", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+4", configuration);

        fixture.ApplyTag("0.1.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.2+1", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.2+2", configuration);

        fixture.Checkout("release/next");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-PullRequest2.7", configuration);

        fixture.Checkout("feature/foo");
        fixture.Repository.Branches.Remove("pull/2/merge");
        fixture.MergeTo("release/next", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+7", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/3/merge");
        fixture.MergeNoFF("release/next");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-PullRequest3.8", configuration);

        fixture.Checkout("release/next");
        fixture.Repository.Branches.Remove("pull/3/merge");
        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-8", configuration);

        if (!useMainline) fixture.ApplyTag("0.1.0");
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.1-1", configuration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeatureDevelopmentWithReleaseNextBranchFast(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
            : configurationBuilder;
        var configuration = builder.Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1", configuration);

        if (!useMainline) fixture.ApplyTag("0.0.1");
        fixture.BranchTo("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.0", configuration);

        fixture.BranchTo("release/next");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+0", configuration);

        fixture.Checkout("develop");
        fixture.MakeACommit();
        fixture.Checkout("release/next");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+2", configuration);

        fixture.BranchTo("feature/foo");

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

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.2+2", configuration);

        fixture.Checkout("release/next");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-PullRequest2.6", configuration);

        fixture.Checkout("feature/foo");
        fixture.Repository.Branches.Remove("pull/2/merge");
        fixture.MergeTo("release/next", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+6", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/3/merge");
        fixture.MergeNoFF("release/next");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-PullRequest3.7", configuration);

        fixture.Checkout("release/next");
        fixture.Repository.Branches.Remove("pull/3/merge");
        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-7", configuration);

        if (!useMainline) fixture.ApplyTag("0.1.0");
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.1-1", configuration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeatureDevelopmentWithRelease100Branch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
            : configurationBuilder;
        var configuration = builder.Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1", configuration);

        if (!useMainline) fixture.ApplyTag("0.0.1");
        fixture.BranchTo("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.1", configuration);

        fixture.BranchTo("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);

        fixture.Checkout("develop");
        fixture.MakeACommit();
        fixture.Checkout("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+2", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+3", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+3", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+4", configuration);

        fixture.ApplyTag("1.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.2+1", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.2+2", configuration);

        fixture.Checkout("release/1.0.0");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-PullRequest2.7", configuration);

        fixture.Checkout("feature/foo");
        fixture.Repository.Branches.Remove("pull/2/merge");
        fixture.MergeTo("release/1.0.0", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+7", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/3/merge");
        fixture.MergeNoFF("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-PullRequest3.8", configuration);

        fixture.Checkout("release/1.0.0");
        fixture.Repository.Branches.Remove("pull/3/merge");
        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-8", configuration);

        if (!useMainline) fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-1", configuration);
    }
}
