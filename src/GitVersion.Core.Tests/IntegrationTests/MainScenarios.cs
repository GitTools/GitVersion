using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.VersionCalculation;
using LibGit2Sharp;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class MainScenarios : TestBase
{
    [Test]
    public void CanHandleManualDeployment()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch(MainBranch, builder => builder.WithDeploymentMode(DeploymentMode.ManualDeployment))
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.MakeCommits(2);
        fixture.AssertFullSemver("1.0.1-1+2", configuration);
    }

    [Test]
    public void CanHandleContinuousDelivery()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("main", builder => builder
                .WithLabel("ci").WithDeploymentMode(DeploymentMode.ContinuousDelivery))
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.MakeCommits(2);
        fixture.AssertFullSemver("1.0.1-ci.2", configuration);
    }

    [Test]
    public void CanHandleContinuousDeployment()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("main", builder => builder
                .WithLabel("ci").WithDeploymentMode(DeploymentMode.ContinuousDeployment))
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.MakeCommits(2);
        fixture.AssertFullSemver("1.0.1", configuration);
    }

    [Test]
    public void GivenARepositoryWithCommitsButNoTagsVersionShouldBe01()
    {
        using var fixture = new EmptyRepositoryFixture();
        // Given
        fixture.Repository.MakeACommit();
        fixture.Repository.MakeACommit();
        fixture.Repository.MakeACommit();

        // When
        fixture.AssertFullSemver("0.0.1-3");
    }

    [Test]
    public void GivenARepositoryWithCommitsButBadTagsVersionShouldBe01()
    {
        using var fixture = new EmptyRepositoryFixture();
        // Given
        fixture.Repository.MakeACommit();
        fixture.Repository.ApplyTag("BadTag");
        fixture.Repository.MakeACommit();
        fixture.Repository.MakeACommit();

        // When
        fixture.AssertFullSemver("0.0.1-3");
    }

    [Test]
    public void GivenARepositoryWithCommitsButNoTagsWithDetachedHeadVersionShouldBe01()
    {
        using var fixture = new EmptyRepositoryFixture();
        // Given
        fixture.Repository.MakeACommit("one");
        fixture.Repository.MakeACommit("two");
        fixture.Repository.MakeACommit("three");

        var commit = fixture.Repository.Head.Tip;
        fixture.Repository.MakeACommit();
        Commands.Checkout(fixture.Repository, commit);

        // When
        fixture.AssertFullSemver("0.0.1-3", onlyTrackedBranches: false);
    }

    [Test]
    public void GivenARepositoryWithTagAndNextVersionInConfigVersionShouldMatchVersionTxtFile()
    {
        const string expectedNextVersion = "1.1.0";
        var configuration = GitFlowConfigurationBuilder.New.WithNextVersion(expectedNextVersion).Build();
        using var fixture = new EmptyRepositoryFixture();
        const string taggedVersion = "1.0.3";
        fixture.Repository.MakeATaggedCommit(taggedVersion);
        fixture.Repository.MakeCommits(5);

        fixture.AssertFullSemver("1.1.0-5", configuration);
    }

    [Test]
    public void GivenARepositoryWithTagAndANextVersionTxtFileAndNoCommitsVersionShouldBeTag()
    {
        var configuration = GitFlowConfigurationBuilder.New.WithNextVersion("1.1.0").Build();

        using var fixture = new EmptyRepositoryFixture();
        const string taggedVersion = "1.0.3";
        fixture.Repository.MakeATaggedCommit(taggedVersion);
        fixture.AssertFullSemver("1.0.3");
        fixture.AssertFullSemver("1.0.3", configuration);
    }

    [Test]
    public void GivenARepositoryWithTagAndANextVersionTxtFileAndNoCommitsVersionShouldBeTag2()
    {
        using var fixture = new EmptyRepositoryFixture();
        const string taggedVersion = "1.0.3";
        fixture.Repository.MakeATaggedCommit(taggedVersion);
        fixture.Repository.MakeACommit();
        fixture.AssertFullSemver("1.0.4-1");

        // I'm not sure if the postfix +1 is correct here...
        // but the next version configuration property is something for the user to manipulate the resulting version.
        var configuration = GitFlowConfigurationBuilder.New.WithNextVersion("1.1.0").Build();
        fixture.AssertFullSemver("1.1.0-1", configuration);
    }

    [Test]
    public void GivenARepositoryWithTagAndANextVersionTxtFileAndNoCommitsVersionShouldBeTag3()
    {
        using var fixture = new EmptyRepositoryFixture();
        const string taggedVersion = "1.0.3";
        fixture.Repository.MakeATaggedCommit(taggedVersion);
        fixture.AssertFullSemver("1.0.3");
        var configuration = GitFlowConfigurationBuilder.New.WithNextVersion("1.0.2").Build();
        fixture.AssertFullSemver("1.0.3", configuration);
    }

    [Test]
    public void GivenARepositoryWithTagAndANextVersionTxtFileAndNoCommitsVersionShouldBeTag4()
    {
        using var fixture = new EmptyRepositoryFixture();
        const string taggedVersion = "1.0.3";
        fixture.Repository.MakeATaggedCommit(taggedVersion);
        fixture.Repository.MakeACommit();
        fixture.AssertFullSemver("1.0.4-1");
        var configuration = GitFlowConfigurationBuilder.New.WithNextVersion("1.0.4").Build();
        fixture.AssertFullSemver("1.0.4-1", configuration);
    }

    [Test]
    public void GivenARepositoryWithTagAndNoNextVersionTxtFileVersionShouldBeTagWithBumpedPatch()
    {
        using var fixture = new EmptyRepositoryFixture();
        const string taggedVersion = "1.0.3";
        fixture.Repository.MakeATaggedCommit(taggedVersion);
        fixture.Repository.MakeCommits(5);

        fixture.AssertFullSemver("1.0.4-5");
    }

    [Test]
    public void GivenARepositoryWithTagAndNoNextVersionTxtFileAndNoCommitsVersionShouldBeTag()
    {
        using var fixture = new EmptyRepositoryFixture();
        const string taggedVersion = "1.0.3";
        fixture.MakeATaggedCommit(taggedVersion);

        fixture.AssertFullSemver("1.0.3");
    }

    [Test]
    public void GivenARepositoryWithTagAndOldNextVersionConfigVersionShouldBeTagWithBumpedPatch()
    {
        using var fixture = new EmptyRepositoryFixture();
        const string taggedVersion = "1.1.0";
        fixture.MakeATaggedCommit(taggedVersion);
        fixture.Repository.MakeCommits(5);

        var configuration = GitFlowConfigurationBuilder.New.WithNextVersion("1.0.0").Build();
        fixture.AssertFullSemver("1.1.1-5", configuration);
    }

    [Test]
    public void GivenARepositoryWithTagAndOldNextVersionConfigAndNoCommitsVersionShouldBeTag()
    {
        using var fixture = new EmptyRepositoryFixture();
        const string taggedVersion = "1.1.0";
        fixture.Repository.MakeATaggedCommit(taggedVersion);

        var configuration = GitFlowConfigurationBuilder.New.WithNextVersion("1.0.0").Build();
        fixture.AssertFullSemver("1.1.0", configuration);
    }

    [Test]
    public void CanSpecifyTagPrefixes()
    {
        using var fixture = new EmptyRepositoryFixture();
        const string taggedVersion = "version-1.0.3";
        fixture.Repository.MakeATaggedCommit(taggedVersion);
        fixture.Repository.MakeCommits(5);

        var configuration = GitFlowConfigurationBuilder.New.WithTagPrefix("version-").Build();
        fixture.AssertFullSemver("1.0.4-5", configuration);
    }

    [Test]
    public void CanSpecifyTagPrefixesAsRegex()
    {
        var configuration = GitFlowConfigurationBuilder.New.WithTagPrefix($"version-|{RegexPatterns.Configuration.DefaultTagPrefixPattern}").Build();
        using var fixture = new EmptyRepositoryFixture();
        var taggedVersion = "v1.0.3";
        fixture.Repository.MakeATaggedCommit(taggedVersion);
        fixture.Repository.MakeCommits(5);

        fixture.AssertFullSemver("1.0.4-5", configuration);

        taggedVersion = "version-1.0.5";
        fixture.Repository.MakeATaggedCommit(taggedVersion);
        fixture.Repository.MakeCommits(5);

        fixture.AssertFullSemver("1.0.6-5", configuration);
    }

    [Test]
    public void AreTagsNotAdheringToTagPrefixIgnored()
    {
        var configuration = GitFlowConfigurationBuilder.New.WithTagPrefix("").Build();
        using var fixture = new EmptyRepositoryFixture();
        var taggedVersion = "version-1.0.3";
        fixture.Repository.MakeATaggedCommit(taggedVersion);
        fixture.Repository.MakeCommits(5);

        fixture.AssertFullSemver("0.0.1-6", configuration);

        taggedVersion = "bad/1.0.3";
        fixture.Repository.MakeATaggedCommit(taggedVersion);

        fixture.AssertFullSemver("0.0.1-7", configuration);
    }

    [Test]
    public void NextVersionShouldBeConsideredOnTheDevelopmentBranch()
    {
        using var fixture = new EmptyRepositoryFixture("develop");

        var configurationBuilder = GitFlowConfigurationBuilder.New;

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.1", configurationBuilder.Build());

        configurationBuilder.WithNextVersion("1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-alpha.1", configurationBuilder.Build());

        fixture.MakeACommit();
        configurationBuilder.WithNextVersion(null);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-alpha.2", configurationBuilder.Build());

        configurationBuilder.WithNextVersion("1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-alpha.2", configurationBuilder.Build());
    }

    /// <summary>
    /// Prevent decrementation of versions on the develop branch #3177
    /// (see https://github.com/GitTools/GitVersion/discussions/3177)
    /// </summary>
    [Test]
    public void PreventDecrementationOfVersionsOnTheDevelopmentBranch()
    {
        using var fixture = new EmptyRepositoryFixture("develop");

        var configurationBuilder = GitFlowConfigurationBuilder.New;

        configurationBuilder.WithNextVersion("1.0.0");
        fixture.MakeACommit();

        // now we are ready to start with the preparation of the 1.0.0 release
        fixture.BranchTo("release/1.0.0");
        fixture.Checkout("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.0", configurationBuilder.Build());

        fixture.Checkout("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+1", configurationBuilder.Build());

        // make another commit on release/1.0.0 to prepare the actual beta1 release
        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+2", configurationBuilder.Build());

        // now we make changes on develop that may or may not end up in the 1.0.0 release
        fixture.Checkout("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.0", configurationBuilder.Build());

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.1", configurationBuilder.Build());

        // now we do the actual release of beta 1
        fixture.Checkout("release/1.0.0");
        fixture.ApplyTag("1.0.0-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.2+0", configurationBuilder.Build());

        // continue with more work on develop that may or may not end up in the 1.0.0 release
        fixture.Checkout("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.1", configurationBuilder.Build());

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.2", configurationBuilder.Build());

        // now we decide that the new on develop should be part of the beta 2 release,
        // so we merge it into release/1.0.0 with --no-ff because it is a protected branch,
        // but we don't do the release of beta 2 just yet
        fixture.Checkout("release/1.0.0");
        fixture.MergeNoFF("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.2+3", configurationBuilder.Build());

        fixture.Checkout("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.0", configurationBuilder.Build());

        fixture.Checkout("release/1.0.0");
        fixture.ApplyTag("1.0.0-beta.2");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.3+0", configurationBuilder.Build());

        fixture.Checkout("develop");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.0", configurationBuilder.Build());

        fixture.MergeNoFF("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.3", configurationBuilder.Build());

        fixture.Repository.Branches.Remove("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.6", configurationBuilder.Build());

        fixture.Repository.Tags.Remove("1.0.0-beta.1");
        fixture.Repository.Tags.Remove("1.0.0-beta.2");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.6", configurationBuilder.Build());

        configurationBuilder.WithNextVersion("1.1.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-alpha.6", configurationBuilder.Build());
    }

    [TestCase(true, "1.1.0-4")]
    [TestCase(false, "1.0.1-4")]
    public void TrackMergeMessageShouldBeConsideredOnTheMainBranch(bool trackMergeMessage, string expectedSemanticVersion)
    {
        using var fixture = new EmptyRepositoryFixture();

        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("main", branchBuilder => branchBuilder.WithTrackMergeMessage(trackMergeMessage))
            .Build();

        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("release/1.1.0");
        fixture.MakeACommit();
        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-beta.1+2", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-1", configuration);

        fixture.MergeNoFF("release/1.1.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver(expectedSemanticVersion, configuration);

        fixture.Remove("release/1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver(expectedSemanticVersion, configuration);
    }
}
