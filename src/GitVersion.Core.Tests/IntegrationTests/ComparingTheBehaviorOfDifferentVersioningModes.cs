using GitVersion.Configuration;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests.IntegrationTests;

/// <summary>
/// - For mode 1 the main-releases are especially not marked with dedicated tags.
/// - For mode 2, 3 and 4 the pre-releases and the main-releases are always marked with dedicated tags.
/// - Continuous deployment with is mainline false requires always a pre-release tag unless the commit is tagged
/// </summary>
[TestFixture]
internal class ComparingTheBehaviorOfDifferentVersioningModes
{
    private static readonly GitHubFlowConfigurationBuilder configurationBuilder = GitHubFlowConfigurationBuilder.New
        .WithLabel(null).WithIsMainline(null)
        .WithBranch("main", _ => _
            .WithIncrement(IncrementStrategy.Patch).WithVersioningMode(null).WithLabel(null)
        ).WithBranch("feature", _ => _
            .WithIncrement(IncrementStrategy.Inherit).WithVersioningMode(null).WithLabel("{BranchName}")
        );

    private static readonly IGitVersionConfiguration trunkBased = configurationBuilder
        .WithVersioningMode(VersioningMode.Mainline).WithIsMainline(true)
        .WithBranch("main", _ => _.WithIsMainline(true))
        .WithBranch("feature", _ => _.WithIsMainline(true))
        .Build();

    private static readonly IGitVersionConfiguration continuousDeployment = configurationBuilder
            .WithVersioningMode(VersioningMode.ContinuousDeployment).WithIsMainline(true)
            .WithBranch("main", _ => _.WithIsMainline(true))
            .WithBranch("feature", _ => _.WithIsMainline(true))
            .Build();

    private static readonly IGitVersionConfiguration continuousDelivery = configurationBuilder
            .WithVersioningMode(VersioningMode.ContinuousDeployment).WithIsMainline(false)
            .WithBranch("main", _ => _.WithIsMainline(false))
            .WithBranch("feature", _ => _.WithIsMainline(false))
            .Build();

    private static readonly IGitVersionConfiguration manualDeployment = configurationBuilder
            .WithVersioningMode(VersioningMode.ContinuousDelivery).WithIsMainline(false)
            .WithBranch("main", _ => _.WithIsMainline(false))
            .WithBranch("feature", _ => _.WithIsMainline(false))
            .Build();

    [Test]
    public void ExpectedBehavior()
    {
        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeATaggedCommit("1.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0", trunkBased);
        fixture.AssertFullSemver("1.0.0", continuousDeployment);
        fixture.AssertFullSemver("1.0.0", continuousDelivery);
        fixture.AssertFullSemver("1.0.0", manualDeployment);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1", trunkBased);
        fixture.AssertFullSemver("1.0.1", continuousDeployment);
        fixture.AssertFullSemver("1.0.1-1", continuousDelivery);
        fixture.AssertFullSemver("1.0.1-1+1", manualDeployment);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.2", trunkBased);
        fixture.AssertFullSemver("1.0.1", continuousDeployment);
        fixture.AssertFullSemver("1.0.1-2", continuousDelivery);
        fixture.AssertFullSemver("1.0.1-1+2", manualDeployment);

        fixture.ApplyTag("1.0.1-alpha.1");

        // ✅ succeeds as expected
        //fixture.AssertFullSemver("1.0.1-alpha.1", trunkBased);
        fixture.AssertFullSemver("1.0.1", continuousDeployment);
        fixture.AssertFullSemver("1.0.1-alpha.1", continuousDelivery);
        fixture.AssertFullSemver("1.0.1-alpha.1", manualDeployment);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        //fixture.AssertFullSemver("1.0.1-alpha.2", trunkBased);
        fixture.AssertFullSemver("1.0.1", continuousDeployment);
        fixture.AssertFullSemver("1.0.1-alpha.2", continuousDelivery);
        fixture.AssertFullSemver("1.0.1-alpha.2+1", manualDeployment);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        //fixture.AssertFullSemver("1.0.1-alpha.3", trunkBased);
        fixture.AssertFullSemver("1.0.1", continuousDeployment);
        fixture.AssertFullSemver("1.0.1-alpha.3", continuousDelivery);
        fixture.AssertFullSemver("1.0.1-alpha.2+2", manualDeployment);

        fixture.MakeATaggedCommit("1.0.1-beta.1");

        // ✅ succeeds as expected
        //fixture.AssertFullSemver("1.0.1-beta.1", trunkBased);
        fixture.AssertFullSemver("1.0.1", continuousDeployment);
        fixture.AssertFullSemver("1.0.1-beta.1", continuousDelivery);
        fixture.AssertFullSemver("1.0.1-beta.1", manualDeployment);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        //fixture.AssertFullSemver("1.0.1-beta.2", trunkBased);
        fixture.AssertFullSemver("1.0.1", continuousDeployment);
        fixture.AssertFullSemver("1.0.1-beta.2", continuousDelivery);
        fixture.AssertFullSemver("1.0.1-beta.2+1", manualDeployment);

        fixture.MakeATaggedCommit("1.0.1-beta.2");

        // ✅ succeeds as expected
        //fixture.AssertFullSemver("1.0.1-beta.2", trunkBased);
        fixture.AssertFullSemver("1.0.1", continuousDeployment);
        fixture.AssertFullSemver("1.0.1-beta.2", continuousDelivery);
        fixture.AssertFullSemver("1.0.1-beta.2", manualDeployment);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        //fixture.AssertFullSemver("1.0.1-beta.3", trunkBased);
        fixture.AssertFullSemver("1.0.1", continuousDeployment);
        fixture.AssertFullSemver("1.0.1-beta.3", continuousDelivery);
        fixture.AssertFullSemver("1.0.1-beta.3+1", manualDeployment);

        fixture.ApplyTag("1.0.1");

        // ✅ succeeds as expected
        //fixture.AssertFullSemver("1.0.1", trunkBased);
        fixture.AssertFullSemver("1.0.1", continuousDeployment);
        fixture.AssertFullSemver("1.0.1", continuousDelivery);
        fixture.AssertFullSemver("1.0.1", manualDeployment);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        //fixture.AssertFullSemver("1.0.2", trunkBased);
        fixture.AssertFullSemver("1.0.2", continuousDeployment);
        fixture.AssertFullSemver("1.0.2-1", continuousDelivery);
        fixture.AssertFullSemver("1.0.2-1+1", manualDeployment);

        fixture.ApplyTag("1.0.2-1");

        // ✅ succeeds as expected
        //fixture.AssertFullSemver("1.0.2-1", trunkBased);
        fixture.AssertFullSemver("1.0.2", continuousDeployment);
        fixture.AssertFullSemver("1.0.2-1", continuousDelivery);
        fixture.AssertFullSemver("1.0.2-1", manualDeployment);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        //fixture.AssertFullSemver("1.0.2-2", trunkBased);
        fixture.AssertFullSemver("1.0.2", continuousDeployment);
        fixture.AssertFullSemver("1.0.2-2", continuousDelivery);
        fixture.AssertFullSemver("1.0.2-2+1", manualDeployment);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        //fixture.AssertFullSemver("1.0.2-3", trunkBased);
        fixture.AssertFullSemver("1.0.2", continuousDeployment);
        fixture.AssertFullSemver("1.0.2-3", continuousDelivery);
        fixture.AssertFullSemver("1.0.2-2+2", manualDeployment);
    }

    [Test]
    public void MainRelease()
    {
        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeATaggedCommit("0.0.2");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2", trunkBased);
        fixture.AssertFullSemver("0.0.2", continuousDeployment);
        fixture.AssertFullSemver("0.0.2", continuousDelivery);
        fixture.AssertFullSemver("0.0.2", manualDeployment);
    }

    [Test]
    public void MergeFeatureToMain()
    {
        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeATaggedCommit("0.0.2");
        fixture.BranchTo("feature/test");
        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-test.1", trunkBased);
        //fixture.AssertFullSemver("?", continuousDeployment);
        fixture.AssertFullSemver("0.0.3-test.1", continuousDelivery);
        fixture.AssertFullSemver("0.0.3-test.1+1", manualDeployment);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        //fixture.AssertFullSemver("0.0.3-test.2", trunkBased);
        //fixture.AssertFullSemver("?", continuousDeployment);
        fixture.AssertFullSemver("0.0.3-test.2", continuousDelivery);
        fixture.AssertFullSemver("0.0.3-test.1+2", manualDeployment);

        fixture.Checkout("main");
        fixture.MergeNoFF("feature/test");
        fixture.Repository.Branches.Remove("feature/test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3", trunkBased);
        fixture.AssertFullSemver("0.0.3", continuousDeployment);
        fixture.AssertFullSemver("0.0.3-3", continuousDelivery);
        fixture.AssertFullSemver("0.0.3-1+3", manualDeployment);
    }

    [Test]
    public void MergeFeatureToMainWithPreviousCommits()
    {
        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeATaggedCommit("0.0.2");
        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3", trunkBased);
        fixture.AssertFullSemver("0.0.3", continuousDeployment);
        fixture.AssertFullSemver("0.0.3-1", continuousDelivery);
        fixture.AssertFullSemver("0.0.3-1+1", manualDeployment);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.4", trunkBased);
        fixture.AssertFullSemver("0.0.3", continuousDeployment);
        fixture.AssertFullSemver("0.0.3-2", continuousDelivery);
        fixture.AssertFullSemver("0.0.3-1+2", manualDeployment);

        fixture.BranchTo("feature/test");
        fixture.MakeACommit();

        // ✅ succeeds as expected
        //fixture.AssertFullSemver("0.0.5-test.1", trunkBased);
        //fixture.AssertFullSemver("?", continuousDeployment);
        fixture.AssertFullSemver("0.0.3-test.3", continuousDelivery);
        fixture.AssertFullSemver("0.0.3-test.1+3", manualDeployment);

        fixture.MakeACommit();

        // ✅ succeeds as expected
        //fixture.AssertFullSemver("0.0.5-test.2", trunkBased);
        //fixture.AssertFullSemver("?", continuousDeployment);
        fixture.AssertFullSemver("0.0.3-test.4", continuousDelivery);
        fixture.AssertFullSemver("0.0.3-test.1+4", manualDeployment);

        fixture.Checkout("main");
        fixture.MergeNoFF("feature/test");
        fixture.Repository.Branches.Remove("feature/test");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.5", trunkBased);
        fixture.AssertFullSemver("0.0.3", continuousDeployment);
        fixture.AssertFullSemver("0.0.3-5", continuousDelivery);
        fixture.AssertFullSemver("0.0.3-1+5", manualDeployment);
    }

    [Test]
    public void MergeFeatureToMainWithMinorMinorSemversionIncrement()
    {
        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeATaggedCommit("0.0.2");
        fixture.BranchTo("feature/test");
        fixture.MakeACommit("+semver: minor");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-test.1", trunkBased);
        //fixture.AssertFullSemver("?", continuousDeployment);
        fixture.AssertFullSemver("0.1.0-test.1", continuousDelivery);
        fixture.AssertFullSemver("0.1.0-test.1+1", manualDeployment);

        fixture.MakeACommit("+semver: minor");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-test.2", trunkBased);
        //fixture.AssertFullSemver("?", continuousDeployment);
        fixture.AssertFullSemver("0.1.0-test.2", continuousDelivery);
        fixture.AssertFullSemver("0.1.0-test.1+2", manualDeployment);

        fixture.Checkout("main");
        fixture.MergeNoFF("feature/test");
        fixture.Repository.Branches.Remove("feature/test");

        // ✅ succeeds as expected
        //fixture.AssertFullSemver("0.2.0", trunkBased);
        fixture.AssertFullSemver("0.1.0", continuousDeployment);
        fixture.AssertFullSemver("0.1.0-3", continuousDelivery);
        fixture.AssertFullSemver("0.1.0-1+3", manualDeployment);
    }

    [Test]
    public void MergeFeatureToMainWithMajorMinorSemversionIncrement()
    {
        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeATaggedCommit("0.0.2");
        fixture.BranchTo("feature/test");
        fixture.MakeACommit("+semver: major");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-test.1", trunkBased);
        //fixture.AssertFullSemver("?", continuousDeployment);
        fixture.AssertFullSemver("1.0.0-test.1", continuousDelivery);
        fixture.AssertFullSemver("1.0.0-test.1+1", manualDeployment);

        fixture.MakeACommit("+semver: minor");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-test.2", trunkBased);
        //fixture.AssertFullSemver("?", continuousDeployment);
        fixture.AssertFullSemver("1.0.0-test.2", continuousDelivery);
        fixture.AssertFullSemver("1.0.0-test.1+2", manualDeployment);

        fixture.Checkout("main");
        fixture.MergeNoFF("feature/test");
        fixture.Repository.Branches.Remove("feature/test");

        // ✅ succeeds as expected
        //fixture.AssertFullSemver("1.1.0", trunkBased);
        fixture.AssertFullSemver("1.0.0", continuousDeployment);
        fixture.AssertFullSemver("1.0.0-3", continuousDelivery);
        fixture.AssertFullSemver("1.0.0-1+3", manualDeployment);
    }

    [Test]
    public void MergeFeatureToMainWithPreviousCommitsAndMinorMinorSemversionIncrement()
    {
        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeATaggedCommit("0.0.2");
        fixture.MakeACommit("+semver: minor");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0", trunkBased);
        fixture.AssertFullSemver("0.1.0", continuousDeployment);
        fixture.AssertFullSemver("0.1.0-1", continuousDelivery);
        fixture.AssertFullSemver("0.1.0-1+1", manualDeployment);

        fixture.MakeACommit("+semver: minor");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0", trunkBased);
        fixture.AssertFullSemver("0.1.0", continuousDeployment);
        fixture.AssertFullSemver("0.1.0-2", continuousDelivery);
        fixture.AssertFullSemver("0.1.0-1+2", manualDeployment);

        fixture.BranchTo("feature/test");
        fixture.MakeACommit("+semver: minor");

        // ✅ succeeds as expected
        //fixture.AssertFullSemver("0.3.0-test.1", trunkBased);
        //fixture.AssertFullSemver("?", continuousDeployment);
        fixture.AssertFullSemver("0.1.0-test.3", continuousDelivery);
        fixture.AssertFullSemver("0.1.0-test.1+3", manualDeployment);

        fixture.MakeACommit("+semver: minor");

        // ✅ succeeds as expected
        //fixture.AssertFullSemver("0.4.0-test.2", trunkBased);
        //fixture.AssertFullSemver("?", continuousDeployment);
        fixture.AssertFullSemver("0.1.0-test.4", continuousDelivery);
        fixture.AssertFullSemver("0.1.0-test.1+4", manualDeployment);

        fixture.Checkout("main");
        fixture.MergeNoFF("feature/test");
        fixture.Repository.Branches.Remove("feature/test");

        // ✅ succeeds as expected
        //fixture.AssertFullSemver("0.4.0", trunkBased);
        fixture.AssertFullSemver("0.1.0", continuousDeployment);
        fixture.AssertFullSemver("0.1.0-5", continuousDelivery);
        fixture.AssertFullSemver("0.1.0-1+5", manualDeployment);
    }

    [Test]
    public void MergeFeatureToMainWithPreviousCommitsAndMinorMajorSemversionIncrement()
    {
        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeATaggedCommit("0.0.2");
        fixture.MakeACommit("+semver: minor");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0", trunkBased);
        fixture.AssertFullSemver("0.1.0", continuousDeployment);
        fixture.AssertFullSemver("0.1.0-1", continuousDelivery);
        fixture.AssertFullSemver("0.1.0-1+1", manualDeployment);

        fixture.MakeACommit("+semver: major");

        // ✅ succeeds as expected
        //fixture.AssertFullSemver("1.1.0", trunkBased);
        fixture.AssertFullSemver("1.0.0", continuousDeployment);
        fixture.AssertFullSemver("1.0.0-2", continuousDelivery);
        fixture.AssertFullSemver("1.0.0-1+2", manualDeployment);

        fixture.MakeACommit("+semver: minor");

        // ✅ succeeds as expected
        //fixture.AssertFullSemver("1.1.0", trunkBased);
        fixture.AssertFullSemver("1.0.0", continuousDeployment);
        fixture.AssertFullSemver("1.0.0-3", continuousDelivery);
        fixture.AssertFullSemver("1.0.0-1+3", manualDeployment);

        fixture.BranchTo("feature/test");
        fixture.MakeACommit("+semver: major");

        // ✅ succeeds as expected
        //fixture.AssertFullSemver("2.1.0-test.1", trunkBased);
        //fixture.AssertFullSemver("?", continuousDeployment);
        fixture.AssertFullSemver("1.0.0-test.4", continuousDelivery);
        fixture.AssertFullSemver("1.0.0-test.1+4", manualDeployment);

        fixture.MakeACommit("+semver: minor");

        // ✅ succeeds as expected
        //fixture.AssertFullSemver("2.2.0-test.2", trunkBased);
        //fixture.AssertFullSemver("?", continuousDeployment);
        fixture.AssertFullSemver("1.0.0-test.5", continuousDelivery);
        fixture.AssertFullSemver("1.0.0-test.1+5", manualDeployment);

        fixture.Checkout("main");
        fixture.MergeNoFF("feature/test");
        fixture.Repository.Branches.Remove("feature/test");

        // ✅ succeeds as expected
        //fixture.AssertFullSemver("2.2.0", trunkBased);
        fixture.AssertFullSemver("1.0.0", continuousDeployment);
        fixture.AssertFullSemver("1.0.0-6", continuousDelivery);
        fixture.AssertFullSemver("1.0.0-1+6", manualDeployment);
    }
}
