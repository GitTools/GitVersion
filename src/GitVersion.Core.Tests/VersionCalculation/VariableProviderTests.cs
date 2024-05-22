using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Git;
using GitVersion.Logging;
using GitVersion.VersionCalculation;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Core.Tests;

[TestFixture]
public class VariableProviderTests : TestBase
{
    private IVariableProvider variableProvider;
    private List<string> logMessages;

    [SetUp]
    public void Setup()
    {
        ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestAttribute>();

        this.logMessages = [];

        var sp = ConfigureServices(services =>
        {
            var log = new Log(new TestLogAppender(this.logMessages.Add));
            services.AddSingleton<ILog>(log);
        });

        this.variableProvider = sp.GetRequiredService<IVariableProvider>();
    }

    [Test]
    public void ProvidesVariablesInContinuousDeliveryModeForPreRelease()
    {
        var semanticVersion = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            PreReleaseTag = "unstable.4",
            BuildMetaData = new("5.Branch.develop")
            {
                VersionSourceSha = "versionSourceSha",
                Sha = "commitSha",
                ShortSha = "commitShortSha",
                CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z")
            }
        };

        var configuration = GitFlowConfigurationBuilder.New.Build();
        int preReleaseWeight = configuration.GetEffectiveConfiguration(ReferenceName.FromBranchName("unstable")).PreReleaseWeight;
        var variables = this.variableProvider.GetVariablesFor(semanticVersion, configuration, preReleaseWeight);

        variables.ToJson().ShouldMatchApproved(c => c.SubFolder("Approved"));
    }

    [Test]
    public void ProvidesVariablesInContinuousDeploymentModeForPreRelease()
    {
        var semanticVersion = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            PreReleaseTag = new("unstable", 8, true),
            BuildMetaData = new("Branch.develop")
            {
                VersionSourceSha = "versionSourceSha",
                Sha = "commitSha",
                ShortSha = "commitShortSha",
                CommitsSinceVersionSource = 5,
                CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z")
            }
        };

        var configuration = GitFlowConfigurationBuilder.New.Build();
        int preReleaseWeight = configuration.GetEffectiveConfiguration(ReferenceName.FromBranchName("unstable")).PreReleaseWeight;
        var variables = this.variableProvider.GetVariablesFor(semanticVersion, configuration, preReleaseWeight);

        variables.ToJson().ShouldMatchApproved(c => c.SubFolder("Approved"));
    }

    [Test]
    public void ProvidesVariablesInContinuousDeliveryModeForStable()
    {
        var semanticVersion = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            BuildMetaData = new("5.Branch.develop")
            {
                VersionSourceSha = "versionSourceSha",
                Sha = "commitSha",
                ShortSha = "commitShortSha",
                CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z")
            }
        };

        var configuration = GitFlowConfigurationBuilder.New.WithTagPreReleaseWeight(0).Build();
        int preReleaseWeight = configuration.GetEffectiveConfiguration(ReferenceName.FromBranchName("develop")).PreReleaseWeight;
        var variables = this.variableProvider.GetVariablesFor(semanticVersion, configuration, preReleaseWeight);

        variables.ToJson().ShouldMatchApproved(c => c.SubFolder("Approved"));
    }

    [Test]
    public void ProvidesVariablesInContinuousDeploymentModeForStable()
    {
        var semanticVersion = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            PreReleaseTag = new("ci", 5, true),
            BuildMetaData = new("Branch.develop")
            {
                VersionSourceSha = "versionSourceSha",
                Sha = "commitSha",
                ShortSha = "commitShortSha",
                CommitsSinceVersionSource = 5,
                CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z")
            }
        };

        var configuration = GitFlowConfigurationBuilder.New.Build();
        int preReleaseWeight = configuration.GetEffectiveConfiguration(ReferenceName.FromBranchName("develop")).PreReleaseWeight;
        var variables = this.variableProvider.GetVariablesFor(semanticVersion, configuration, preReleaseWeight);

        variables.ToJson().ShouldMatchApproved(c => c.SubFolder("Approved"));
    }

    [Test]
    public void ProvidesVariablesInContinuousDeploymentModeForStableWhenCurrentCommitIsTagged()
    {
        var semanticVersion = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            BuildMetaData = new()
            {
                VersionSourceSha = "versionSourceSha",
                CommitsSinceTag = 5,
                CommitsSinceVersionSource = 5,
                Sha = "commitSha",
                ShortSha = "commitShortSha",
                CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z")
            }
        };

        var configuration = GitFlowConfigurationBuilder.New.WithTagPreReleaseWeight(0).Build();
        int preReleaseWeight = configuration.GetEffectiveConfiguration(ReferenceName.FromBranchName("develop")).PreReleaseWeight;
        var variables = this.variableProvider.GetVariablesFor(semanticVersion, configuration, preReleaseWeight);

        variables.ToJson().ShouldMatchApproved(c => c.SubFolder("Approved"));
    }

    [Test]
    public void ProvidesVariablesInContinuousDeploymentModeWithTagNamePattern()
    {
        var semanticVersion = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            PreReleaseTag = new("PullRequest2", 5, true),
            BuildMetaData = new("Branch.develop")
            {
                Branch = "pull/2/merge",
                Sha = "commitSha",
                ShortSha = "commitShortSha",
                CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z")
            }
        };

        var configuration = GitFlowConfigurationBuilder.New.Build();
        int preReleaseWeight = configuration.GetEffectiveConfiguration(ReferenceName.FromBranchName("pull-request")).PreReleaseWeight;
        var variables = this.variableProvider.GetVariablesFor(semanticVersion, configuration, preReleaseWeight);

        variables.FullSemVer.ShouldBe("1.2.3-PullRequest2.5");
    }

    [Test]
    public void ProvidesVariablesInContinuousDeploymentModeWithTagSetToBranchName()
    {
        var semanticVersion = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            PreReleaseTag = new("feature", 5, true),
            BuildMetaData = new("Branch.develop")
            {
                Branch = "feature",
                Sha = "commitSha",
                ShortSha = "commitShortSha",
                CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z")
            }
        };

        var configuration = GitFlowConfigurationBuilder.New.Build();
        int preReleaseWeight = configuration.GetEffectiveConfiguration(ReferenceName.FromBranchName("develop")).PreReleaseWeight;
        var variables = this.variableProvider.GetVariablesFor(semanticVersion, configuration, preReleaseWeight);

        variables.FullSemVer.ShouldBe("1.2.3-feature.5");
    }

    [Test]
    public void ProvidesVariablesInContinuousDeliveryModeForFeatureBranch()
    {
        var semanticVersion = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            BuildMetaData = new("5.Branch.feature/123")
            {
                Branch = "feature/123",
                VersionSourceSha = "versionSourceSha",
                Sha = "commitSha",
                ShortSha = "commitShortSha",
                CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z")
            }
        };

        var configuration = GitFlowConfigurationBuilder.New.WithTagPreReleaseWeight(0).Build();
        int preReleaseWeight = configuration.GetEffectiveConfiguration(ReferenceName.FromBranchName("develop")).PreReleaseWeight;
        var variables = this.variableProvider.GetVariablesFor(semanticVersion, configuration, preReleaseWeight);

        variables.ToJson().ShouldMatchApproved(c => c.SubFolder("Approved"));
    }

    [Test]
    public void ProvidesVariablesInContinuousDeliveryModeForFeatureBranchWithCustomAssemblyInfoFormat()
    {
        var semanticVersion = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            BuildMetaData = new("5.Branch.feature/123")
            {
                Branch = "feature/123",
                VersionSourceSha = "versionSourceSha",
                Sha = "commitSha",
                ShortSha = "commitShortSha",
                CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z")
            }
        };

        var configuration = GitFlowConfigurationBuilder.New.WithTagPreReleaseWeight(0)
            .WithAssemblyInformationalFormat("{Major}.{Minor}.{Patch}+{CommitsSinceVersionSource}.Branch.{BranchName}.Sha.{ShortSha}")
            .Build();
        int preReleaseWeight = configuration.GetEffectiveConfiguration(ReferenceName.FromBranchName("develop")).PreReleaseWeight;
        var variables = this.variableProvider.GetVariablesFor(semanticVersion, configuration, preReleaseWeight);

        variables.ToJson().ShouldMatchApproved(c => c.SubFolder("Approved"));
    }

    [Test]
    public void ProvidesVariablesInContinuousDeploymentModeForMainBranchWithEmptyLabel()
    {
        var semanticVersion = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            PreReleaseTag = new(string.Empty, 9, true),
            BuildMetaData = new("Branch.main")
            {
                Branch = "main",
                VersionSourceSha = "versionSourceSha",
                Sha = "commitSha",
                ShortSha = "commitShortSha",
                CommitsSinceVersionSource = 5,
                CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z")
            }
        };

        var configuration = GitFlowConfigurationBuilder.New.Build();
        int preReleaseWeight = configuration.GetEffectiveConfiguration(ReferenceName.FromBranchName("main")).PreReleaseWeight;
        var variables = this.variableProvider.GetVariablesFor(semanticVersion, configuration, preReleaseWeight);

        variables.ToJson().ShouldMatchApproved(x => x.SubFolder("Approved"));
    }
}
