using GitVersion.Configuration;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class CompareTheDifferentWhenUsingTrunkBasedVersionStrategyWithGitFlow2
{
    private static GitFlowConfigurationBuilder configurationBuilder => GitFlowConfigurationBuilder.New;

    /// <summary>
    /// GitFlow - PullRequest to Develop branch (Increment patch on main, minor on development and minor on feature branch)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void GitFlowFeatureDevelopmentWithDevelopBranch(bool useTrunkBased)
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

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.1-1", configuration);
    }

    /// <summary>
    /// GitFlow - PullRequest to Develop branch (Increment patch on main, minor on development and minor on feature branch)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void GitFlowFeatureDevelopmentWithDevelopBranchFast(bool useTrunkBased)
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
        fixture.BranchTo("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.0", configuration);

        fixture.BranchTo("feature/foo");

        if (useTrunkBased)
        {
            // ❔ expected: "0.1.0-foo.1+0"
            fixture.AssertFullSemver("0.0.2-foo.1+0", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.1.0-foo.1+0", configuration);
        }

        fixture.MakeACommit("B");

        if (useTrunkBased)
        {
            // ❔ expected: "0.1.0-foo.1+1"
            fixture.AssertFullSemver("0.0.2-foo.1+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.1.0-foo.1+1", configuration);
        }

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

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.1-1", configuration);
    }

    /// <summary>
    /// GitFlow - Feature PullRequest to Main branch (Increment patch on main, minor on development and minor on feature branch)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void GitFlowFeatureDevelopmentWithMainBranchFast(bool useTrunkBased)
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

        if (!useTrunkBased) fixture.ApplyTag("0.0.2");
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-1", configuration);
    }

    /// <summary>
    /// GitFlow - Feature PullRequest to Main branch (Increment patch on main, minor on development and minor on feature branch)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void GitFlowBugFixWithMainBranch(bool useTrunkBased)
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

        if (!useTrunkBased) fixture.ApplyTag("0.0.2");
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-1", configuration);
    }

    /// <summary>
    /// GitFlow - Feature PullRequest to Main branch (Increment patch on main, minor on development and minor on feature branch)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void GitFlowBugFixWithDevelopBranch(bool useTrunkBased)
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

        if (!useTrunkBased) fixture.ApplyTag("0.0.2");
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-1", configuration);
    }

    /// <summary>
    /// GitFlow - Feature PullRequest to Main branch (Increment patch on main, minor on development and minor on feature branch)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void GitFlowBugFixWithDevelopBranchFast(bool useTrunkBased)
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

        if (useTrunkBased)
        {
            // ❔ expected: "0.1.0-PullRequest2.4"
            fixture.AssertFullSemver("0.0.2-PullRequest2.4", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.1.0-PullRequest2.4", configuration);
        }

        fixture.Checkout("main");
        fixture.BranchTo("pull/3/merge");
        fixture.MergeNoFF("hotfix/foo");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.2-PullRequest3.4", configuration);
        }
        else
        {
            // ❔ expected: "0.0.2-PullRequest3.4"
            fixture.AssertFullSemver("0.1.0-PullRequest3.4", configuration);
        }

        fixture.Checkout("hotfix/foo");
        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-4", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.2");
        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-1", configuration);
    }

    [Test]
    public void JustATest()
    {
        var configuration = configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1", configuration);

        fixture.ApplyTag("0.1.0");

        for (int i = 0; i < 10; i++) fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.10-1", configuration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void GitFlowFeatureDevelopmentWithReleaseNextBranch(bool useTrunkBased)
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

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.1-1", configuration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void GitFlowFeatureDevelopmentWithReleaseNextBranchFast(bool useTrunkBased)
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

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.1-1", configuration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void GitFlowFeatureDevelopmentWithRelease100Branch(bool useTrunkBased)
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

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-1", configuration);
    }
}
