using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.VersionCalculation;
using LibGit2Sharp;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class OtherBranchScenarios : TestBase
{
    [TestCase("", "NotAVersion", "2.0.0-1", "1.9.0", "1.9.0", "1.9.1-1")]
    [TestCase("", "1.5.0", "1.5.0", "1.9.0", "1.9.0", "1.9.1-1")]
    [TestCase("prefix", "1.5.0", "2.0.0-1", "1.9.0", "2.0.0-1", "2.0.0-2")]
    [TestCase("prefix", "1.5.0", "2.0.0-1", "prefix1.9.0", "1.9.0", "1.9.1-1")]
    [TestCase("prefix", "prefix1.5.0", "1.5.0", "1.9.0", "1.5.0", "1.5.1-1")]
    public void CanUseCommitMessagesToBumpVersion_TagsTakePriorityOnlyIfVersions(
        string tagPrefix,
        string firstTag,
        string expectedAfterFirstTag,
        string secondTag,
        string expectedAfterSecondTag,
        string expectedVersionAfterNewCommit)
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithTagPrefix(tagPrefix)
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        var repo = fixture.Repository;

        repo.MakeATaggedCommit($"{tagPrefix}1.0.0");
        repo.MakeACommit("+semver:major");
        fixture.AssertFullSemver("2.0.0-1", configuration);

        repo.ApplyTag(firstTag);
        fixture.AssertFullSemver(expectedAfterFirstTag, configuration);

        repo.ApplyTag(secondTag);
        fixture.AssertFullSemver(expectedAfterSecondTag, configuration);

        repo.MakeACommit();
        fixture.AssertFullSemver(expectedVersionAfterNewCommit, configuration);
    }

    [TestCase("", null, "1.9.0-1", "2.0.0-1+1", ExpectedResult = "1.9.0-1")]
    [TestCase("", "", "1.9.0-1", "2.0.0-1+1", ExpectedResult = "1.9.0-1")]
    [TestCase("", "foo", "1.9.0-1", "2.0.0-foo.1+1", ExpectedResult = "2.0.0-foo.1+1")]
    [TestCase("", "bar", "1.9.0-1", "2.0.0-bar.1+1", ExpectedResult = "2.0.0-bar.1+1")]
    [TestCase("prefix", null, "1.9.0-1", "2.0.0-1+1", ExpectedResult = "2.0.0-1+1")]
    [TestCase("prefix", "", "1.9.0-1", "2.0.0-1+1", ExpectedResult = "2.0.0-1+1")]
    [TestCase("prefix", "foo", "1.9.0-1", "2.0.0-foo.1+1", ExpectedResult = "2.0.0-foo.1+1")]
    [TestCase("prefix", "bar", "1.9.0-1", "2.0.0-bar.1+1", ExpectedResult = "2.0.0-bar.1+1")]

    [TestCase("", null, "2.1.0-1", "2.0.0-1+1", ExpectedResult = "2.1.0-1")]
    [TestCase("", "", "2.1.0-1", "2.0.0-1+1", ExpectedResult = "2.1.0-1")]
    [TestCase("", "foo", "2.1.0-1", "2.0.0-foo.1+1", ExpectedResult = "2.1.0-foo.1+1")]
    [TestCase("", "bar", "2.1.0-1", "2.0.0-bar.1+1", ExpectedResult = "2.1.0-bar.1+1")]
    [TestCase("prefix", null, "2.1.0-1", "2.0.0-1+1", ExpectedResult = "2.0.0-1+1")]
    [TestCase("prefix", "", "2.1.0-1", "2.0.0-1+1", ExpectedResult = "2.0.0-1+1")]
    [TestCase("prefix", "foo", "2.1.0-1", "2.0.0-foo.1+1", ExpectedResult = "2.0.0-foo.1+1")]
    [TestCase("prefix", "bar", "2.1.0-1", "2.0.0-bar.1+1", ExpectedResult = "2.0.0-bar.1+1")]
    public string WhenTaggingACommitAsPreRelease(string tagPrefix, string? label, string tag, string expectedVersion)
    {
        var configuration = GitFlowConfigurationBuilder.New.WithLabel(null).WithTagPrefix(tagPrefix)
            .WithBranch("main", _ => _.WithLabel(label).WithDeploymentMode(DeploymentMode.ManualDeployment))
            .Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeATaggedCommit($"{tagPrefix}1.0.0");
        fixture.MakeACommit("+semver:major");
        fixture.AssertFullSemver(expectedVersion, configuration);
        fixture.ApplyTag(tag);

        return fixture!.GetVersion(configuration).FullSemVer;
    }

    /// <summary>
    /// https://github.com/GitTools/GitVersion/issues/2340
    /// </summary>
    [Test]
    public void ShouldOnlyConsiderTagsMatchingOfCurrentBranch()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("develop", builder => builder.WithLabel("snapshot"))
            .WithBranch("release", builder => builder.WithLabel("rc"))
            .Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit();
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.MakeATaggedCommit("0.1.2-snapshot.2");
        fixture.BranchTo("release/0.1.2");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.2-rc.1+3", configuration);
    }

    [Test]
    public void CanTakeVersionFromReleaseBranch()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("release", _ => _.WithLabel("{BranchName}").WithRegularExpression("(?<BranchName>.+)"))
            .Build();

        using var fixture = new EmptyRepositoryFixture();

        const string taggedVersion = "1.0.3";
        fixture.MakeATaggedCommit(taggedVersion);
        fixture.Repository.MakeCommits(5);
        fixture.BranchTo("release/2.0.0-LTS");
        fixture.MakeACommit();

        fixture.AssertFullSemver("2.0.0-LTS.1+6", configuration);
    }

    [Test]
    public void CanTakeVersionFromHotfixBranch()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("hotfix", _ => _.WithLabel("{BranchName}").WithRegularExpression("(?<BranchName>.+)"))
            .Build();

        using var fixture = new EmptyRepositoryFixture();

        const string taggedVersion = "1.0.3";
        fixture.MakeATaggedCommit(taggedVersion);
        fixture.BranchTo("hotfix/1.0.5-LTS");
        fixture.MakeACommit();

        fixture.AssertFullSemver("1.0.5-LTS.1+1", configuration);
    }

    [Test]
    public void BranchesWithIllegalCharsShouldNotBeUsedInVersionNames()
    {
        using var fixture = new EmptyRepositoryFixture();
        const string taggedVersion = "1.0.3";
        fixture.Repository.MakeATaggedCommit(taggedVersion);
        fixture.Repository.MakeCommits(5);
        fixture.Repository.CreateBranch("issue/m/github-569");
        Commands.Checkout(fixture.Repository, "issue/m/github-569");

        fixture.AssertFullSemver("1.0.4-issue-m-github-569.1+5");
    }

    [Test]
    public void ShouldNotGetVersionFromFeatureBranchIfNotMerged()
    {
        // * 1c08923 54 minutes ago  (HEAD -> develop)
        // | * 03dd6d5 56 minutes ago  (tag: 1.0.1-feature.1, feature)
        // |/
        // * e2ff13b 58 minutes ago  (tag: 1.0.0-unstable.0, main)

        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("develop", builder => builder.WithTrackMergeTarget(false))
            .Build();

        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeATaggedCommit("1.0.0-unstable.0"); // initial commit in main

        fixture.BranchTo("feature");
        fixture.MakeATaggedCommit("1.0.1-feature.1");
        fixture.Checkout(MainBranch);
        fixture.BranchTo("develop");
        fixture.MakeACommit();

        fixture.AssertFullSemver("1.0.0-alpha.2", configuration);

        fixture.Repository.DumpGraph();
    }

    [TestCase("alpha", "JIRA-123", "alpha")]
    [TestCase($"alpha.{ConfigurationConstants.BranchNamePlaceholder}", "JIRA-123", "alpha.JIRA-123")]
    public void LabelIsBranchNameForBranchesWithoutPrefixedBranchName(string label, string branchName, string preReleaseTagName)
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("other", builder => builder
                .WithIncrement(IncrementStrategy.Patch)
                .WithRegularExpression("(?<BranchName>.+)")
                .WithSourceBranches()
                .WithLabel(label))
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.CreateBranch(branchName);
        Commands.Checkout(fixture.Repository, branchName);
        fixture.Repository.MakeCommits(5);

        var expectedFullSemVer = $"1.0.1-{preReleaseTagName}.5";
        fixture.AssertFullSemver(expectedFullSemVer, configuration);
    }
}
