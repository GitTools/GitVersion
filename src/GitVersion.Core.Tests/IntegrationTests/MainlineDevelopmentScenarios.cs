using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Testing.Extensions;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests.IntegrationTests;

public class MainlineDevelopmentScenarios : TestBase
{
    private static GitFlowConfigurationBuilder GetConfigurationBuilder() => GitFlowConfigurationBuilder.New
        .WithVersionStrategy(VersionStrategies.Mainline)
        .WithBranch("main", builder => builder
            .WithIsMainBranch(true).WithIncrement(IncrementStrategy.Patch)
            .WithDeploymentMode(DeploymentMode.ContinuousDeployment)
            .WithSourceBranches()
        )
        .WithBranch("develop", builder => builder
            .WithIsMainBranch(false).WithIncrement(IncrementStrategy.Minor)
            .WithDeploymentMode(DeploymentMode.ContinuousDelivery)
            .WithSourceBranches("main")
        )
        .WithBranch("feature", builder => builder
            .WithIsMainBranch(false).WithIncrement(IncrementStrategy.Minor)
            .WithDeploymentMode(DeploymentMode.ContinuousDelivery)
            .WithSourceBranches("main")
        )
        .WithBranch("hotfix", builder => builder
            .WithIsMainBranch(false).WithIncrement(IncrementStrategy.Patch)
            .WithDeploymentMode(DeploymentMode.ContinuousDelivery)
            .WithRegularExpression(@"^hotfix[\/-](?<BranchName>.+)").WithLabel("{BranchName}")
            .WithSourceBranches("main")
        )
        .WithBranch("pull-request", builder => builder
            .WithIsMainBranch(false).WithIncrement(IncrementStrategy.Inherit)
            .WithDeploymentMode(DeploymentMode.ContinuousDelivery)
            .WithSourceBranches("main")
        );

    [Test]
    public void MergedFeatureBranchesToMainImpliesRelease()
    {
        var configuration = GetConfigurationBuilder()
            .WithBranch("feature", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit("1");
        fixture.MakeATaggedCommit("1.0.0");

        fixture.BranchTo("feature/foo", "foo");
        fixture.MakeACommit("2");
        fixture.AssertFullSemver("1.0.1-foo.1", configuration);
        fixture.MakeACommit("2.1");
        fixture.AssertFullSemver("1.0.1-foo.2", configuration);
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("feature/foo");

        fixture.AssertFullSemver("1.0.1", configuration);

        fixture.BranchTo("feature/foo2", "foo2");
        fixture.MakeACommit("3 +semver: minor");
        fixture.AssertFullSemver("1.1.0-foo2.1", configuration);
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("feature/foo2");
        fixture.AssertFullSemver("1.1.0", configuration);

        fixture.BranchTo("feature/foo3", "foo3");
        fixture.MakeACommit("4");
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("feature/foo3");
        fixture.SequenceDiagram.NoteOver("Merge message contains '+semver: minor'", MainBranch);
        var commit = fixture.Repository.Head.Tip;
        // Put semver increment in merge message
        fixture.Repository.Commit(commit.Message + " +semver: minor", commit.Author, commit.Committer, new() { AmendPreviousCommit = true });
        fixture.AssertFullSemver("1.2.0", configuration);

        fixture.BranchTo("feature/foo4", "foo4");
        fixture.MakeACommit("5 +semver: major");
        fixture.AssertFullSemver("2.0.0-foo4.1", configuration);
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("feature/foo4");
        fixture.AssertFullSemver("2.0.0", configuration);

        // We should evaluate any commits not included in merge commit calculations for direct commit/push or squash to merge commits
        fixture.MakeACommit("6 +semver: major");
        fixture.AssertFullSemver("3.0.0", configuration);
        fixture.MakeACommit("7 +semver: minor");
        fixture.AssertFullSemver("3.1.0", configuration);
        fixture.MakeACommit("8");
        fixture.AssertFullSemver("3.1.1", configuration);

        // Finally verify that the merge commits still function properly
        fixture.BranchTo("feature/foo5", "foo5");
        fixture.MakeACommit("9 +semver: minor");
        fixture.AssertFullSemver("3.2.0-foo5.1", configuration);
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("feature/foo5");
        fixture.AssertFullSemver("3.2.0", configuration);

        // One more direct commit for good measure
        fixture.MakeACommit("10 +semver: minor");
        fixture.AssertFullSemver("3.3.0", configuration);
        // And we can commit without bumping semver
        fixture.MakeACommit("11 +semver: none");
        fixture.AssertFullSemver("3.3.1", configuration);
        Console.WriteLine(fixture.SequenceDiagram.GetDiagram());
    }

    [Test]
    public void VerifyPullRequestsActLikeContinuousDeliveryOnFeatureBranch()
    {
        var configuration = GetConfigurationBuilder().Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("1");

        fixture.AssertFullSemver("0.0.1", configuration);

        fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit("2");

        fixture.AssertFullSemver("1.0.1", configuration);

        fixture.BranchTo("feature/foo", "foo");
        fixture.AssertFullSemver("1.1.0-foo.0", configuration);
        fixture.MakeACommit("3");
        fixture.MakeACommit("4");
        fixture.Repository.CreatePullRequestRef("feature/foo", MainBranch, prNumber: 8, normalise: true);
        fixture.AssertFullSemver("1.1.0-PullRequest8.3", configuration);
    }

    [Test]
    public void VerifyPullRequestsActLikeContinuousDeliveryOnHotfixBranch()
    {
        var configuration = GetConfigurationBuilder().Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("1");

        fixture.AssertFullSemver("0.0.1", configuration);

        fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit("2");

        fixture.AssertFullSemver("1.0.1", configuration);

        fixture.BranchTo("hotfix/foo", "foo");
        fixture.AssertFullSemver("1.0.2-foo.0", configuration);
        fixture.MakeACommit("3");
        fixture.MakeACommit("4");
        fixture.Repository.CreatePullRequestRef("hotfix/foo", MainBranch, prNumber: 8, normalise: true);
        fixture.AssertFullSemver("1.0.2-PullRequest8.3", configuration);
    }

    [Test]
    public void VerifyForwardMerge()
    {
        var configuration = GetConfigurationBuilder()
            .WithBranch("feature", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit("1");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit(); // 1.0.1

        fixture.BranchTo("feature/foo", "foo");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.2-foo.1", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.2-foo.2", configuration);

        fixture.Checkout(MainBranch);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.2", configuration);
        fixture.Checkout("feature/foo");
        // This may seem surprising, but this happens because we branched off mainline
        // and incremented. Mainline has then moved on. We do not follow mainline
        // in feature branches, you need to merge mainline in to get the mainline version
        fixture.AssertFullSemver("1.0.2-foo.2", configuration);
        fixture.MergeNoFF(MainBranch);
        fixture.AssertFullSemver("1.0.3-foo.3", configuration);
    }

    [Test]
    public void VerifyDevelopTracksMainVersion()
    {
        var configuration = GetConfigurationBuilder().Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit("1");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit();

        // branching increments the version
        fixture.BranchTo("develop");
        fixture.AssertFullSemver("1.1.0-alpha.0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.1.0-alpha.1", configuration);

        // merging develop into main increments minor version on main
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("develop");
        fixture.AssertFullSemver("1.1.0", configuration);

        // a commit on develop before the merge still has the same version number
        fixture.Checkout("develop");
        fixture.AssertFullSemver("1.1.0-alpha.1", configuration);

        // moving on to further work on develop tracks main's version from the merge
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.1.0-alpha.2", configuration);

        // adding a commit to main increments patch
        fixture.Checkout(MainBranch);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.1.1", configuration);

        // adding a commit to main doesn't change develop 's version
        fixture.Checkout("develop");
        fixture.AssertFullSemver("1.1.0-alpha.2", configuration);
    }

    [Test]
    public void VerifyDevelopFeatureTracksMainVersion()
    {
        var configuration = GetConfigurationBuilder().WithBranch("feature", builder => builder
            .WithIncrement(IncrementStrategy.Minor)
        ).Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit("1");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit();

        // branching increments the version
        fixture.BranchTo("develop");
        fixture.AssertFullSemver("1.1.0-alpha.0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.1.0-alpha.1", configuration);

        // merging develop into main increments minor version on main
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("develop");
        fixture.AssertFullSemver("1.1.0", configuration);

        // a commit on develop before the merge still has the same version number
        fixture.Checkout("develop");
        fixture.AssertFullSemver("1.1.0-alpha.1", configuration);

        // a branch from develop before the merge tracks the pre-merge version from main
        // (note: the commit on develop looks like a commit to this branch, thus the .1)
        fixture.BranchTo("feature/foo");
        fixture.AssertFullSemver("1.1.0-foo.1", configuration);

        // further work on the branch tracks the merged version from main
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.1.0-foo.2", configuration);

        // adding a commit to main increments patch
        fixture.Checkout(MainBranch);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.1.1", configuration);

        // adding a commit to main doesn't change the feature's version
        fixture.Checkout("feature/foo");
        fixture.AssertFullSemver("1.1.0-foo.2", configuration);

        // merging the feature to develop increments develop
        fixture.Checkout("develop");
        fixture.MergeNoFF("feature/foo");
        fixture.AssertFullSemver("1.1.0-alpha.3", configuration);
    }

    [Test]
    public void VerifyMergingMainToFeatureDoesNotCauseBranchCommitsToIncrementVersion()
    {
        var configuration = GetConfigurationBuilder()
            .WithBranch("feature", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit($"first in {MainBranch}");

        fixture.BranchTo("feature/foo", "foo");
        fixture.MakeACommit("first in foo");

        fixture.Checkout(MainBranch);
        fixture.MakeACommit($"second in {MainBranch}");

        fixture.Checkout("feature/foo");
        fixture.MergeNoFF(MainBranch);
        fixture.MakeACommit("second in foo");

        fixture.Checkout(MainBranch);
        fixture.MakeATaggedCommit("1.0.0");

        fixture.MergeNoFF("feature/foo");
        fixture.AssertFullSemver("1.0.1", configuration);
    }

    [Test]
    public void VerifyMergingMainToFeatureDoesNotStopMainCommitsIncrementingVersion()
    {
        var configuration = GetConfigurationBuilder()
            .WithBranch("feature", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit($"first in {MainBranch}");

        fixture.BranchTo("feature/foo", "foo");
        fixture.MakeACommit("first in foo");

        fixture.Checkout(MainBranch);
        fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit($"third in {MainBranch}");

        fixture.Checkout("feature/foo");
        fixture.MergeNoFF(MainBranch);
        fixture.MakeACommit("second in foo");

        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("feature/foo");
        fixture.AssertFullSemver("1.0.2", configuration);
    }

    [Test]
    public void VerifyIssue1154CanForwardMergeMainToFeatureBranch()
    {
        var configuration = GetConfigurationBuilder()
            .WithBranch("feature", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.AssertFullSemver("0.0.1", configuration);
        fixture.BranchTo("feature/branch2");
        fixture.BranchTo("feature/branch1");
        fixture.MakeACommit();
        fixture.MakeACommit();

        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("feature/branch1");
        fixture.AssertFullSemver("0.0.2", configuration);

        fixture.Checkout("feature/branch2");
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.MergeNoFF(MainBranch);

        fixture.AssertFullSemver("0.0.3-branch2.4", configuration);
    }

    [Test]
    public void VerifyMergingMainIntoAFeatureBranchWorksWithMultipleBranches()
    {
        var configuration = GetConfigurationBuilder()
            .WithBranch("feature", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit($"first in {MainBranch}");

        fixture.BranchTo("feature/foo", "foo");
        fixture.MakeACommit("first in foo");

        fixture.BranchTo("feature/bar", "bar");
        fixture.MakeACommit("first in bar");

        fixture.Checkout(MainBranch);
        fixture.MakeACommit($"second in {MainBranch}");

        fixture.Checkout("feature/foo");
        fixture.MergeNoFF(MainBranch);
        fixture.MakeACommit("second in foo");

        fixture.Checkout("feature/bar");
        fixture.MergeNoFF(MainBranch);
        fixture.MakeACommit("second in bar");

        fixture.Checkout(MainBranch);
        fixture.MakeATaggedCommit("1.0.0");

        fixture.MergeNoFF("feature/foo");
        fixture.MergeNoFF("feature/bar");
        fixture.AssertFullSemver("1.0.2", configuration);
    }

    [Test]
    public void MergingFeatureBranchThatIncrementsMinorNumberIncrementsMinorVersionOfMain()
    {
        var configuration = GetConfigurationBuilder()
            .WithBranch("feature", builder => builder
                .WithDeploymentMode(DeploymentMode.ContinuousDelivery)
                .WithIncrement(IncrementStrategy.Minor)
            )
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit($"first in {MainBranch}");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.AssertFullSemver("1.0.0", configuration);

        fixture.BranchTo("feature/foo", "foo");
        fixture.MakeACommit("first in foo");
        fixture.MakeACommit("second in foo");
        fixture.AssertFullSemver("1.1.0-foo.2", configuration);

        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("feature/foo");
        fixture.AssertFullSemver("1.1.0", configuration);
    }

    [Test]
    public void VerifyIncrementConfigIsHonoured()
    {
        var minorIncrementConfig = GitFlowConfigurationBuilder.New
            .WithVersionStrategy(VersionStrategies.Mainline)
            .WithBranch("main", builder => builder
                .WithDeploymentMode(DeploymentMode.ContinuousDeployment)
                .WithIncrement(IncrementStrategy.None)
            )
            .WithBranch("feature", builder => builder
                .WithDeploymentMode(DeploymentMode.ContinuousDelivery)
                .WithIncrement(IncrementStrategy.None)
            )
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit("1");
        fixture.MakeATaggedCommit("1.0.0");

        fixture.BranchTo("feature/foo", "foo");
        fixture.MakeACommit("2 +semver: minor");
        fixture.AssertFullSemver("1.1.0-foo.1", minorIncrementConfig);
        fixture.MakeACommit("2.1");
        fixture.AssertFullSemver("1.1.0-foo.2", minorIncrementConfig);
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("feature/foo");

        fixture.AssertFullSemver("1.1.0", minorIncrementConfig);

        fixture.BranchTo("feature/foo2", "foo2");
        fixture.MakeACommit("3 +semver: patch");
        fixture.AssertFullSemver("1.1.1-foo2.1", minorIncrementConfig);
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("feature/foo2");
        fixture.AssertFullSemver("1.1.1", minorIncrementConfig);

        fixture.BranchTo("feature/foo3", "foo3");
        fixture.MakeACommit("4");
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("feature/foo3");
        fixture.SequenceDiagram.NoteOver("Merge message contains '+semver: patch'", MainBranch);
        var commit = fixture.Repository.Head.Tip;
        // Put semver increment in merge message
        fixture.Repository.Commit(commit.Message + " +semver: patch", commit.Author, commit.Committer, new() { AmendPreviousCommit = true });
        fixture.AssertFullSemver("1.1.2", minorIncrementConfig);

        var configuration = GetConfigurationBuilder().Build();
        fixture.BranchTo("feature/foo4", "foo4");
        fixture.MakeACommit("5 +semver: major");
        fixture.AssertFullSemver("2.0.0-foo4.1", minorIncrementConfig);
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("feature/foo4");
        fixture.AssertFullSemver("2.0.0", configuration);

        // We should evaluate any commits not included in merge commit calculations for direct commit/push or squash to merge commits
        fixture.MakeACommit("6 +semver: major");
        fixture.AssertFullSemver("3.0.0", minorIncrementConfig);
        fixture.MakeACommit("7 +semver: minor");
        fixture.AssertFullSemver("3.1.0", minorIncrementConfig);
        fixture.MakeACommit("8 +semver: patch");
        fixture.AssertFullSemver("3.1.1", minorIncrementConfig);

        // Finally verify that the merge commits still function properly
        fixture.BranchTo("feature/foo5", "foo5");
        fixture.MakeACommit("9 +semver: patch");
        fixture.AssertFullSemver("3.1.2-foo5.1", minorIncrementConfig);
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("feature/foo5");
        fixture.AssertFullSemver("3.1.2", minorIncrementConfig);

        // One more direct commit for good measure
        fixture.MakeACommit("10 +semver: patch");
        fixture.AssertFullSemver("3.1.3", minorIncrementConfig);
        // And we can commit without bumping semver
        fixture.MakeACommit("11 +semver: none");
        fixture.AssertFullSemver("3.1.3", minorIncrementConfig);
        Console.WriteLine(fixture.SequenceDiagram.GetDiagram());
    }

    [Test]
    public void BranchWithoutMergeBaseMainlineBranchIsFound()
    {
        var configuration = GetConfigurationBuilder()
            .WithBranch("unknown", builder => builder.WithDeploymentMode(DeploymentMode.ContinuousDelivery))
            .WithAssemblyFileVersioningScheme(AssemblyFileVersioningScheme.MajorMinorPatchTag)
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        fixture.AssertFullSemver("0.0.1", configuration);
        fixture.BranchTo("master");
        fixture.Repository.Branches.Remove(fixture.Repository.Branches["main"]);
        fixture.AssertFullSemver("0.0.1", configuration);
        fixture.Repository.MakeCommits(2);
        fixture.AssertFullSemver("0.0.3", configuration);
        fixture.BranchTo("issue-branch");
        fixture.Repository.MakeACommit();
        fixture.AssertFullSemver("0.0.4-issue-branch.1", configuration);
    }

    [Test]
    public void GivenARemoteGitRepositoryWithCommitsThenClonedLocalDevelopShouldMatchRemoteVersion()
    {
        var configuration = GetConfigurationBuilder().Build();

        using var fixture = new RemoteRepositoryFixture();
        fixture.AssertFullSemver("0.0.5", configuration); // RemoteRepositoryFixture creates 5 commits.
        fixture.BranchTo("develop");
        fixture.AssertFullSemver("0.1.0-alpha.0", configuration);
        fixture.Repository.DumpGraph();
        using var local = fixture.CloneRepository();
        fixture.AssertFullSemver("0.1.0-alpha.0", configuration, repository: local.Repository);
        local.Repository.DumpGraph();
    }

    [TestCase("feat!: Break stuff +semver: none")]
    [TestCase("feat: Add stuff +semver: none")]
    [TestCase("fix: Fix stuff +semver: none")]
    public void NoBumpMessageTakesPrecedenceOverBumpMessage(string commitMessage)
    {
        // Same configuration as found here: https://gitversion.net/docs/reference/version-increments#conventional-commit-messages
        var conventionalCommitsConfig = GetConfigurationBuilder()
            .WithMajorVersionBumpMessage(@"^(build|chore|ci|docs|feat|fix|perf|refactor|revert|style|test)(\([\w\s-]*\))?(!:|:.*\n\n((.+\n)+\n)?BREAKING CHANGE:\s.+)")
            .WithMinorVersionBumpMessage(@"^(feat)(\([\w\s-]*\))?:")
            .WithPatchVersionBumpMessage(@"^(build|chore|ci|docs|fix|perf|refactor|revert|style|test)(\([\w\s-]*\))?:")
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");

        fixture.MakeACommit(commitMessage);

        fixture.AssertFullSemver("1.0.1", conventionalCommitsConfig);
    }
}
