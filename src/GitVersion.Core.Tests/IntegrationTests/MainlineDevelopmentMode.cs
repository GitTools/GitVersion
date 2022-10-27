using GitTools.Testing;
using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using GitVersion.VersionCalculation;
using LibGit2Sharp;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.Core.Tests.IntegrationTests;

public class MainlineDevelopmentMode : TestBase
{
    private readonly GitVersionConfiguration configuration = new() { VersioningMode = VersioningMode.Mainline };

    [Test]
    public void VerifyNonMainMainlineVersionIdenticalAsMain()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit("1");

        fixture.BranchTo("feature/foo", "foo");
        fixture.MakeACommit("2 +semver: major");
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("feature/foo");

        fixture.AssertFullSemver("1.0.0", this.configuration);

        fixture.BranchTo("support/1.0", "support");

        fixture.AssertFullSemver("1.0.0", this.configuration);
    }

    [Test]
    public void MergedFeatureBranchesToMainImpliesRelease()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit("1");
        fixture.MakeATaggedCommit("1.0.0");

        fixture.BranchTo("feature/foo", "foo");
        fixture.MakeACommit("2");
        fixture.AssertFullSemver("1.0.1-foo.1", this.configuration);
        fixture.MakeACommit("2.1");
        fixture.AssertFullSemver("1.0.1-foo.2", this.configuration);
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("feature/foo");

        fixture.AssertFullSemver("1.0.1", this.configuration);

        fixture.BranchTo("feature/foo2", "foo2");
        fixture.MakeACommit("3 +semver: minor");
        fixture.AssertFullSemver("1.1.0-foo2.1", this.configuration);
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("feature/foo2");
        fixture.AssertFullSemver("1.1.0", this.configuration);

        fixture.BranchTo("feature/foo3", "foo3");
        fixture.MakeACommit("4");
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("feature/foo3");
        fixture.SequenceDiagram.NoteOver("Merge message contains '+semver: minor'", MainBranch);
        var commit = fixture.Repository.Head.Tip;
        // Put semver increment in merge message
        fixture.Repository.Commit(commit.Message + " +semver: minor", commit.Author, commit.Committer, new CommitOptions { AmendPreviousCommit = true });
        fixture.AssertFullSemver("1.2.0", this.configuration);

        fixture.BranchTo("feature/foo4", "foo4");
        fixture.MakeACommit("5 +semver: major");
        fixture.AssertFullSemver("2.0.0-foo4.1", this.configuration);
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("feature/foo4");
        fixture.AssertFullSemver("2.0.0", this.configuration);

        // We should evaluate any commits not included in merge commit calculations for direct commit/push or squash to merge commits
        fixture.MakeACommit("6 +semver: major");
        fixture.AssertFullSemver("3.0.0", this.configuration);
        fixture.MakeACommit("7 +semver: minor");
        fixture.AssertFullSemver("3.1.0", this.configuration);
        fixture.MakeACommit("8");
        fixture.AssertFullSemver("3.1.1", this.configuration);

        // Finally verify that the merge commits still function properly
        fixture.BranchTo("feature/foo5", "foo5");
        fixture.MakeACommit("9 +semver: minor");
        fixture.AssertFullSemver("3.2.0-foo5.1", this.configuration);
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("feature/foo5");
        fixture.AssertFullSemver("3.2.0", this.configuration);

        // One more direct commit for good measure
        fixture.MakeACommit("10 +semver: minor");
        fixture.AssertFullSemver("3.3.0", this.configuration);
        // And we can commit without bumping semver
        fixture.MakeACommit("11 +semver: none");
        fixture.AssertFullSemver("3.3.0", this.configuration);
        Console.WriteLine(fixture.SequenceDiagram.GetDiagram());
    }

    [Test]
    public void VerifyPullRequestsActLikeContinuousDelivery()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit("1");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.1", this.configuration);

        fixture.BranchTo("feature/foo", "foo");
        fixture.AssertFullSemver("1.0.2-foo.0", this.configuration);
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.Repository.CreatePullRequestRef("feature/foo", MainBranch, normalise: true, prNumber: 8);
        fixture.AssertFullSemver("1.0.2-PullRequest8.3", this.configuration);
    }

    [Test]
    public void SupportBranches()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit("1");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit(); // 1.0.1
        fixture.MakeACommit(); // 1.0.2
        fixture.AssertFullSemver("1.0.2", this.configuration);

        fixture.BranchTo("support/1.0", "support10");
        fixture.AssertFullSemver("1.0.2", this.configuration);

        // Move main on
        fixture.Checkout(MainBranch);
        fixture.MakeACommit("+semver: major"); // 2.0.0 (on main)
        fixture.AssertFullSemver("2.0.0", this.configuration);

        // Continue on support/1.0
        fixture.Checkout("support/1.0");
        fixture.MakeACommit(); // 1.0.3
        fixture.MakeACommit(); // 1.0.4
        fixture.AssertFullSemver("1.0.4", this.configuration);
        fixture.BranchTo("feature/foo", "foo");
        fixture.AssertFullSemver("1.0.5-foo.0", this.configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.5-foo.1", this.configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.5-foo.2", this.configuration);
        fixture.Repository.CreatePullRequestRef("feature/foo", "support/1.0", normalise: true, prNumber: 7);
        fixture.AssertFullSemver("1.0.5-PullRequest7.3", this.configuration);
    }

    [Test]
    public void VerifyForwardMerge()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit("1");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit(); // 1.0.1

        fixture.BranchTo("feature/foo", "foo");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.2-foo.1", this.configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.2-foo.2", this.configuration);

        fixture.Checkout(MainBranch);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.2", this.configuration);
        fixture.Checkout("feature/foo");
        // This may seem surprising, but this happens because we branched off mainline
        // and incremented. Mainline has then moved on. We do not follow mainline
        // in feature branches, you need to merge mainline in to get the mainline version
        fixture.AssertFullSemver("1.0.2-foo.2", this.configuration);
        fixture.MergeNoFF(MainBranch);
        fixture.AssertFullSemver("1.0.3-foo.3", this.configuration);
    }

    [Test]
    public void VerifySupportForwardMerge()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit("1");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit(); // 1.0.1

        fixture.BranchTo("support/1.0", "support10");
        fixture.MakeACommit();
        fixture.MakeACommit();

        fixture.Checkout(MainBranch);
        fixture.MakeACommit("+semver: minor");
        fixture.AssertFullSemver("1.1.0", this.configuration);
        fixture.MergeNoFF("support/1.0");
        fixture.AssertFullSemver("1.1.1", this.configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.1.2", this.configuration);
        fixture.Checkout("support/1.0");
        fixture.AssertFullSemver("1.0.3", this.configuration);

        fixture.BranchTo("feature/foo", "foo");
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.4-foo.2", this.configuration); // TODO This probably should be 1.0.5
    }

    [Test]
    public void VerifyDevelopTracksMainVersion()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit("1");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit();

        // branching increments the version
        fixture.BranchTo("develop");
        fixture.AssertFullSemver("1.1.0-alpha.0", this.configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.1.0-alpha.1", this.configuration);

        // merging develop into main increments minor version on main
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("develop");
        fixture.AssertFullSemver("1.1.0", this.configuration);

        // a commit on develop before the merge still has the same version number
        fixture.Checkout("develop");
        fixture.AssertFullSemver("1.1.0-alpha.1", this.configuration);

        // moving on to further work on develop tracks main's version from the merge
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.2.0-alpha.1", this.configuration);

        // adding a commit to main increments patch
        fixture.Checkout(MainBranch);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.1.1", this.configuration);

        // adding a commit to main doesn't change develop's version
        fixture.Checkout("develop");
        fixture.AssertFullSemver("1.2.0-alpha.1", this.configuration);
    }

    [Test]
    public void VerifyDevelopFeatureTracksMainVersion()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit("1");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit();

        // branching increments the version
        fixture.BranchTo("develop");
        fixture.AssertFullSemver("1.1.0-alpha.0", this.configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.1.0-alpha.1", this.configuration);

        // merging develop into main increments minor version on main
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("develop");
        fixture.AssertFullSemver("1.1.0", this.configuration);

        // a commit on develop before the merge still has the same version number
        fixture.Checkout("develop");
        fixture.AssertFullSemver("1.1.0-alpha.1", this.configuration);

        // a branch from develop before the merge tracks the pre-merge version from main
        // (note: the commit on develop looks like a commit to this branch, thus the .1)
        fixture.BranchTo("feature/foo");
        fixture.AssertFullSemver("1.0.2-foo.1", this.configuration);

        // further work on the branch tracks the merged version from main
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.1.1-foo.1", this.configuration);

        // adding a commit to main increments patch
        fixture.Checkout(MainBranch);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.1.1", this.configuration);

        // adding a commit to main doesn't change the feature's version
        fixture.Checkout("feature/foo");
        fixture.AssertFullSemver("1.1.1-foo.1", this.configuration);

        // merging the feature to develop increments develop
        fixture.Checkout("develop");
        fixture.MergeNoFF("feature/foo");
        fixture.AssertFullSemver("1.2.0-alpha.2", this.configuration);
    }

    [Test]
    public void VerifyMergingMainToFeatureDoesNotCauseBranchCommitsToIncrementVersion()
    {
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
        fixture.AssertFullSemver("1.0.1", this.configuration);
    }

    [Test]
    public void VerifyMergingMainToFeatureDoesNotStopMainCommitsIncrementingVersion()
    {
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
        fixture.AssertFullSemver("1.0.2", this.configuration);
    }

    [Test]
    public void VerifyIssue1154CanForwardMergeMainToFeatureBranch()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.AssertFullSemver("0.0.1", this.configuration);
        fixture.BranchTo("feature/branch2");
        fixture.BranchTo("feature/branch1");
        fixture.MakeACommit();
        fixture.MakeACommit();

        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("feature/branch1");
        fixture.AssertFullSemver("0.0.2", this.configuration);

        fixture.Checkout("feature/branch2");
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.MergeNoFF(MainBranch);

        fixture.AssertFullSemver("0.0.3-branch2.4", this.configuration);
    }

    [Test]
    public void VerifyMergingMainIntoAFeatureBranchWorksWithMultipleBranches()
    {
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
        fixture.AssertFullSemver("1.0.2", this.configuration);
    }

    [Test]
    public void MergingFeatureBranchThatIncrementsMinorNumberIncrementsMinorVersionOfMain()
    {
        var currentConfig = new GitVersionConfiguration { VersioningMode = VersioningMode.Mainline, Branches = new Dictionary<string, BranchConfiguration> { { "feature", new BranchConfiguration { VersioningMode = VersioningMode.ContinuousDeployment, Increment = IncrementStrategy.Minor } } } };

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit($"first in {MainBranch}");
        fixture.MakeATaggedCommit("1.0.0");
        fixture.AssertFullSemver("1.0.0", currentConfig);

        fixture.BranchTo("feature/foo", "foo");
        fixture.MakeACommit("first in foo");
        fixture.MakeACommit("second in foo");
        fixture.AssertFullSemver("1.1.0-foo.2", currentConfig);

        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("feature/foo");
        fixture.AssertFullSemver("1.1.0", currentConfig);
    }

    [Test]
    public void VerifyIncrementConfigIsHonoured()
    {
        var minorIncrementConfig = new GitVersionConfiguration
        {
            VersioningMode = VersioningMode.Mainline,
            Increment = IncrementStrategy.Minor,
            Branches = new Dictionary<string, BranchConfiguration>
            {
                { MainBranch, new BranchConfiguration { Increment = IncrementStrategy.Minor, Name = MainBranch, Regex = MainBranch } },
                { "feature", new BranchConfiguration { Increment = IncrementStrategy.Minor, Name = "feature", Regex = "features?[/-]" } }
            }
        };

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit("1");
        fixture.MakeATaggedCommit("1.0.0");

        fixture.BranchTo("feature/foo", "foo");
        fixture.MakeACommit("2");
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
        fixture.Repository.Commit(commit.Message + " +semver: patch", commit.Author, commit.Committer, new CommitOptions { AmendPreviousCommit = true });
        fixture.AssertFullSemver("1.1.2", minorIncrementConfig);

        fixture.BranchTo("feature/foo4", "foo4");
        fixture.MakeACommit("5 +semver: major");
        fixture.AssertFullSemver("2.0.0-foo4.1", minorIncrementConfig);
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("feature/foo4");
        fixture.AssertFullSemver("2.0.0", this.configuration);

        // We should evaluate any commits not included in merge commit calculations for direct commit/push or squash to merge commits
        fixture.MakeACommit("6 +semver: major");
        fixture.AssertFullSemver("3.0.0", minorIncrementConfig);
        fixture.MakeACommit("7");
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
        var currentConfig = new GitVersionConfiguration { VersioningMode = VersioningMode.Mainline, AssemblyFileVersioningScheme = AssemblyFileVersioningScheme.MajorMinorPatchTag };

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        fixture.AssertFullSemver("0.0.1", currentConfig);
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("master"));
        fixture.Repository.Branches.Remove(fixture.Repository.Branches["main"]);
        fixture.AssertFullSemver("0.0.1", currentConfig);
        fixture.Repository.MakeCommits(2);
        fixture.AssertFullSemver("0.0.3", currentConfig);
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("issue-branch"));
        fixture.Repository.MakeACommit();
        fixture.AssertFullSemver("0.0.4-issue-branch.1", currentConfig);
    }

    [Test]
    public void GivenARemoteGitRepositoryWithCommitsThenClonedLocalDevelopShouldMatchRemoteVersion()
    {
        using var fixture = new RemoteRepositoryFixture();
        fixture.AssertFullSemver("0.0.5", configuration); // RemoteRepositoryFixture creates 5 commits.
        fixture.BranchTo("develop");
        fixture.AssertFullSemver("0.1.0-alpha.0", configuration);
        Console.WriteLine(fixture.SequenceDiagram.GetDiagram());
        var local = fixture.CloneRepository();
        fixture.AssertFullSemver("0.1.0-alpha.0", configuration, repository: local.Repository);
        local.Repository.DumpGraph();
    }

    [Test]
    public void GivenNoMainThrowsWarning()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.MakeACommit();
        Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
        fixture.Repository.Branches.Remove(fixture.Repository.Branches["main"]);

        var exception = Assert.Throws<WarningException>(() => fixture.AssertFullSemver("1.1.0-alpha.1", configuration));
        exception.ShouldNotBeNull();
        exception.Message.ShouldMatch("No branches can be found matching the commit .* in the configured Mainline branches: main, support");
    }

    [TestCase("feat!: Break stuff +semver: none")]
    [TestCase("feat: Add stuff +semver: none")]
    [TestCase("fix: Fix stuff +semver: none")]
    public void NoBumpMessageTakesPrecedenceOverBumpMessage(string commitMessage)
    {
        // Same configuration as found here: https://gitversion.net/docs/reference/version-increments#conventional-commit-messages
        var conventionalCommitsConfig = new GitVersionConfiguration
        {
            VersioningMode = VersioningMode.Mainline,
            MajorVersionBumpMessage = "^(build|chore|ci|docs|feat|fix|perf|refactor|revert|style|test)(\\([\\w\\s-]*\\))?(!:|:.*\\n\\n((.+\\n)+\\n)?BREAKING CHANGE:\\s.+)",
            MinorVersionBumpMessage = "^(feat)(\\([\\w\\s-]*\\))?:",
            PatchVersionBumpMessage = "^(build|chore|ci|docs|fix|perf|refactor|revert|style|test)(\\([\\w\\s-]*\\))?:",
        };
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");

        fixture.MakeACommit(commitMessage);

        fixture.AssertFullSemver("1.0.0", conventionalCommitsConfig);
    }

}

internal static class CommitExtensions
{
    public static void MakeACommit(this RepositoryFixtureBase fixture, string commitMsg)
    {
        fixture.Repository.MakeACommit(commitMsg);
        var diagramBuilder = (StringBuilder?)typeof(SequenceDiagram)
            .GetField("diagramBuilder", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(fixture.SequenceDiagram);

        string? GetParticipant(string participant) =>
            (string?)typeof(SequenceDiagram).GetMethod("GetParticipant", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(fixture.SequenceDiagram,
                    new object[]
                    {
                        participant
                    });

        var participant = GetParticipant(fixture.Repository.Head.FriendlyName);
        if (participant != null)
            diagramBuilder?.AppendLineFormat("{0} -> {0}: Commit '{1}'", participant, commitMsg);
    }
}
