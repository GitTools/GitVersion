using GitVersion.Core.Tests.Helpers;
using GitVersion.Logging;
using GitVersion.OutputVariables;
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

        this.logMessages = new List<string>();

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
        var semVer = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            PreReleaseTag = "unstable.4",
            BuildMetaData = new SemanticVersionBuildMetaData("5.Branch.develop")
            {
                VersionSourceSha = "versionSourceSha",
                Sha = "commitSha",
                ShortSha = "commitShortSha",
                CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z")
            }
        };

        var configuration = new TestEffectiveConfiguration();

        var vars = this.variableProvider.GetVariablesFor(semVer, configuration, null);

        vars.ToJsonString().ShouldMatchApproved(c => c.SubFolder("Approved"));
    }

    [Test]
    public void ProvidesVariablesInContinuousDeploymentModeForPreRelease()
    {
        var semVer = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            PreReleaseTag = "unstable.4",
            BuildMetaData = new SemanticVersionBuildMetaData("5.Branch.develop")
            {
                VersionSourceSha = "versionSourceSha",
                Sha = "commitSha",
                ShortSha = "commitShortSha",
                CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z")
            }
        };

        var configuration = new TestEffectiveConfiguration(versioningMode: VersioningMode.ContinuousDeployment);

        var vars = this.variableProvider.GetVariablesFor(semVer, configuration, null);

        vars.ToJsonString().ShouldMatchApproved(c => c.SubFolder("Approved"));
    }

    [Test]
    public void ProvidesVariablesInContinuousDeliveryModeForStable()
    {
        var semVer = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            BuildMetaData = new SemanticVersionBuildMetaData("5.Branch.develop")
            {
                VersionSourceSha = "versionSourceSha",
                Sha = "commitSha",
                ShortSha = "commitShortSha",
                CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z")
            }
        };

        var configuration = new TestEffectiveConfiguration();

        var vars = this.variableProvider.GetVariablesFor(semVer, configuration, null);

        vars.ToJsonString().ShouldMatchApproved(c => c.SubFolder("Approved"));
    }

    [Test]
    public void ProvidesVariablesInContinuousDeploymentModeForStable()
    {
        var semVer = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            BuildMetaData = new SemanticVersionBuildMetaData("5.Branch.develop")
            {
                VersionSourceSha = "versionSourceSha",
                Sha = "commitSha",
                ShortSha = "commitShortSha",
                CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z")
            }
        };

        var configuration = new TestEffectiveConfiguration(versioningMode: VersioningMode.ContinuousDeployment, label: "ci");

        var vars = this.variableProvider.GetVariablesFor(semVer, configuration, null);

        vars.ToJsonString().ShouldMatchApproved(c => c.SubFolder("Approved"));
    }

    [Test]
    public void ProvidesVariablesInContinuousDeploymentModeForStableWhenCurrentCommitIsTagged()
    {
        var semVer = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            BuildMetaData = new SemanticVersionBuildMetaData
            {
                VersionSourceSha = "versionSourceSha",
                CommitsSinceTag = 5,
                CommitsSinceVersionSource = 5,
                Sha = "commitSha",
                ShortSha = "commitShortSha",
                CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z")
            }
        };

        var configuration = new TestEffectiveConfiguration(versioningMode: VersioningMode.ContinuousDeployment);

        var variables = this.variableProvider.GetVariablesFor(semVer, configuration, SemanticVersion.Empty);

        variables.ToJsonString().ShouldMatchApproved(c => c.SubFolder("Approved"));
    }

    [Test]
    public void ProvidesVariablesInContinuousDeploymentModeWithTagNamePattern()
    {
        var semVer = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            PreReleaseTag = "PullRequest",
            BuildMetaData = new SemanticVersionBuildMetaData("5.Branch.develop")
            {
                Branch = "pull/2/merge",
                Sha = "commitSha",
                ShortSha = "commitShortSha",
                CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z")
            }
        };

        var configuration = new TestEffectiveConfiguration(versioningMode: VersioningMode.ContinuousDeployment, labelNumberPattern: @"[/-](?<number>\d+)[-/]");
        var vars = this.variableProvider.GetVariablesFor(semVer, configuration, null);

        vars.FullSemVer.ShouldBe("1.2.3-PullRequest2.5");
    }

    [Test]
    public void ProvidesVariablesInContinuousDeploymentModeWithTagSetToUseBranchName()
    {
        var semVer = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            BuildMetaData = new SemanticVersionBuildMetaData("5.Branch.develop")
            {
                Branch = "feature",
                Sha = "commitSha",
                ShortSha = "commitShortSha",
                CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z")
            }
        };

        var configuration = new TestEffectiveConfiguration(versioningMode: VersioningMode.ContinuousDeployment, label: "useBranchName");
        var vars = this.variableProvider.GetVariablesFor(semVer, configuration, null);

        vars.FullSemVer.ShouldBe("1.2.3-feature.5");
    }

    [Test]
    public void ProvidesVariablesInContinuousDeliveryModeForFeatureBranch()
    {
        var semVer = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            BuildMetaData = new SemanticVersionBuildMetaData("5.Branch.feature/123")
            {
                Branch = "feature/123",
                VersionSourceSha = "versionSourceSha",
                Sha = "commitSha",
                ShortSha = "commitShortSha",
                CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z")
            }
        };

        var configuration = new TestEffectiveConfiguration();

        var vars = this.variableProvider.GetVariablesFor(semVer, configuration, null);

        vars.ToJsonString().ShouldMatchApproved(c => c.SubFolder("Approved"));
    }

    [Test]
    public void ProvidesVariablesInContinuousDeliveryModeForFeatureBranchWithCustomAssemblyInfoFormat()
    {
        var semVer = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            BuildMetaData = new SemanticVersionBuildMetaData("5.Branch.feature/123")
            {
                Branch = "feature/123",
                VersionSourceSha = "versionSourceSha",
                Sha = "commitSha",
                ShortSha = "commitShortSha",
                CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z")
            }
        };

        var configuration = new TestEffectiveConfiguration(
            assemblyInformationalFormat: "{Major}.{Minor}.{Patch}+{CommitsSinceVersionSource}.Branch.{BranchName}.Sha.{ShortSha}"
        );

        var vars = this.variableProvider.GetVariablesFor(semVer, configuration, null);

        vars.ToJsonString().ShouldMatchApproved(c => c.SubFolder("Approved"));
    }
}
