using GitVersion.Configuration;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests.IntegrationTests;

/// <summary>
/// - For mode 1 the main-releases are especially not marked with dedicated tags.
/// - For mode 2, 3 and 4 the pre-releases and the main-releases are always marked with dedicated tags.
/// - Continuous deployment with is mainline false requires always a pre-release tag unless the commit is tagged
/// </summary>
[TestFixture]
internal class ComparingTheBehaviorOfDifferentDeploymentModes
{
    private static GitHubFlowConfigurationBuilder GetConfigurationBuilder() => GitHubFlowConfigurationBuilder.New
        .WithLabel(null)
        .WithBranch("main", b => b
            .WithIncrement(IncrementStrategy.Patch).WithLabel(null)
        ).WithBranch("feature", b => b
            .WithIncrement(IncrementStrategy.Inherit).WithLabel("{BranchName}")
        );

    private static readonly IGitVersionConfiguration continuousDeployment = GetConfigurationBuilder()
            .WithDeploymentMode(DeploymentMode.ContinuousDeployment)
            .WithBranch("main", b => b.WithIsMainBranch(true).WithDeploymentMode(DeploymentMode.ContinuousDeployment))
            .WithBranch("feature", b => b.WithIsMainBranch(false).WithDeploymentMode(DeploymentMode.ContinuousDeployment))
            .Build();

    private static readonly IGitVersionConfiguration continuousDelivery = GetConfigurationBuilder()
            .WithDeploymentMode(DeploymentMode.ContinuousDelivery)
            .WithBranch("main", b => b.WithIsMainBranch(true).WithDeploymentMode(DeploymentMode.ContinuousDelivery))
            .WithBranch("feature", b => b.WithIsMainBranch(false).WithDeploymentMode(DeploymentMode.ContinuousDelivery))
            .Build();

    private static readonly IGitVersionConfiguration manualDeployment = GetConfigurationBuilder()
            .WithDeploymentMode(DeploymentMode.ManualDeployment)
            .WithBranch("main", b => b.WithIsMainBranch(true).WithDeploymentMode(DeploymentMode.ManualDeployment))
            .WithBranch("feature", b => b.WithIsMainBranch(false).WithDeploymentMode(DeploymentMode.ManualDeployment))
            .Build();

    [Test]
    public void ExpectedBehavior()
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeATaggedCommit("1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0", continuousDeployment);
        fixture.AssertFullSemver("1.0.0", continuousDelivery);
        fixture.AssertFullSemver("1.0.0", manualDeployment);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1", continuousDeployment);
        fixture.AssertFullSemver("1.0.1-1", continuousDelivery);
        fixture.AssertFullSemver("1.0.1-1+1", manualDeployment);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1", continuousDeployment);
        fixture.AssertFullSemver("1.0.1-2", continuousDelivery);
        fixture.AssertFullSemver("1.0.1-1+2", manualDeployment);

        fixture.ApplyTag("1.0.1-alpha.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1", continuousDeployment);
        fixture.AssertFullSemver("1.0.1-alpha.1", continuousDelivery);
        fixture.AssertFullSemver("1.0.1-alpha.1", manualDeployment);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1", continuousDeployment);
        fixture.AssertFullSemver("1.0.1-alpha.2", continuousDelivery);
        fixture.AssertFullSemver("1.0.1-alpha.2+1", manualDeployment);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1", continuousDeployment);
        fixture.AssertFullSemver("1.0.1-alpha.3", continuousDelivery);
        fixture.AssertFullSemver("1.0.1-alpha.2+2", manualDeployment);

        fixture.MakeATaggedCommit("1.0.1-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1", continuousDeployment);
        fixture.AssertFullSemver("1.0.1-beta.1", continuousDelivery);
        fixture.AssertFullSemver("1.0.1-beta.1", manualDeployment);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1", continuousDeployment);
        fixture.AssertFullSemver("1.0.1-beta.2", continuousDelivery);
        fixture.AssertFullSemver("1.0.1-beta.2+1", manualDeployment);

        fixture.MakeATaggedCommit("1.0.1-beta.2");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1", continuousDeployment);
        fixture.AssertFullSemver("1.0.1-beta.2", continuousDelivery);
        fixture.AssertFullSemver("1.0.1-beta.2", manualDeployment);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1", continuousDeployment);
        fixture.AssertFullSemver("1.0.1-beta.3", continuousDelivery);
        fixture.AssertFullSemver("1.0.1-beta.3+1", manualDeployment);

        fixture.ApplyTag("1.0.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1", continuousDeployment);
        fixture.AssertFullSemver("1.0.1", continuousDelivery);
        fixture.AssertFullSemver("1.0.1", manualDeployment);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.2", continuousDeployment);
        fixture.AssertFullSemver("1.0.2-1", continuousDelivery);
        fixture.AssertFullSemver("1.0.2-1+1", manualDeployment);

        fixture.ApplyTag("1.0.2-1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.2", continuousDeployment);
        fixture.AssertFullSemver("1.0.2-1", continuousDelivery);
        fixture.AssertFullSemver("1.0.2-1", manualDeployment);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.2", continuousDeployment);
        fixture.AssertFullSemver("1.0.2-2", continuousDelivery);
        fixture.AssertFullSemver("1.0.2-2+1", manualDeployment);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.2", continuousDeployment);
        fixture.AssertFullSemver("1.0.2-3", continuousDelivery);
        fixture.AssertFullSemver("1.0.2-2+2", manualDeployment);
    }

    [Test]
    public void MainRelease()
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeATaggedCommit("0.0.2");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2", continuousDeployment);
        fixture.AssertFullSemver("0.0.2", continuousDelivery);
        fixture.AssertFullSemver("0.0.2", manualDeployment);
    }

    [Test]
    public void MergeFeatureToMain()
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeATaggedCommit("0.0.2");
        fixture.BranchTo("feature/test");
        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3", continuousDeployment);
        fixture.AssertFullSemver("0.0.3-test.1", continuousDelivery);
        fixture.AssertFullSemver("0.0.3-test.1+1", manualDeployment);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3", continuousDeployment);
        fixture.AssertFullSemver("0.0.3-test.2", continuousDelivery);
        fixture.AssertFullSemver("0.0.3-test.1+2", manualDeployment);

        fixture.Checkout("main");
        fixture.MergeNoFF("feature/test");
        fixture.Repository.Branches.Remove("feature/test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3", continuousDeployment);
        fixture.AssertFullSemver("0.0.3-3", continuousDelivery);
        fixture.AssertFullSemver("0.0.3-1+3", manualDeployment);
    }

    [Test]
    public void MergeFeatureToMainWithPreviousCommits()
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeATaggedCommit("0.0.2");
        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3", continuousDeployment);
        fixture.AssertFullSemver("0.0.3-1", continuousDelivery);
        fixture.AssertFullSemver("0.0.3-1+1", manualDeployment);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3", continuousDeployment);
        fixture.AssertFullSemver("0.0.3-2", continuousDelivery);
        fixture.AssertFullSemver("0.0.3-1+2", manualDeployment);

        fixture.BranchTo("feature/test");
        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3", continuousDeployment);
        fixture.AssertFullSemver("0.0.3-test.3", continuousDelivery);
        fixture.AssertFullSemver("0.0.3-test.1+3", manualDeployment);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3", continuousDeployment);
        fixture.AssertFullSemver("0.0.3-test.4", continuousDelivery);
        fixture.AssertFullSemver("0.0.3-test.1+4", manualDeployment);

        fixture.Checkout("main");
        fixture.MergeNoFF("feature/test");
        fixture.Repository.Branches.Remove("feature/test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3", continuousDeployment);
        fixture.AssertFullSemver("0.0.3-5", continuousDelivery);
        fixture.AssertFullSemver("0.0.3-1+5", manualDeployment);
    }

    [Test]
    public void MergeFeatureToMainWithMinorMinorSemVersionIncrement()
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeATaggedCommit("0.0.2");
        fixture.BranchTo("feature/test");
        fixture.MakeACommit("+semver: minor");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0", continuousDeployment);
        fixture.AssertFullSemver("0.1.0-test.1", continuousDelivery);
        fixture.AssertFullSemver("0.1.0-test.1+1", manualDeployment);

        fixture.MakeACommit("+semver: minor");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0", continuousDeployment);
        fixture.AssertFullSemver("0.1.0-test.2", continuousDelivery);
        fixture.AssertFullSemver("0.1.0-test.1+2", manualDeployment);

        fixture.Checkout("main");
        fixture.MergeNoFF("feature/test");
        fixture.Repository.Branches.Remove("feature/test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0", continuousDeployment);
        fixture.AssertFullSemver("0.1.0-3", continuousDelivery);
        fixture.AssertFullSemver("0.1.0-1+3", manualDeployment);
    }

    [Test]
    public void MergeFeatureToMainWithMajorMinorSemVersionIncrement()
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeATaggedCommit("0.0.2");
        fixture.BranchTo("feature/test");
        fixture.MakeACommit("+semver: major");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0", continuousDeployment);
        fixture.AssertFullSemver("1.0.0-test.1", continuousDelivery);
        fixture.AssertFullSemver("1.0.0-test.1+1", manualDeployment);

        fixture.MakeACommit("+semver: minor");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0", continuousDeployment);
        fixture.AssertFullSemver("1.0.0-test.2", continuousDelivery);
        fixture.AssertFullSemver("1.0.0-test.1+2", manualDeployment);

        fixture.Checkout("main");
        fixture.MergeNoFF("feature/test");
        fixture.Repository.Branches.Remove("feature/test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0", continuousDeployment);
        fixture.AssertFullSemver("1.0.0-3", continuousDelivery);
        fixture.AssertFullSemver("1.0.0-1+3", manualDeployment);
    }

    [Test]
    public void MergeFeatureToMainWithPreviousCommitsAndMinorMinorSemVersionIncrement()
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeATaggedCommit("0.0.2");
        fixture.MakeACommit("+semver: minor");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0", continuousDeployment);
        fixture.AssertFullSemver("0.1.0-1", continuousDelivery);
        fixture.AssertFullSemver("0.1.0-1+1", manualDeployment);

        fixture.MakeACommit("+semver: minor");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0", continuousDeployment);
        fixture.AssertFullSemver("0.1.0-2", continuousDelivery);
        fixture.AssertFullSemver("0.1.0-1+2", manualDeployment);

        fixture.BranchTo("feature/test");
        fixture.MakeACommit("+semver: minor");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0", continuousDeployment);
        fixture.AssertFullSemver("0.1.0-test.3", continuousDelivery);
        fixture.AssertFullSemver("0.1.0-test.1+3", manualDeployment);

        fixture.MakeACommit("+semver: minor");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0", continuousDeployment);
        fixture.AssertFullSemver("0.1.0-test.4", continuousDelivery);
        fixture.AssertFullSemver("0.1.0-test.1+4", manualDeployment);

        fixture.Checkout("main");
        fixture.MergeNoFF("feature/test");
        fixture.Repository.Branches.Remove("feature/test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0", continuousDeployment);
        fixture.AssertFullSemver("0.1.0-5", continuousDelivery);
        fixture.AssertFullSemver("0.1.0-1+5", manualDeployment);
    }

    [Test]
    public void MergeFeatureToMainWithPreviousCommitsAndMinorMajorSemVersionIncrement()
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeATaggedCommit("0.0.2");
        fixture.MakeACommit("+semver: minor");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0", continuousDeployment);
        fixture.AssertFullSemver("0.1.0-1", continuousDelivery);
        fixture.AssertFullSemver("0.1.0-1+1", manualDeployment);

        fixture.MakeACommit("+semver: major");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0", continuousDeployment);
        fixture.AssertFullSemver("1.0.0-2", continuousDelivery);
        fixture.AssertFullSemver("1.0.0-1+2", manualDeployment);

        fixture.MakeACommit("+semver: minor");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0", continuousDeployment);
        fixture.AssertFullSemver("1.0.0-3", continuousDelivery);
        fixture.AssertFullSemver("1.0.0-1+3", manualDeployment);

        fixture.BranchTo("feature/test");
        fixture.MakeACommit("+semver: major");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0", continuousDeployment);
        fixture.AssertFullSemver("1.0.0-test.4", continuousDelivery);
        fixture.AssertFullSemver("1.0.0-test.1+4", manualDeployment);

        fixture.MakeACommit("+semver: minor");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0", continuousDeployment);
        fixture.AssertFullSemver("1.0.0-test.5", continuousDelivery);
        fixture.AssertFullSemver("1.0.0-test.1+5", manualDeployment);

        fixture.Checkout("main");
        fixture.MergeNoFF("feature/test");
        fixture.Repository.Branches.Remove("feature/test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0", continuousDeployment);
        fixture.AssertFullSemver("1.0.0-6", continuousDelivery);
        fixture.AssertFullSemver("1.0.0-1+6", manualDeployment);
    }
}
