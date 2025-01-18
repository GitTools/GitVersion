using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Core.Tests.IntegrationTests;
using GitVersion.VersionCalculation;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Core.Tests.VersionCalculation;

public class NextVersionCalculatorTests : TestBase
{
    [Test]
    public void ShouldIncrementVersionBasedOnConfig()
    {
        using var contextBuilder = new GitVersionContextBuilder();

        contextBuilder.Build();

        contextBuilder.ServicesProvider.ShouldNotBeNull();
        var nextVersionCalculator = contextBuilder.ServicesProvider.GetRequiredService<INextVersionCalculator>();
        nextVersionCalculator.ShouldNotBeNull();

        var nextVersion = nextVersionCalculator.FindVersion();

        nextVersion.ToString().ShouldBe("0.0.1-0");
    }

    [Test]
    public void DoesNotIncrementWhenBaseVersionSaysNotTo()
    {
        using var contextBuilder = new GitVersionContextBuilder();

        var overrideConfiguration = new Dictionary<object, object?>
        {
            { "next-version", "1.0.0" }
        };
        contextBuilder.WithOverrideConfiguration(overrideConfiguration).Build();

        contextBuilder.ServicesProvider.ShouldNotBeNull();
        var nextVersionCalculator = contextBuilder.ServicesProvider.GetRequiredService<INextVersionCalculator>();

        nextVersionCalculator.ShouldNotBeNull();

        var nextVersion = nextVersionCalculator.FindVersion();

        nextVersion.ToString().ShouldBe("1.0.0-0");
    }

    [Test]
    public void AppliesBranchPreReleaseTag()
    {
        using var contextBuilder = new GitVersionContextBuilder();

        contextBuilder.WithDevelopBranch().Build();

        contextBuilder.ServicesProvider.ShouldNotBeNull();
        var nextVersionCalculator = contextBuilder.ServicesProvider.GetRequiredService<INextVersionCalculator>();
        nextVersionCalculator.ShouldNotBeNull();

        var nextVersion = nextVersionCalculator.FindVersion();

        nextVersion.ToString("f").ShouldBe("0.1.0-alpha.0");
    }

    [Test]
    public void PreReleaseLabelCanUseBranchName()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithNextVersion("1.0.0")
            .WithBranch("custom", builder => builder
                .WithRegularExpression(@"^custom?[\/-](?<BranchName>.+)")
                .WithLabel(ConfigurationConstants.BranchNamePlaceholder)
                .WithSourceBranches()
            )
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.BranchTo("custom/foo");
        fixture.MakeACommit();

        fixture.AssertFullSemver("1.0.0-foo.3", configuration);
    }

    [Test]
    public void PreReleaseVersionMainline()
    {
        var configuration = TrunkBasedConfigurationBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.BranchTo("feature/foo");
        fixture.MakeACommit();

        fixture.AssertFullSemver("0.1.0-foo.1", configuration);

        fixture.BranchTo("bar");
        fixture.MakeACommit();

        fixture.AssertFullSemver("0.0.3-bar.2", configuration);
    }

    [Test]
    public void MergeIntoMainline()
    {
        var configuration = TrunkBasedConfigurationBuilder.New
            .WithNextVersion("1.0.0")
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.BranchTo("foo");
        fixture.MakeACommit();
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("foo");

        fixture.AssertFullSemver("1.0.0", configuration);
    }

    [Test]
    public void MergeFeatureIntoMainline()
    {
        var configuration = TrunkBasedConfigurationBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.ApplyTag("1.0.0");
        fixture.AssertFullSemver("1.0.0", configuration);

        fixture.BranchTo("feature/foo");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.1.0-foo.1", configuration);
        fixture.ApplyTag("1.1.0-foo.1");

        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("feature/foo");
        fixture.AssertFullSemver("1.1.0", configuration);
    }

    [Test]
    public void MergeHotfixIntoMainline()
    {
        var configuration = TrunkBasedConfigurationBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.ApplyTag("1.0.0");
        fixture.AssertFullSemver("1.0.0", configuration);

        fixture.BranchTo("hotfix/foo");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.1-foo.1", configuration);
        fixture.ApplyTag("1.0.1-foo.1");

        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("hotfix/foo");
        fixture.AssertFullSemver("1.0.1", configuration);
    }

    [Test]
    public void MergeFeatureIntoMainlineWithMinorIncrement()
    {
        var configuration = TrunkBasedConfigurationBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.ApplyTag("1.0.0");
        fixture.AssertFullSemver("1.0.0", configuration);

        fixture.BranchTo("feature/foo");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.1.0-foo.1", configuration);
        fixture.ApplyTag("1.1.0-foo.1");

        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("feature/foo");
        fixture.AssertFullSemver("1.1.0", configuration);
    }

    [Test]
    public void MergeFeatureIntoMainlineWithMinorIncrementAndThenMergeHotfix()
    {
        var configuration = TrunkBasedConfigurationBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.ApplyTag("1.0.0");
        fixture.AssertFullSemver("1.0.0", configuration);

        fixture.BranchTo("feature/foo");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.1.0-foo.1", configuration);
        fixture.ApplyTag("1.1.0-foo.1");

        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("feature/foo");
        fixture.AssertFullSemver("1.1.0", configuration);
        fixture.ApplyTag("1.1.0");

        fixture.BranchTo("hotfix/bar");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.1.1-bar.1", configuration);
        fixture.ApplyTag("1.1.1-bar.1");

        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("hotfix/bar");
        fixture.AssertFullSemver("1.1.1", configuration);
    }

    [Test]
    public void PreReleaseLabelCanUseBranchNameVariable()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithNextVersion("1.0.0")
            .WithBranch("custom", builder => builder
                .WithRegularExpression(@"^custom?[\/-](?<BranchName>.+)")
                .WithLabel($"alpha.{ConfigurationConstants.BranchNamePlaceholder}")
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithSourceBranches()
            )
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.BranchTo("custom/foo");
        fixture.MakeACommit();

        fixture.AssertFullSemver("1.0.0-alpha.foo.1+3", configuration);
    }

    [Test]
    public void PreReleaseNumberShouldBeScopeToPreReleaseLabelInManualDeployment()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("main", builder => builder
                .WithLabel("beta").WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).WithBranch("feature", builder => builder
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            )
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();

        fixture.BranchTo("feature/test");
        fixture.MakeATaggedCommit("0.1.0-test.1");
        fixture.MakeACommit();

        fixture.AssertFullSemver("0.1.0-test.2+1", configuration);

        fixture.Checkout("main");
        fixture.Repository.Merge("feature/test", Generate.SignatureNow());

        fixture.AssertFullSemver("0.1.0-beta.1+3", configuration);
    }

    [Test]
    public void GetNextVersionOnNonMainlineBranchWithoutCommitsShouldWorkNormally()
    {
        var configuration = TrunkBasedConfigurationBuilder.New
            .WithNextVersion("1.0.0")
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit("initial commit");
        fixture.BranchTo("feature/f1");
        fixture.AssertFullSemver("1.0.0-f1.0", configuration);
    }
}
