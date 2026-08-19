using GitVersion.Configuration;
using GitVersion.Testing.Extensions;
using GitVersion.VersionCalculation;

namespace GitVersion.Tests.IntegrationTests;

[TestFixture]
public class IgnoreReferenceScenarios : TestBase
{
    [Test]
    public void GivenIgnoredReleaseBranch_WhenTrackingReleaseBranches_ExcludesItsVersionSource()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.BranchTo("release/0.10.0");
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.Checkout(MainBranch);
        fixture.MakeACommit();

        var trackingConfiguration = CreateTrackingReleaseConfiguration();
        var ignoredConfiguration = CreateTrackingReleaseConfiguration(new IgnoreConfiguration { Branches = ["^RELEASE/0\\.10\\.0$"] });
        var noTrackingConfiguration = CreateTrackingReleaseConfiguration(tracksReleaseBranches: false);

        fixture.AssertFullSemver("0.10.1-pre.1+1", trackingConfiguration);
        fixture.AssertFullSemver("0.0.1-pre.1+2", ignoredConfiguration);
        fixture.AssertFullSemver("0.0.1-pre.1+2", noTrackingConfiguration);
    }

    [Test]
    public void GivenCurrentBranchMatchesIgnorePattern_WhenCalculatingVersion_PreservesCurrentTarget()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.MakeACommit();
        var ignored = GitFlowConfigurationBuilder.New
            .WithIgnoreConfiguration(new IgnoreConfiguration { Branches = ["^main$"] })
            .Build();

        fixture.AssertFullSemver("1.0.1-1", ignored);
    }

    [Test]
    public void GivenIgnoredParentBranch_WhenInheritingConfiguration_PreservesParentConfiguration()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("develop");
        fixture.Repository.MakeACommit();
        fixture.BranchTo("feature/work");
        fixture.Repository.MakeACommit();
        var configuration = GitFlowConfigurationBuilder.New
            .WithIgnoreConfiguration(new IgnoreConfiguration { Branches = ["^develop$"] })
            .Build();

        fixture.AssertFullSemver("1.1.0-work.1+2", configuration);
    }

    [Test]
    public void GivenIgnoredSourceBranchSharesTaggedHistory_WhenCalculatingVersion_KeepsSharedCommitEligible()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("feature/work");
        fixture.Repository.MakeACommit();
        var configuration = GitFlowConfigurationBuilder.New
            .WithIgnoreConfiguration(new IgnoreConfiguration { Branches = ["^main$"] })
            .Build();

        fixture.AssertFullSemver("1.0.1-work.1+1", configuration);
    }

    [Test]
    public void GivenIgnoredTagAndEarlierEligibleTag_WhenCalculatingVersion_UsesEarlierTagAndCountsTargetCommit()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.MakeATaggedCommit("2.0.0");
        fixture.Repository.MakeACommit();
        var configuration = GitFlowConfigurationBuilder.New
            .WithIgnoreConfiguration(new IgnoreConfiguration { Tags = ["^2\\.0\\.0$"] })
            .Build();

        fixture.AssertFullSemver("1.0.1-2", configuration);
    }

    [Test]
    public void GivenNonMatchingTagPattern_WhenCalculatingVersion_PreservesCurrentVersion()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.MakeACommit();
        var configuration = GitFlowConfigurationBuilder.New
            .WithIgnoreConfiguration(new IgnoreConfiguration { Tags = ["^preview-"] })
            .Build();

        fixture.AssertFullSemver("1.0.1-1", configuration);
    }

    private static IGitVersionConfiguration CreateTrackingReleaseConfiguration(
        IgnoreConfiguration? ignore = null,
        bool tracksReleaseBranches = true)
    {
        var builder = GitFlowConfigurationBuilder.New
            .WithDeploymentMode(DeploymentMode.ManualDeployment)
            .WithBranch("unknown", branch => branch.WithIncrement(IncrementStrategy.Patch).WithTracksReleaseBranches(true))
            .WithBranch(MainBranch, branch => branch.WithLabel("pre").WithTracksReleaseBranches(tracksReleaseBranches))
            .WithBranch("release", branch => branch.WithLabel("rc"));

        if (ignore is not null)
        {
            builder.WithIgnoreConfiguration(ignore);
        }

        return builder.Build();
    }
}
