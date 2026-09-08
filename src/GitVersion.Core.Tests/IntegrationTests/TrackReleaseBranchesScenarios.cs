using GitVersion.Configuration;
using GitVersion.VersionCalculation;
using LibGit2Sharp;

namespace GitVersion.Tests.IntegrationTests;

[TestFixture]
public class TrackReleaseBranchesScenarios : TestBase
{
    [TestCase("releases/R2301")]
    [TestCase("release/1.0.0")]
    public void DevelopBranchedFromTaggedReleaseAdvancesMinor(string releaseBranch)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.BranchTo(releaseBranch);
        fixture.MakeATaggedCommit("1.0.0-RC.1");
        fixture.BranchTo("develop");
        fixture.MakeACommit();

        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("develop", branch => branch.WithLabel("beta"))
            .WithBranch("release", branch => branch.WithLabel("rc"))
            .WithBranch("hotfix", branch => branch.WithLabel("rc"))
            .Build();

        fixture.AssertFullSemver("1.1.0-beta.1", configuration);
    }

    [TestCase("release/some_release")]
    [TestCase("release/1.3.0")]
    public void DevelopWithSiblingTaggedReleaseAdvancesMinor(string releaseBranch)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.2.1");
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.BranchTo(releaseBranch);
        fixture.MakeATaggedCommit("1.3.0-beta.1");
        fixture.Checkout("develop");
        fixture.MakeACommit();

        fixture.AssertFullSemver("1.4.0-alpha.1");
    }

    [TestCase("release/some_release")]
    [TestCase("release/1.3.0")]
    public void CurrentDevelopTipTracksNewerReleaseTag(string releaseBranch)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.2.1");
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.BranchTo(releaseBranch);
        fixture.MakeATaggedCommit("1.3.0-beta.1");
        fixture.Checkout("develop");

        fixture.AssertFullSemver("1.4.0-alpha.0");
    }

    [TestCase("release/some_release")]
    [TestCase("release/1.3.0")]
    public void TaggedReleaseMergedIntoMainRetainsDevelopVersion(string releaseBranch)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.2.1");
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.BranchTo(releaseBranch);
        fixture.MakeATaggedCommit("1.3.0-beta.1");
        fixture.Checkout("develop");
        var developCommit = fixture.MakeACommit();
        fixture.AssertFullSemver("1.4.0-alpha.1");

        fixture.Checkout(MainBranch);
        fixture.MergeNoFF(releaseBranch);
        fixture.Checkout("develop");
        fixture.AssertFullSemver("1.4.0-alpha.1", commitId: developCommit);

        fixture.Checkout(MainBranch);
        fixture.MakeACommit();
        fixture.Checkout("develop");
        fixture.AssertFullSemver("1.4.0-alpha.1", commitId: developCommit);

        fixture.Checkout(MainBranch);
        fixture.ApplyTag("1.3.0");
        fixture.Checkout("develop");
        fixture.AssertFullSemver("1.4.0-alpha.1", commitId: developCommit);
    }

    [Test]
    public void VersionedReleaseNameTakesPrecedenceOverHigherTag()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.2.1");
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.BranchTo("release/1.3.0");
        fixture.MakeATaggedCommit("9.0.0-beta.1");
        fixture.Checkout("develop");
        fixture.MakeACommit();

        // Other strategies still see the higher tag, but release tracking must not bump it to 9.1.0.
        fixture.AssertFullSemver("9.0.0-alpha.2");
    }

    [Test]
    public void HighestReleaseVersionWinsRegardlessOfTagDateOrBranchOrder()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.2.1");
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.BranchTo("release/a");
        fixture.MakeATaggedCommit("2.0.0-beta.2+build.7");
        fixture.MakeATaggedCommit("1.5.0-beta.3");
        fixture.Checkout("develop");
        fixture.BranchTo("release/z");
        fixture.MakeATaggedCommit("1.3.0-beta.1");
        fixture.Checkout("develop");
        fixture.MakeACommit();

        fixture.AssertFullSemver("2.1.0-alpha.1");
    }

    [TestCase(null)]
    [TestCase("not-a-version")]
    [TestCase("9.0.0")]
    [TestCase("9.0.0-alpha.1")]
    public void ReleaseWithoutEligiblePrereleaseTagDoesNotAdvanceMinor(string? tag)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.2.1");
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.BranchTo("release/some_release");
        fixture.MakeACommit();
        if (tag is not null)
        {
            fixture.ApplyTag(tag);
        }
        fixture.Checkout("develop");
        fixture.MakeACommit();

        // Isolate release tracking from TaggedCommit, which independently consumes stable/alpha tags.
        var configuration = GitFlowConfigurationBuilder.New
            .WithNextVersion("1.3.0")
            .WithVersionStrategies(VersionStrategies.ConfiguredNextVersion, VersionStrategies.TrackReleaseBranches)
            .Build();
        fixture.GetVersion(configuration).MajorMinorPatch.ShouldBe("1.3.0");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void TagOnAnotherBranchIsNotOwnedByRelease(bool mergeIntoRelease)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.2.1");
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.BranchTo("feature/other");
        fixture.MakeATaggedCommit("9.0.0-beta.1");
        fixture.Checkout("develop");
        fixture.BranchTo("release/some_release");
        fixture.MakeATaggedCommit("1.3.0-beta.1");
        if (mergeIntoRelease)
        {
            fixture.MergeNoFF("feature/other");
        }
        fixture.Checkout("develop");
        fixture.MakeACommit();

        // TaggedCommit can use the higher tag as an alternative, without the release-tracking increment.
        fixture.AssertFullSemver("9.0.0-alpha.2");
    }

    [Test]
    public void PrereleaseTagInheritedFromMainDoesNotIdentifyActiveRelease()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("9.0.0-beta.1");
        fixture.MakeATaggedCommit("1.2.1");
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.BranchTo("release/some_release");
        fixture.MakeACommit();
        fixture.Checkout("develop");
        fixture.MakeACommit();

        fixture.AssertFullSemver("9.0.0-alpha.2");
    }

    [TestCase("tag")]
    [TestCase("commit")]
    [TestCase("branch")]
    public void IgnoredReleaseTagCommitOrBranchDoesNotAdvanceMinor(string ignored)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.2.1");
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.BranchTo("release/some_release");
        var taggedCommit = fixture.MakeATaggedCommit("1.3.0-beta.1");
        fixture.Checkout("develop");
        fixture.MakeACommit();

        var ignore = ignored switch
        {
            "tag" => new IgnoreConfiguration { Tags = ["^1\\.3\\.0-beta\\.1$"] },
            "commit" => new IgnoreConfiguration { Shas = [taggedCommit] },
            _ => new IgnoreConfiguration { Branches = ["^release/"] }
        };
        var configuration = GitFlowConfigurationBuilder.New.WithIgnoreConfiguration(ignore).Build();

        fixture.AssertFullSemver("1.3.0-alpha.2", configuration);
    }

    [Test]
    public void TagPrefixAndIgnoreRulesPreserveEligibleTagOnSameCommit()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("v1.2.1");
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.BranchTo("release/some_release");
        fixture.MakeATaggedCommit("v1.3.0-beta.1");
        fixture.ApplyTag("v9.0.0-beta.1");
        fixture.ApplyTag("other-8.0.0-beta.1");
        fixture.Checkout("develop");
        fixture.MakeACommit();
        var configuration = GitFlowConfigurationBuilder.New
            .WithTagPrefixPattern("^v")
            .WithIgnoreConfiguration(new IgnoreConfiguration { Tags = ["^v9\\."] })
            .Build();

        fixture.AssertFullSemver("1.4.0-alpha.1", configuration);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void DisabledReleaseTrackingDoesNotUseTagFallback(bool disableStrategy)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.2.1");
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.BranchTo("release/some_release");
        fixture.MakeATaggedCommit("1.3.0-beta.1");
        fixture.Checkout("develop");
        fixture.MakeACommit();
        var builder = GitFlowConfigurationBuilder.New;
        if (disableStrategy)
        {
            builder.WithVersionStrategies(VersionStrategies.Fallback, VersionStrategies.TaggedCommit);
        }
        else
        {
            builder.WithBranch("develop", branch => branch.WithTracksReleaseBranches(false));
        }

        fixture.AssertFullSemver("1.3.0-alpha.2", builder.Build());
    }

    [TestCase("release/some_release")]
    [TestCase("release/1.3.0")]
    public void DevelopCommitRetainsReleaseVersionAfterBranchAdvances(string releaseBranch)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.2.1");
        fixture.BranchTo("develop");
        var historicalCommit = fixture.MakeACommit();
        fixture.BranchTo(releaseBranch);
        fixture.MakeATaggedCommit("1.3.0-beta.1");
        fixture.Checkout("develop");
        fixture.AssertFullSemver("1.4.0-alpha.0", commitId: historicalCommit);
        fixture.MakeACommit();

        fixture.AssertFullSemver("1.4.0-alpha.0", commitId: historicalCommit);
        fixture.AssertFullSemver("1.4.0-alpha.1");
    }

    [TestCase("release/unrelated")]
    [TestCase("release/9.0.0")]
    public void ReleaseWithUnrelatedHistoryDoesNotAdvanceMinor(string releaseBranch)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.2.1");
        var mainCommit = fixture.Repository.Head.Tip;
        var unrelatedCommit = fixture.Repository.ObjectDatabase.CreateCommit(
            mainCommit.Author, mainCommit.Committer, "Unrelated release root", mainCommit.Tree, [], false);
        fixture.Repository.CreateBranch(releaseBranch, unrelatedCommit);
        fixture.Repository.Tags.Add("9.0.0-beta.1", unrelatedCommit);
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        var configuration = GitFlowConfigurationBuilder.New
            .WithNextVersion("1.3.0")
            .WithVersionStrategies(VersionStrategies.ConfiguredNextVersion, VersionStrategies.TrackReleaseBranches)
            .Build();

        fixture.GetVersion(configuration).MajorMinorPatch.ShouldBe("1.3.0");
    }

    [Test]
    public void HistoricalDevelopUsesMergeBaseAtSelectedCommit()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.2.1");
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.BranchTo("release/some_release");
        fixture.MakeATaggedCommit("1.3.0-beta.1");
        fixture.Checkout("develop");
        var historicalCommit = fixture.MakeACommit();
        fixture.MergeNoFF("release/some_release");

        fixture.AssertFullSemver("1.4.0-alpha.1", commitId: historicalCommit);
    }

    [TestCase("hotfix/foo")]
    [TestCase("hotfix/0.0.2")]
    public void PullRequestTrackingTaggedHotfixUsesSameSourceAsVersionedName(string hotfixBranch)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("0.0.1");
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.BranchTo(hotfixBranch);
        fixture.MakeATaggedCommit("0.0.2-beta.1");
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.Checkout("develop");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF(hotfixBranch);

        fixture.AssertFullSemver("0.1.0-PullRequest2.4");
    }
}
