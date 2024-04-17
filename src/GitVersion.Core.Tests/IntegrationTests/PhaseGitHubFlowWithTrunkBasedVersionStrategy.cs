using GitVersion.Configuration;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class PhaseGitHubFlowWithTrunkBasedVersionStrategy
{
    private static GitHubFlowConfigurationBuilder configurationBuilder => GitHubFlowConfigurationBuilder.New;

    [Test]
    public void __Just_A_Test_00__()
    {
        var builder = configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased);
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Patch)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");
        fixture.BranchTo("feature/foo");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.1+1", configuration);

        fixture.BranchTo("feature/bar");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-bar.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-bar.1+2", configuration);
    }

    [TestCase(true, "release/0.0.0")]
    public void __Just_A_Test_01__(bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Patch)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.1+3", configuration);

        fixture.ApplyTag("0.0.2-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.2+1", configuration);

        fixture.Checkout(releaseBranch);
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo(releaseBranch, removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.2-1+6", configuration);
        }
        else
        {
            // ❔ expected: "0.0.2-1+6"
            fixture.AssertFullSemver("0.0.2-1+6", configuration);
        }

        if (!useTrunkBased) fixture.ApplyTag("0.0.2");
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-1+1", configuration);
    }

    [TestCase(true)]
    public void __Just_A_Test_02__(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Inherit)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Minor)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-foo.1+0", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-foo.1+1", configuration);

        fixture.ApplyTag("2.1.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-foo.2+0", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-foo.2+1", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("2.1.0-1+4", configuration);
        }
        else
        {
            // ❔ expected: "2.1.0-1+4"
            fixture.AssertFullSemver("3.0.0-1+4", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Feature branch (Increment inherit on main and inherit on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeatureWithIncrementInheritOnMainAndInheritOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Inherit)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.1+0", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.1+1", configuration);

        fixture.ApplyTag("3.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.2+0", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.2+1", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-1+4", configuration);
    }

    /// <summary>
    /// GitHubFlow - Feature branch (Increment inherit on main and none on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeatureWithIncrementInheritOnMainAndNoneOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Inherit)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.None)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+0", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+1", configuration);

        fixture.ApplyTag("2.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.2+0", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.2+1", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("2.0.0-2+4", configuration);
        }
        else
        {
            // ❔ expected: "2.0.0-2+4"
            fixture.AssertFullSemver("3.0.0-1+4", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Feature branch (Increment inherit on main and patch on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeatureWithIncrementInheritOnMainAndPatchOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Inherit)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-foo.1+0", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-foo.1+1", configuration);

        fixture.ApplyTag("2.0.1-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-foo.2+0", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-foo.2+1", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-foo.2+2", configuration);

        fixture.MergeTo("main");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("2.0.1-1+4", configuration);
        }
        else
        {
            // ❔ expected: "2.0.1-1+4"
            fixture.AssertFullSemver("3.0.0-1+4", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Feature branch (Increment inherit on main and minor on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeatureWithIncrementInheritOnMainAndMinorOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Inherit)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Minor)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-foo.1+0", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-foo.1+1", configuration);

        fixture.ApplyTag("2.1.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-foo.2+0", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-foo.2+1", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("2.1.0-1+4", configuration);
        }
        else
        {
            // ❔ expected: "2.1.0-1+4"
            fixture.AssertFullSemver("3.0.0-1+4", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Feature branch (Increment inherit on main and major on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeatureWithIncrementInheritOnMainAndMajorOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Inherit)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Major)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.1+0", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.1+1", configuration);

        fixture.ApplyTag("3.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.2+0", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.2+1", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-1+4", configuration);
    }

    /// <summary>
    /// GitHubFlow - Feature branch (Increment none on main and inherit on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeatureWithIncrementNoneOnMainAndInheritOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.None)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.1+0", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.1+1", configuration);

        fixture.ApplyTag("0.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.2+0", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.2+1", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-2+4", configuration);
        }
        else
        {
            // ❔ expected: "0.0.0-2+4"
            fixture.AssertFullSemver("0.0.0-1+4", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Feature branch (Increment none on main and none on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeatureWithIncrementNoneOnMainAndNoneOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.None)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.None)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.1+0", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.1+1", configuration);

        fixture.ApplyTag("0.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.2+0", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.2+1", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-2+4", configuration);
        }
        else
        {
            // ❔ expected: "0.0.0-2+4"
            fixture.AssertFullSemver("0.0.0-1+4", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Feature branch (Increment none on main and patch on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeatureWithIncrementNoneOnMainAndPatchOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.None)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-foo.1+0", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-foo.1+1", configuration);

        fixture.ApplyTag("0.0.1-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-foo.2+0", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-foo.2+1", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-foo.2+2", configuration);

        fixture.MergeTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+4", configuration);
    }

    /// <summary>
    /// GitHubFlow - Feature branch (Increment none on main and minor on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeatureWithIncrementNoneOnMainAndMinorOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.None)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Minor)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+0", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+1", configuration);

        fixture.ApplyTag("0.1.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.2+0", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.2+1", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+4", configuration);
    }

    /// <summary>
    /// GitHubFlow - Feature branch (Increment none on main and major on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeatureWithIncrementNoneOnMainAndMajorOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.None)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Major)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+0", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+1", configuration);

        fixture.ApplyTag("1.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.2+0", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.2+1", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+4", configuration);
    }

    /// <summary>
    /// GitHubFlow - Feature branch (Increment patch on main and inherit on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeatureWithIncrementPatchOnMainAndInheritOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Patch)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.2");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-foo.1+0", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-foo.1+1", configuration);

        fixture.ApplyTag("0.0.3-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-foo.2+0", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-foo.2+1", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-foo.2+2", configuration);

        fixture.MergeTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-1+4", configuration);
    }

    /// <summary>
    /// GitHubFlow - Feature branch (Increment patch on main and none on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeatureWithIncrementPatchOnMainAndNoneOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Patch)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.None)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.2");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.1+0", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.1+1", configuration);

        fixture.ApplyTag("0.0.2-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.2+0", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.2+1", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.2+2", configuration);

        fixture.MergeTo("main");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.2-2+4", configuration);
        }
        else
        {
            // ❔ expected: "0.0.2-2+4"
            fixture.AssertFullSemver("0.0.3-1+4", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Feature branch (Increment patch on main and patch on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeatureWithIncrementPatchOnMainAndPatchOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Patch)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.2");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-foo.1+0", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-foo.1+1", configuration);

        fixture.ApplyTag("0.0.3-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-foo.2+0", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-foo.2+1", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-foo.2+2", configuration);

        fixture.MergeTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-1+4", configuration);
    }

    /// <summary>
    /// GitHubFlow - Feature branch (Increment patch on main and minor on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeatureWithIncrementPatchOnMainAndMinorOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Patch)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Minor)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.2");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+0", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+1", configuration);

        fixture.ApplyTag("0.1.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.2+0", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.2+1", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+4", configuration);
    }

    /// <summary>
    /// GitHubFlow - Feature branch (Increment patch on main and major on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeatureWithIncrementPatchOnMainAndMajorOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Patch)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Major)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.2");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+0", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+1", configuration);

        fixture.ApplyTag("1.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.2+0", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.2+1", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+4", configuration);
    }

    /// <summary>
    /// GitHubFlow - Feature branch (Increment minor on main and inherit on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeatureWithIncrementMinorOnMainAndInheritOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.2.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-foo.1+0", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-foo.1+1", configuration);

        fixture.ApplyTag("0.3.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-foo.2+0", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-foo.2+1", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-1+4", configuration);
    }

    /// <summary>
    /// GitHubFlow - Feature branch (Increment minor on main and none on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeatureWithIncrementMinorOnMainAndNoneOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.None)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.2.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-foo.1+0", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-foo.1+1", configuration);

        fixture.ApplyTag("0.2.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-foo.2+0", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-foo.2+1", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.2.0-2+4", configuration);
        }
        else
        {
            // ❔ expected: "0.2.0-2+4"
            fixture.AssertFullSemver("0.3.0-1+4", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Feature branch (Increment minor on main and patch on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeatureWithIncrementMinorOnMainAndPatchOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.2.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.1-foo.1+0", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.1-foo.1+1", configuration);

        fixture.ApplyTag("0.2.1-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.1-foo.2+0", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.1-foo.2+1", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.1-foo.2+2", configuration);

        fixture.MergeTo("main");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.2.1-1+4", configuration);
        }
        else
        {
            // ❔ expected: "0.2.1-1+4"
            fixture.AssertFullSemver("0.3.0-1+4", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Feature branch (Increment minor on main and minor on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeatureWithIncrementMinorOnMainAndMinorOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Minor)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.2.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-foo.1+0", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-foo.1+1", configuration);

        fixture.ApplyTag("0.3.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-foo.2+0", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-foo.2+1", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-1+4", configuration);
    }

    /// <summary>
    /// GitHubFlow - Feature branch (Increment minor on main and major on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeatureWithIncrementMinorOnMainAndMajorOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Major)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.2.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+0", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+1", configuration);

        fixture.ApplyTag("1.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.2+0", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.2+1", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+4", configuration);
    }

    /// <summary>
    /// GitHubFlow - Feature branch (Increment major on main and inherit on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeatureWithIncrementMajorOnMainAndInheritOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Major)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.1+0", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.1+1", configuration);

        fixture.ApplyTag("3.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.2+0", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.2+1", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-1+4", configuration);
    }

    /// <summary>
    /// GitHubFlow - Feature branch (Increment major on main and none on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeatureWithIncrementMajorOnMainAndNoneOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Major)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.None)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+0", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+1", configuration);

        fixture.ApplyTag("2.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.2+0", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.2+1", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("2.0.0-2+4", configuration);
        }
        else
        {
            // ❔ expected: "2.0.0-2+4"
            fixture.AssertFullSemver("3.0.0-1+4", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Feature branch (Increment major on main and patch on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeatureWithIncrementMajorOnMainAndPatchOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Major)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-foo.1+0", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-foo.1+1", configuration);

        fixture.ApplyTag("2.0.1-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-foo.2+0", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-foo.2+1", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-foo.2+2", configuration);

        fixture.MergeTo("main");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("2.0.1-1+4", configuration);
        }
        else
        {
            // ❔ expected: "2.0.1-foo.2+3"
            fixture.AssertFullSemver("3.0.0-1+4", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Feature branch (Increment major on main and minor on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeatureWithIncrementMajorOnMainAndMinorOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Major)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Minor)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-foo.1+0", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-foo.1+1", configuration);

        fixture.ApplyTag("2.1.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-foo.2+0", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-foo.2+1", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("2.1.0-1+4", configuration);
        }
        else
        {
            // ❔ expected: "2.1.0-1+4"
            fixture.AssertFullSemver("3.0.0-1+4", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Feature branch (Increment major on main and major on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeatureWithIncrementMajorOnMainAndMajorOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Major)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Major)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.1+0", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.1+1", configuration);

        fixture.ApplyTag("3.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.2+0", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.2+1", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-1+4", configuration);
    }

    /// <summary>
    /// GitHubFlow - Merge main to feature branch (Increment inherit on main and inherit on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureMergeMainToFeatureWithIncrementInheritOnMainAndInheritOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Inherit)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MergeTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.1+3", configuration);

        fixture.ApplyTag("3.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.2+1", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-1+6", configuration);
    }

    /// <summary>
    /// GitHubFlow - Merge main to feature branch (Increment inherit on main and none on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureMergeMainToFeatureWithIncrementInheritOnMainAndNoneOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Inherit)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.None)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MergeTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+3", configuration);

        fixture.ApplyTag("2.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.2+1", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("2.0.0-2+6", configuration);
        }
        else
        {
            // ❔ expected: "2.0.0-2+6"
            fixture.AssertFullSemver("3.0.0-1+6", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Merge main to feature branch (Increment inherit on main and patch on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureMergeMainToFeatureWithIncrementInheritOnMainAndPatchOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Inherit)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MergeTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-foo.1+3", configuration);

        fixture.ApplyTag("2.0.1-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-foo.2+1", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-foo.2+2", configuration);

        fixture.MergeTo("main");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("2.0.1-1+6", configuration);
        }
        else
        {
            // ❔ expected: "2.0.1-1+6"
            fixture.AssertFullSemver("3.0.0-1+6", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Merge main to feature branch (Increment inherit on main and minor on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureMergeMainToFeatureWithIncrementInheritOnMainAndMinorOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Inherit)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Minor)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MergeTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-foo.1+3", configuration);

        fixture.ApplyTag("2.1.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-foo.2+1", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("2.1.0-1+6", configuration);
        }
        else
        {
            // ❔ expected: "2.1.0-1+6"
            fixture.AssertFullSemver("3.0.0-1+6", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Merge main to feature branch (Increment inherit on main and major on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureMergeMainToFeatureWithIncrementInheritOnMainAndMajorOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Inherit)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Major)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MergeTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.1+3", configuration);

        fixture.ApplyTag("3.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.2+1", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-1+6", configuration);
    }

    /// <summary>
    /// GitHubFlow - Merge main to feature branch (Increment none on main and inherit on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureMergeMainToFeatureWithIncrementNoneOnMainAndInheritOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.None)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-2+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-1+1", configuration);
        }

        if (!useTrunkBased)
        {
            fixture.Repository.Tags.Remove("0.0.0");
            fixture.ApplyTag("0.0.0");
        }
        fixture.MergeTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.1+3", configuration);

        fixture.ApplyTag("0.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.2+1", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-3+6", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-1+6", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Merge main to feature branch (Increment none on main and none on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureMergeMainToFeatureWithIncrementNoneOnMainAndNoneOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.None)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.None)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-2+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-1+1", configuration);
        }

        if (!useTrunkBased)
        {
            fixture.Repository.Tags.Remove("0.0.0");
            fixture.ApplyTag("0.0.0");
        }
        fixture.MergeTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.1+3", configuration);

        fixture.ApplyTag("0.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.2+1", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-3+6", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-1+6", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Merge main to feature branch (Increment none on main and patch on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureMergeMainToFeatureWithIncrementNoneOnMainAndPatchOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.None)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-2+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-1+1", configuration);
        }

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.MergeTo("feature/foo");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.1-foo.1+2", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.2-foo.1+2", configuration);
        }

        fixture.MakeACommit("D");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.1-foo.1+3", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.2-foo.1+3", configuration);
        }

        fixture.ApplyTag("0.0.2-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.2+1", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.2+2", configuration);

        fixture.MergeTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-1+6", configuration);
    }

    /// <summary>
    /// GitHubFlow - Merge main to feature branch (Increment none on main and minor on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureMergeMainToFeatureWithIncrementNoneOnMainAndMinorOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.None)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Minor)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-2+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-1+1", configuration);
        }

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.MergeTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+3", configuration);

        fixture.ApplyTag("0.1.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.2+1", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+6", configuration);
    }

    /// <summary>
    /// GitHubFlow - Merge main to feature branch (Increment none on main and major on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureMergeMainToFeatureWithIncrementNoneOnMainAndMajorOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.None)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Major)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-2+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-1+1", configuration);
        }

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.MergeTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+3", configuration);

        fixture.ApplyTag("1.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.2+1", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+6", configuration);
    }

    /// <summary>
    /// GitHubFlow - Merge main to feature branch (Increment patch on main and inherit on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureMergeMainToFeatureWithIncrementPatchOnMainAndInheritOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Patch)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.2");
        fixture.MergeTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-foo.1+3", configuration);

        fixture.ApplyTag("0.0.3-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-foo.2+1", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-foo.2+2", configuration);

        fixture.MergeTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-1+6", configuration);
    }

    /// <summary>
    /// GitHubFlow - Merge main to feature branch (Increment patch on main and none on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureMergeMainToFeatureWithIncrementPatchOnMainAndNoneOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Patch)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.None)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.2");
        fixture.MergeTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.1+3", configuration);

        fixture.ApplyTag("0.0.2-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.2+1", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.2+2", configuration);

        fixture.MergeTo("main");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.2-2+6", configuration);
        }
        else
        {
            // ❔ expected: "0.0.2-2+6"
            fixture.AssertFullSemver("0.0.3-1+6", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Merge main to feature branch (Increment patch on main and patch on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureMergeMainToFeatureWithIncrementPatchOnMainAndPatchOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Patch)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.2");
        fixture.MergeTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-foo.1+3", configuration);

        fixture.ApplyTag("0.0.3-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-foo.2+1", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-foo.2+2", configuration);

        fixture.MergeTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-1+6", configuration);
    }

    /// <summary>
    /// GitHubFlow - Merge main to feature branch (Increment patch on main and minor on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureMergeMainToFeatureWithIncrementPatchOnMainAndMinorOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Patch)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Minor)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.2");
        fixture.MergeTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+3", configuration);

        fixture.ApplyTag("0.1.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.2+1", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+6", configuration);
    }

    /// <summary>
    /// GitHubFlow - Merge main to feature branch (Increment patch on main and major on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureMergeMainToFeatureWithIncrementPatchOnMainAndMajorOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Patch)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Major)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.2");
        fixture.MergeTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+3", configuration);

        fixture.ApplyTag("1.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.2+1", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+6", configuration);
    }

    /// <summary>
    /// GitHubFlow - Merge main to feature branch (Increment minor on main and inherit on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureMergeMainToFeatureWithIncrementMinorOnMainAndInheritOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.2.0");
        fixture.MergeTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-foo.1+3", configuration);

        fixture.ApplyTag("0.3.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-foo.2+1", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-1+6", configuration);
    }

    /// <summary>
    /// GitHubFlow - Merge main to feature branch (Increment minor on main and none on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureMergeMainToFeatureWithIncrementMinorOnMainAndNoneOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.None)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.2.0");
        fixture.MergeTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-foo.1+3", configuration);

        fixture.ApplyTag("0.2.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-foo.2+1", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.2.0-2+6", configuration);
        }
        else
        {
            // ❔ expected: "0.2.0-1+6"
            fixture.AssertFullSemver("0.3.0-1+6", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Merge main to feature branch (Increment minor on main and patch on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureMergeMainToFeatureWithIncrementMinorOnMainAndPatchOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.1-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.1-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.2.0");
        fixture.MergeTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.1-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.1-foo.1+3", configuration);

        fixture.ApplyTag("0.2.1-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.1-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.1-foo.2+1", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.1-foo.2+2", configuration);

        fixture.MergeTo("main");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.2.1-1+6", configuration);
        }
        else
        {
            // ❔ expected: "0.2.1-1+6"
            fixture.AssertFullSemver("0.3.0-1+6", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Merge main to feature branch (Increment minor on main and minor on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureMergeMainToFeatureWithIncrementMinorOnMainAndMinorOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Minor)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.2.0");
        fixture.MergeTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-foo.1+3", configuration);

        fixture.ApplyTag("0.3.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-foo.2+1", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-1+6", configuration);
    }

    /// <summary>
    /// GitHubFlow - Merge main to feature branch (Increment minor on main and major on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureMergeMainToFeatureWithIncrementMinorOnMainAndMajorOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Major)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.2.0");
        fixture.MergeTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+3", configuration);

        fixture.ApplyTag("1.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.2+1", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+6", configuration);
    }

    /// <summary>
    /// GitHubFlow - Merge main to feature branch (Increment major on main and inherit on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureMergeMainToFeatureWithIncrementMajorOnMainAndInheritOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Major)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MergeTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.1+3", configuration);

        fixture.ApplyTag("3.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.2+1", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-1+6", configuration);
    }

    /// <summary>
    /// GitHubFlow - Merge main to feature branch (Increment major on main and none on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureMergeMainToFeatureWithIncrementMajorOnMainAndNoneOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Major)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.None)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MergeTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+3", configuration);

        fixture.ApplyTag("2.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.2+1", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("2.0.0-2+6", configuration);
        }
        else
        {
            // ❔ expected: "2.0.0-2+6"
            fixture.AssertFullSemver("3.0.0-1+6", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Merge main to feature branch (Increment major on main and patch on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureMergeMainToFeatureWithIncrementMajorOnMainAndPatchOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Major)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MergeTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-foo.1+3", configuration);

        fixture.ApplyTag("2.0.1-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-foo.2+1", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-foo.2+2", configuration);

        fixture.MergeTo("main");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("2.0.1-1+6", configuration);
        }
        else
        {
            // ❔ expected: "2.0.1-1+6"
            fixture.AssertFullSemver("3.0.0-1+6", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Merge main to feature branch (Increment major on main and minor on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureMergeMainToFeatureWithIncrementMajorOnMainAndMinorOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Major)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Minor)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MergeTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-foo.1+3", configuration);

        fixture.ApplyTag("2.1.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-foo.2+1", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("2.1.0-1+6", configuration);
        }
        else
        {
            // ❔ expected: "2.1.0-1+6"
            fixture.AssertFullSemver("3.0.0-1+6", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Merge main to feature branch (Increment major on main and major on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureMergeMainToFeatureWithIncrementMajorOnMainAndMajorOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Major)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Major)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MergeTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.1+3", configuration);

        fixture.ApplyTag("3.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.2+1", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-foo.2+2", configuration);

        fixture.MergeTo("main");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-1+6", configuration);
    }

    /// <summary>
    /// GitHubFlow - Pull requests (increment inherit on main and inherit on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsurePullRequestWithIncrementInheritOnMainAndInheritOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Inherit)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-PullRequest2.2", configuration);

        fixture.Checkout("main");
        fixture.Remove("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+2", configuration);
    }

    /// <summary>
    /// GitHubFlow - Pull requests (increment inherit on main and none on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsurePullRequestWithIncrementInheritOnMainAndNoneOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Inherit)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.None)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("1.0.0-PullRequest2.2", configuration);
        }
        else
        {
            // ❔ expected: "1.0.0-PullRequest2.2"
            fixture.AssertFullSemver("2.0.0-PullRequest2.2", configuration);
        }

        fixture.Checkout("main");
        fixture.Remove("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("1.0.0-2+2", configuration);
        }
        else
        {
            // ❔ expected: "1.0.0-1+2"
            fixture.AssertFullSemver("2.0.0-1+2", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Pull requests (increment inherit on main and patch on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsurePullRequestWithIncrementInheritOnMainAndPatchOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Inherit)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("1.0.1-PullRequest2.2", configuration);
        }
        else
        {
            // ❔ expected: "1.0.1-PullRequest2.2"
            fixture.AssertFullSemver("2.0.0-PullRequest2.2", configuration);
        }

        fixture.Checkout("main");
        fixture.Remove("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("1.0.1-1+2", configuration);
        }
        else
        {
            // ❔ expected: "1.0.1-1+2"
            fixture.AssertFullSemver("2.0.0-1+2", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Pull requests (increment inherit on main and minor on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsurePullRequestWithIncrementInheritOnMainAndMinorOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Inherit)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Minor)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("1.1.0-PullRequest2.2", configuration);
        }
        else
        {
            // ❔ expected: "1.1.0-PullRequest2.2"
            fixture.AssertFullSemver("2.0.0-PullRequest2.2", configuration);
        }

        fixture.Checkout("main");
        fixture.Remove("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("1.1.0-1+2", configuration);
        }
        else
        {
            // ❔ expected: "1.1.0-1+2"
            fixture.AssertFullSemver("2.0.0-1+2", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Pull requests (increment inherit on main and major on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsurePullRequestWithIncrementInheritOnMainAndMajorOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Inherit)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Major)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-PullRequest2.2", configuration);

        fixture.Checkout("main");
        fixture.Remove("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+2", configuration);
    }

    /// <summary>
    /// GitHubFlow - Pull requests (increment none on main and inherit on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsurePullRequestWithIncrementNoneOnMainAndInheritOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.None)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-PullRequest2.2", configuration);

        fixture.Checkout("main");
        fixture.Remove("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-2+2", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-1+2", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Pull requests (increment none on main and none on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsurePullRequestWithIncrementNoneOnMainAndNoneOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.None)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.None)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-PullRequest2.2", configuration);

        fixture.Checkout("main");
        fixture.Remove("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-2+2", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-1+2", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Pull requests (increment none on main and patch on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsurePullRequestWithIncrementNoneOnMainAndPatchOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.None)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-PullRequest2.2", configuration);

        fixture.Checkout("main");
        fixture.Remove("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.1-1+2", configuration);
        }
        else
        {
            // ❔ expected: "0.0.1-1+2"
            fixture.AssertFullSemver("0.0.0-1+2", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Pull requests (increment none on main and minor on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsurePullRequestWithIncrementNoneOnMainAndMinorOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.None)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Minor)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-PullRequest2.2", configuration);

        fixture.Checkout("main");
        fixture.Remove("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.1.0-1+2", configuration);
        }
        else
        {
            // ❔ expected: "0.1.0-1+2"
            fixture.AssertFullSemver("0.0.0-1+2", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Pull requests (increment none on main and major on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsurePullRequestWithIncrementNoneOnMainAndMajorOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.None)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Major)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-PullRequest2.2", configuration);

        fixture.Checkout("main");
        fixture.Remove("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("1.0.0-1+2", configuration);
        }
        else
        {
            // ❔ expected: "1.0.0-1+2"
            fixture.AssertFullSemver("0.0.0-1+2", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Pull requests (increment patch on main and inherit on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsurePullRequestWithIncrementPatchOnMainAndInheritOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Patch)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-PullRequest2.2", configuration);

        fixture.Checkout("main");
        fixture.Remove("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-1+2", configuration);
    }

    /// <summary>
    /// GitHubFlow - Pull requests (increment patch on main and none on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsurePullRequestWithIncrementPatchOnMainAndNoneOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Patch)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.None)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.1-PullRequest2.2", configuration);
        }
        else
        {
            // ❔ expected: "0.0.1-PullRequest2.2"
            fixture.AssertFullSemver("0.0.2-PullRequest2.2", configuration);
        }

        fixture.Checkout("main");
        fixture.Remove("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.1-2+2", configuration);
        }
        else
        {
            // ❔ expected: "0.0.1-2+2"
            fixture.AssertFullSemver("0.0.2-1+2", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Pull requests (increment patch on main and patch on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsurePullRequestWithIncrementPatchOnMainAndPatchOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Patch)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-PullRequest2.2", configuration);

        fixture.Checkout("main");
        fixture.Remove("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-1+2", configuration);
    }

    /// <summary>
    /// GitHubFlow - Pull requests (increment patch on main and minor on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsurePullRequestWithIncrementPatchOnMainAndMinorOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Patch)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Minor)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-PullRequest2.2", configuration);

        fixture.Checkout("main");
        fixture.Remove("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.1.0-1+2", configuration);
        }
        else
        {
            // ❔ expected: "0.1.0-1+2"
            fixture.AssertFullSemver("0.0.2-1+2", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Pull requests (increment patch on main and major on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsurePullRequestWithIncrementPatchOnMainAndMajorOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Patch)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Major)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-PullRequest2.2", configuration);

        fixture.Checkout("main");
        fixture.Remove("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("1.0.0-1+2", configuration);
        }
        else
        {
            // ❔ expected: "1.0.0-1+2"
            fixture.AssertFullSemver("0.0.2-1+2", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Pull requests (increment minor on main and inherit on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsurePullRequestWithIncrementMinorOnMainAndInheritOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-PullRequest2.2", configuration);

        fixture.Checkout("main");
        fixture.Remove("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-1+2", configuration);
    }

    /// <summary>
    /// GitHubFlow - Pull requests (increment minor on main and none on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsurePullRequestWithIncrementMinorOnMainAndNoneOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.None)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.1.0-PullRequest2.2", configuration);
        }
        else
        {
            // ❔ expected: "0.1.0-PullRequest2.2"
            fixture.AssertFullSemver("0.2.0-PullRequest2.2", configuration);
        }

        fixture.Checkout("main");
        fixture.Remove("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.1.0-2+2", configuration);
        }
        else
        {
            // ❔ expected: "0.1.0-1+2"
            fixture.AssertFullSemver("0.2.0-1+2", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Pull requests (increment minor on main and patch on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsurePullRequestWithIncrementMinorOnMainAndPatchOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.1-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.1-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.1.1-PullRequest2.2", configuration);
        }
        else
        {
            // ❔ expected: "0.1.1-PullRequest2.2"
            fixture.AssertFullSemver("0.2.0-PullRequest2.2", configuration);
        }

        fixture.Checkout("main");
        fixture.Remove("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.1.1-1+2", configuration);
        }
        else
        {
            // ❔ expected: "0.1.1-1+2"
            fixture.AssertFullSemver("0.2.0-1+2", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Pull requests (increment minor on main and minor on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsurePullRequestWithIncrementMinorOnMainAndMinorOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Minor)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-PullRequest2.2", configuration);

        fixture.Checkout("main");
        fixture.Remove("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-1+2", configuration);
    }

    /// <summary>
    /// GitHubFlow - Pull requests (increment minor on main and major on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsurePullRequestWithIncrementMinorOnMainAndMajorOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Major)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-PullRequest2.2", configuration);

        fixture.Checkout("main");
        fixture.Remove("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("1.0.0-1+2", configuration);
        }
        else
        {
            // ❔ expected: "1.0.0-1+2"
            fixture.AssertFullSemver("0.2.0-1+2", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Pull requests (increment major on main and inherit on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsurePullRequestWithIncrementMajorOnMainAndInheritOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Major)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-PullRequest2.2", configuration);

        fixture.Checkout("main");
        fixture.Remove("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+2", configuration);
    }

    /// <summary>
    /// GitHubFlow - Pull requests (increment major on main and none on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsurePullRequestWithIncrementMajorOnMainAndNoneOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Major)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.None)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("1.0.0-PullRequest2.2", configuration);
        }
        else
        {
            // ❔ expected: "1.0.0-PullRequest2.2"
            fixture.AssertFullSemver("2.0.0-PullRequest2.2", configuration);
        }

        fixture.Checkout("main");
        fixture.Remove("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("1.0.0-2+2", configuration);
        }
        else
        {
            // ❔ expected: "1.0.0-1+2"
            fixture.AssertFullSemver("2.0.0-1+2", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Pull requests (increment major on main and patch on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsurePullRequestWithIncrementMajorOnMainAndPatchOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Major)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Patch)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("1.0.1-PullRequest2.2", configuration);
        }
        else
        {
            // ❔ expected: "1.0.1-PullRequest2.2"
            fixture.AssertFullSemver("2.0.0-PullRequest2.2", configuration);
        }

        fixture.Checkout("main");
        fixture.Remove("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("1.0.1-1+2", configuration);
        }
        else
        {
            // ❔ expected: "1.0.1-1+2"
            fixture.AssertFullSemver("2.0.0-1+2", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Pull requests (increment major on main and minor on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsurePullRequestWithIncrementMajorOnMainAndMinorOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Major)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Minor)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("1.1.0-PullRequest2.2", configuration);
        }
        else
        {
            // ❔ expected: "1.1.0-PullRequest2.2"
            fixture.AssertFullSemver("2.0.0-PullRequest2.2", configuration);
        }

        fixture.Checkout("main");
        fixture.Remove("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("1.1.0-1+2", configuration);
        }
        else
        {
            // ❔ expected: "1.1.0-1+2"
            fixture.AssertFullSemver("2.0.0-1+2", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Pull requests (increment major on main and major on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsurePullRequestWithIncrementMajorOnMainAndMajorOnFeatureBranch(bool useTrunkBased)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Major)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Major)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-PullRequest2.2", configuration);

        fixture.Checkout("main");
        fixture.Remove("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+2", configuration);
    }

    /// <summary>
    /// GitHubFlow - Release branch (Increment inherit on main)
    /// </summary>
    [TestCase(false, IncrementStrategy.Inherit)]
    [TestCase(true, IncrementStrategy.Inherit)]
    [TestCase(false, IncrementStrategy.None)]
    [TestCase(true, IncrementStrategy.None)]
    [TestCase(false, IncrementStrategy.Patch)]
    [TestCase(true, IncrementStrategy.Patch)]
    [TestCase(false, IncrementStrategy.Minor)]
    [TestCase(true, IncrementStrategy.Minor)]
    [TestCase(false, IncrementStrategy.Major)]
    [TestCase(true, IncrementStrategy.Major)]
    public void EnsureReleaseBranchWithIncrementInheritOnMain(bool useTrunkBased, IncrementStrategy incrementOnReleaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Inherit)
            ).WithBranch("release", _ => _
                .WithIncrement(incrementOnReleaseBranch)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo("release/3.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MergeTo("release/3.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.1+3", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.1+4", configuration);

        fixture.ApplyTag("3.0.0-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.2+0", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.2+1", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("3.0.0");
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("4.0.0-1+1", configuration);
    }

    /// <summary>
    /// GitHubFlow - Release branch (Increment inherit on main and inherit on release)
    /// </summary>
    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.1.0")]
    [TestCase(true, "release/0.1.0")]
    [TestCase(false, "release/1.0.0")]
    [TestCase(true, "release/1.0.0")]
    [TestCase(false, "release/1.0.1")]
    [TestCase(true, "release/1.0.1")]
    [TestCase(false, "release/1.1.0")]
    [TestCase(true, "release/1.1.0")]
    public void EnsureReleaseBranchWithIncrementInheritOnMainAndInheritOnReleaseBranch(bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Inherit)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Major)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MergeTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.1+3", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.1+4", configuration);

        fixture.ApplyTag("3.0.0-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.2+0", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.2+1", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("3.0.0");
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("4.0.0-1+1", configuration);
    }

    /// <summary>
    /// GitHubFlow - Release branch (Increment inherit on main and none on release)
    /// </summary>
    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.1.0")]
    [TestCase(true, "release/0.1.0")]
    [TestCase(false, "release/1.0.0")]
    [TestCase(true, "release/1.0.0")]
    public void EnsureReleaseBranchWithIncrementInheritOnMainAndNoneOnReleaseBranch(bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Inherit)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.None)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MergeTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+3", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+4", configuration);

        fixture.ApplyTag("2.0.0-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.2+0", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.2+1", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("2.0.0-2+6", configuration);
        }
        else
        {
            // ❔ expected: "2.0.0-2+6"
            fixture.AssertFullSemver("3.0.0-1+6", configuration);
        }

        if (!useTrunkBased)
        {
            fixture.Repository.Tags.Remove("2.0.0");
            fixture.ApplyTag("2.0.0");
        }
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-1+1", configuration);
    }

    /// <summary>
    /// GitHubFlow - Release branch (Increment inherit on main and patch on release)
    /// </summary>
    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.1.0")]
    [TestCase(true, "release/0.1.0")]
    [TestCase(false, "release/1.0.0")]
    [TestCase(true, "release/1.0.0")]
    [TestCase(false, "release/1.0.1")]
    [TestCase(true, "release/1.0.1")]
    public void EnsureReleaseBranchWithIncrementInheritOnMainAndPatchOnReleaseBranch(bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Inherit)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Patch)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MergeTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-beta.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-beta.1+3", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-beta.1+4", configuration);

        fixture.ApplyTag("2.0.1-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-beta.2+0", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-beta.2+1", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("2.0.1-1+6", configuration);
        }
        else
        {
            // ❔ expected: "2.0.1-1+6"
            fixture.AssertFullSemver("3.0.0-1+6", configuration);
        }

        if (!useTrunkBased) fixture.ApplyTag("2.0.1");
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-1+1", configuration);
    }

    /// <summary>
    /// GitHubFlow - Release branch (Increment inherit on main and minor on release)
    /// </summary>
    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.1.0")]
    [TestCase(true, "release/0.1.0")]
    [TestCase(false, "release/1.0.0")]
    [TestCase(true, "release/1.0.0")]
    [TestCase(false, "release/1.0.1")]
    [TestCase(true, "release/1.0.1")]
    [TestCase(false, "release/1.1.0")]
    [TestCase(true, "release/1.1.0")]
    public void EnsureReleaseBranchWithIncrementInheritOnMainAndMinorOnReleaseBranch(bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Inherit)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Minor)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MergeTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-beta.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-beta.1+3", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-beta.1+4", configuration);

        fixture.ApplyTag("2.1.0-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-beta.2+0", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-beta.2+1", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("2.1.0-1+6", configuration);
        }
        else
        {
            // ❔ expected: "2.1.0-1+6"
            fixture.AssertFullSemver("3.0.0-1+6", configuration);
        }

        if (!useTrunkBased) fixture.ApplyTag("2.1.0");
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-1+1", configuration);
    }

    /// <summary>
    /// GitHubFlow - Release branch (Increment inherit on main and major on release)
    /// </summary>
    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.1.0")]
    [TestCase(true, "release/0.1.0")]
    [TestCase(false, "release/1.0.0")]
    [TestCase(true, "release/1.0.0")]
    [TestCase(false, "release/1.0.1")]
    [TestCase(true, "release/1.0.1")]
    [TestCase(false, "release/1.1.0")]
    [TestCase(true, "release/1.1.0")]
    public void EnsureReleaseBranchWithIncrementInheritOnMainAndMajorOnReleaseBranch(bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Inherit)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Major)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MergeTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.1+3", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.1+4", configuration);

        fixture.ApplyTag("3.0.0-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.2+0", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.2+1", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("3.0.0");
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("4.0.0-1+1", configuration);
    }

    /// <summary>
    /// GitHubFlow - Release branch (Increment none on main)
    /// </summary>
    [TestCase(false, IncrementStrategy.Inherit)]
    [TestCase(true, IncrementStrategy.Inherit)]
    [TestCase(false, IncrementStrategy.None)]
    [TestCase(true, IncrementStrategy.None)]
    [TestCase(false, IncrementStrategy.Patch)]
    [TestCase(true, IncrementStrategy.Patch)]
    [TestCase(false, IncrementStrategy.Minor)]
    [TestCase(true, IncrementStrategy.Minor)]
    [TestCase(false, IncrementStrategy.Major)]
    [TestCase(true, IncrementStrategy.Major)]
    public void EnsureReleaseBranchWithIncrementNoneOnMain(bool useTrunkBased, IncrementStrategy incrementOnReleaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.None)
            ).WithBranch("release", _ => _
                .WithIncrement(incrementOnReleaseBranch)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.0");
        fixture.BranchTo("release/2.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-2+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-1+1", configuration);
        }

        if (!useTrunkBased)
        {
            fixture.Repository.Tags.Remove("0.0.0");
            fixture.ApplyTag("0.0.0");
        }
        fixture.MergeTo("release/2.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+3", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+4", configuration);

        fixture.ApplyTag("2.0.0-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.2+0", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.2+1", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MakeACommit("G");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("2.0.0-2+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("2.0.0-1+1", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Release branch (Increment none on main and inherit on release)
    /// </summary>
    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    public void EnsureReleaseBranchWithIncrementNoneOnMainAndInheritOnReleaseBranch(bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.None)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-2+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-1+1", configuration);
        }

        if (!useTrunkBased)
        {
            fixture.Repository.Tags.Remove("0.0.0");
            fixture.ApplyTag("0.0.0");
        }
        fixture.MergeTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-beta.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-beta.1+3", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-beta.1+4", configuration);

        fixture.ApplyTag("0.0.0-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-beta.2+0", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-beta.2+1", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-3+6", configuration);
        }
        else
        {
            // ❔ expected: "0.0.0-3+6"
            fixture.AssertFullSemver("0.0.0-1+6", configuration);
        }

        if (!useTrunkBased)
        {
            fixture.Repository.Tags.Remove("0.0.0");
            fixture.ApplyTag("0.0.0");
        }
        fixture.MakeACommit("G");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-4+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-1+1", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Release branch (Increment none on main and none on release)
    /// </summary>
    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    public void EnsureReleaseBranchWithIncrementNoneOnMainAndNoneOnReleaseBranch(bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.None)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.None)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-2+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-1+1", configuration);
        }

        if (!useTrunkBased)
        {
            fixture.Repository.Tags.Remove("0.0.0");
            fixture.ApplyTag("0.0.0");
        }
        fixture.MergeTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-beta.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-beta.1+3", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-beta.1+4", configuration);

        fixture.ApplyTag("0.0.0-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-beta.2+0", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-beta.2+1", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-3+6", configuration);
        }
        else
        {
            // ❔ expected: "0.0.0-3+6"
            fixture.AssertFullSemver("0.0.0-1+6", configuration);
        }

        if (!useTrunkBased)
        {
            fixture.Repository.Tags.Remove("0.0.0");
            fixture.ApplyTag("0.0.0");
        }
        fixture.MakeACommit("G");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-4+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-1+1", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Release branch (Increment none on main and patch on release)
    /// </summary>
    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    public void EnsureReleaseBranchWithIncrementNoneOnMainAndPatchOnReleaseBranch(bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.None)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Patch)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-2+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-1+1", configuration);
        }

        if (!useTrunkBased)
        {
            fixture.Repository.Tags.Remove("0.0.0");
            fixture.ApplyTag("0.0.0");
        }
        fixture.MergeTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-beta.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-beta.1+3", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-beta.1+4", configuration);

        fixture.ApplyTag("0.0.1-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-beta.2+0", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-beta.2+1", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.MakeACommit("G");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.1-2+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.1-1+1", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Release branch (Increment none on main and minor on release)
    /// </summary>
    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    public void EnsureReleaseBranchWithIncrementNoneOnMainAndMinorOnReleaseBranch(bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.None)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Minor)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-2+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-1+1", configuration);
        }

        if (!useTrunkBased)
        {
            fixture.Repository.Tags.Remove("0.0.0");
            fixture.ApplyTag("0.0.0");
        }
        fixture.MergeTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+3", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+4", configuration);

        fixture.ApplyTag("0.1.0-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.2+0", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.2+1", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.MakeACommit("G");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.1.0-2+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.1.0-1+1", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Release branch (Increment none on main and minor on release)
    /// </summary>
    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    public void EnsureReleaseBranchWithIncrementNoneOnMainAndMajorOnReleaseBranch(bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.None)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Major)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-2+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-1+1", configuration);
        }

        if (!useTrunkBased)
        {
            fixture.Repository.Tags.Remove("0.0.0");
            fixture.ApplyTag("0.0.0");
        }
        fixture.MergeTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+3", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+4", configuration);

        fixture.ApplyTag("1.0.0-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.2+0", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.2+1", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("G");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("1.0.0-2+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("1.0.0-1+1", configuration);
        }
    }

    /// <summary>
    /// GitHubFlow - Release branch (Increment patch on main)
    /// </summary>
    [TestCase(false, IncrementStrategy.Inherit)]
    [TestCase(true, IncrementStrategy.Inherit)]
    [TestCase(false, IncrementStrategy.None)]
    [TestCase(true, IncrementStrategy.None)]
    [TestCase(false, IncrementStrategy.Patch)]
    [TestCase(true, IncrementStrategy.Patch)]
    [TestCase(false, IncrementStrategy.Minor)]
    [TestCase(true, IncrementStrategy.Minor)]
    [TestCase(false, IncrementStrategy.Major)]
    [TestCase(true, IncrementStrategy.Major)]
    public void EnsureReleaseBranchWithIncrementPatchOnMain(bool useTrunkBased, IncrementStrategy incrementOnReleaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Patch)
            ).WithBranch("release", _ => _
                .WithIncrement(incrementOnReleaseBranch)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.BranchTo("release/2.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.2");
        fixture.MergeTo("release/2.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+3", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+4", configuration);

        fixture.ApplyTag("2.0.0-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.2+0", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.2+1", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-1+1", configuration);
    }

    /// <summary>
    /// GitHubFlow - Release branch (Increment patch on main and inherit on release)
    /// </summary>
    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.0.2")]
    [TestCase(true, "release/0.0.2")]
    public void EnsureReleaseBranchWithIncrementPatchOnMainAndInheritOnReleaseBranch(bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Patch)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.2");
        fixture.MergeTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-beta.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-beta.1+3", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-beta.1+4", configuration);

        fixture.ApplyTag("0.0.3-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-beta.2+0", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-beta.2+1", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.3");
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.4-1+1", configuration);
    }

    /// <summary>
    /// GitHubFlow - Release branch (Increment patch on main and none on release)
    /// </summary>
    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    public void EnsureReleaseBranchWithIncrementPatchOnMainAndNoneOnReleaseBranch(bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Patch)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.None)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.2");
        fixture.MergeTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.1+3", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.1+4", configuration);

        fixture.ApplyTag("0.0.2-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.2+0", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.2+1", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.2-2+6", configuration);
        }
        else
        {
            // ❔ expected: "0.0.2-2+6"
            fixture.AssertFullSemver("0.0.3-1+6", configuration);
        }

        if (!useTrunkBased)
        {
            fixture.Repository.Tags.Remove("0.0.2");
            fixture.ApplyTag("0.0.2");
        }
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-1+1", configuration);
    }

    /// <summary>
    /// GitHubFlow - Release branch (Increment patch on main and patch on release)
    /// </summary>
    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.0.2")]
    [TestCase(true, "release/0.0.2")]
    public void EnsureReleaseBranchWithIncrementPatchOnMainAndPatchOnReleaseBranch(bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Patch)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Patch)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.2");
        fixture.MergeTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-beta.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-beta.1+3", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-beta.1+4", configuration);

        fixture.ApplyTag("0.0.3-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-beta.2+0", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-beta.2+1", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.3");
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.4-1+1", configuration);
    }

    /// <summary>
    /// GitHubFlow - Release branch (Increment patch on main and minor on release)
    /// </summary>
    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    public void EnsureReleaseBranchWithIncrementPatchOnMainAndMinorOnReleaseBranch(bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Patch)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Minor)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.2");
        fixture.MergeTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+3", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+4", configuration);

        fixture.ApplyTag("0.1.0-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.2+0", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.2+1", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.1-1+1", configuration);
    }

    /// <summary>
    /// GitHubFlow - Release branch (Increment patch on main and major on release)
    /// </summary>
    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    public void EnsureReleaseBranchWithIncrementPatchOnMainAndMajorOnReleaseBranch(bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Patch)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Major)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.2");
        fixture.MergeTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+3", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+4", configuration);

        fixture.ApplyTag("1.0.0-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.2+0", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.2+1", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-1+1", configuration);
    }

    /// <summary>
    /// GitHubFlow - Release branch (Increment minor on main)
    /// </summary>
    [TestCase(false, IncrementStrategy.Inherit)]
    [TestCase(true, IncrementStrategy.Inherit)]
    [TestCase(false, IncrementStrategy.None)]
    [TestCase(true, IncrementStrategy.None)]
    [TestCase(false, IncrementStrategy.Patch)]
    [TestCase(true, IncrementStrategy.Patch)]
    [TestCase(false, IncrementStrategy.Minor)]
    [TestCase(true, IncrementStrategy.Minor)]
    [TestCase(false, IncrementStrategy.Major)]
    [TestCase(true, IncrementStrategy.Major)]
    public void EnsureReleaseBranchWithIncrementMinorOnMain(bool useTrunkBased, IncrementStrategy incrementOnReleaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("release", _ => _
                .WithIncrement(incrementOnReleaseBranch)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.BranchTo("release/2.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.2.0");
        fixture.MergeTo("release/2.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+3", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+4", configuration);

        fixture.ApplyTag("2.0.0-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.2+0", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.2+1", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-1+1", configuration);
    }

    /// <summary>
    /// GitHubFlow - Release branch (Increment minor on main and inherit on release)
    /// </summary>
    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.2.0")]
    [TestCase(true, "release/0.2.0")]
    public void EnsureReleaseBranchWithIncrementMinorOnMainAndInheritOnReleaseBranch(bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.2.0");
        fixture.MergeTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-beta.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-beta.1+3", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-beta.1+4", configuration);

        fixture.ApplyTag("0.3.0-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-beta.2+0", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-beta.2+1", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.3.0");
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.4.0-1+1", configuration);
    }

    /// <summary>
    /// GitHubFlow - Release branch (Increment minor on main and none on release)
    /// </summary>
    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.1.0")]
    [TestCase(true, "release/0.1.0")]
    public void EnsureReleaseBranchWithIncrementMinorOnMainAndNoneOnReleaseBranch(bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.None)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.2.0");
        fixture.MergeTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-beta.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-beta.1+3", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-beta.1+4", configuration);

        fixture.ApplyTag("0.2.0-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-beta.2+0", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-beta.2+1", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.2.0-2+6", configuration);
        }
        else
        {
            // ❔ expected: "0.2.0-2+6"
            fixture.AssertFullSemver("0.3.0-1+6", configuration);
        }

        if (!useTrunkBased)
        {
            fixture.Repository.Tags.Remove("0.2.0");
            fixture.ApplyTag("0.2.0");
        }
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-1+1", configuration);
    }

    /// <summary>
    /// GitHubFlow - Release branch (Increment minor on main and patch on release)
    /// </summary>
    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.1.1")]
    [TestCase(true, "release/0.1.1")]
    public void EnsureReleaseBranchWithIncrementMinorOnMainAndPatchOnReleaseBranch(bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Patch)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.1-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.1-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.2.0");
        fixture.MergeTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.1-beta.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.1-beta.1+3", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.1-beta.1+4", configuration);

        fixture.ApplyTag("0.2.1-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.1-beta.2+0", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.1-beta.2+1", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.2.1-1+6", configuration);
        }
        else
        {
            // ❔ expected: "0.2.1-1+6"
            fixture.AssertFullSemver("0.3.0-1+6", configuration);
        }

        if (!useTrunkBased) fixture.ApplyTag("0.2.1");
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-1+1", configuration);
    }

    /// <summary>
    /// GitHubFlow - Release branch (Increment minor on main and minor on release)
    /// </summary>
    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.2.0")]
    [TestCase(true, "release/0.2.0")]
    public void EnsureReleaseBranchWithIncrementMinorOnMainAndMinorOnReleaseBranch(bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Minor)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.2.0");
        fixture.MergeTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-beta.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-beta.1+3", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-beta.1+4", configuration);

        fixture.ApplyTag("0.3.0-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-beta.2+0", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-beta.2+1", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.3.0");
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.4.0-1+1", configuration);
    }

    /// <summary>
    /// GitHubFlow - Release branch (Increment minor on main and major on release)
    /// </summary>
    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.1.0")]
    [TestCase(true, "release/0.1.0")]
    [TestCase(false, "release/0.1.1")]
    [TestCase(true, "release/0.1.1")]
    public void EnsureReleaseBranchWithIncrementMinorOnMainAndMajorOnReleaseBranch(bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Major)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.2.0");
        fixture.MergeTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+3", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+4", configuration);

        fixture.ApplyTag("1.0.0-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.2+0", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.2+1", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-1+1", configuration);
    }

    /// <summary>
    /// GitHubFlow - Release branch (Increment major on main)
    /// </summary>
    [TestCase(false, IncrementStrategy.Inherit)]
    [TestCase(true, IncrementStrategy.Inherit)]
    [TestCase(false, IncrementStrategy.None)]
    [TestCase(true, IncrementStrategy.None)]
    [TestCase(false, IncrementStrategy.Patch)]
    [TestCase(true, IncrementStrategy.Patch)]
    [TestCase(false, IncrementStrategy.Minor)]
    [TestCase(true, IncrementStrategy.Minor)]
    [TestCase(false, IncrementStrategy.Major)]
    [TestCase(true, IncrementStrategy.Major)]
    public void EnsureReleaseBranchWithIncrementMajorOnMain(bool useTrunkBased, IncrementStrategy incrementOnReleaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Major)
            ).WithBranch("release", _ => _
                .WithIncrement(incrementOnReleaseBranch)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo("release/3.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MergeTo("release/3.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.1+3", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.1+4", configuration);

        fixture.ApplyTag("3.0.0-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.2+0", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.2+1", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("3.0.0");
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("4.0.0-1+1", configuration);
    }

    /// <summary>
    /// GitHubFlow - Release branch (Increment major on main and inherit on release)
    /// </summary>
    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/1.0.0")]
    [TestCase(true, "release/1.0.0")]
    [TestCase(false, "release/1.0.1")]
    [TestCase(true, "release/1.0.1")]
    [TestCase(false, "release/1.1.0")]
    [TestCase(true, "release/1.1.0")]
    [TestCase(false, "release/1.1.1")]
    [TestCase(true, "release/1.1.1")]
    public void EnsureReleaseBranchWithIncrementMajorOnMainAndInheritOnReleaseBranch(bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Major)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MergeTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.1+3", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.1+4", configuration);

        fixture.ApplyTag("3.0.0-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.2+0", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.2+1", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("3.0.0");
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("4.0.0-1+1", configuration);
    }

    /// <summary>
    /// GitHubFlow - Release branch (Increment major on main and none on release)
    /// </summary>
    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/1.0.0")]
    [TestCase(true, "release/1.0.0")]
    public void EnsureReleaseBranchWithIncrementMajorOnMainAndNoneOnReleaseBranch(bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Major)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.None)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MergeTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+3", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+4", configuration);

        fixture.ApplyTag("2.0.0-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.2+0", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.2+1", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("2.0.0-2+6", configuration);
        }
        else
        {
            // ❔ expected: "2.0.0-2+6"
            fixture.AssertFullSemver("3.0.0-1+6", configuration);
        }

        if (!useTrunkBased)
        {
            fixture.Repository.Tags.Remove("2.0.0");
            fixture.ApplyTag("2.0.0");
        }
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-1+1", configuration);
    }

    /// <summary>
    /// GitHubFlow - Release branch (Increment major on main and patch on release)
    /// </summary>
    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/1.0.0")]
    [TestCase(true, "release/1.0.0")]
    [TestCase(false, "release/1.0.1")]
    [TestCase(true, "release/1.0.1")]
    public void EnsureReleaseBranchWithIncrementMajorOnMainAndPatchOnReleaseBranch(bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Major)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Patch)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MergeTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-beta.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-beta.1+3", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-beta.1+4", configuration);

        fixture.ApplyTag("2.0.1-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-beta.2+0", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-beta.2+1", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("2.0.1-1+6", configuration);
        }
        else
        {
            // ❔ expected: "2.0.1-1+6"
            fixture.AssertFullSemver("3.0.0-1+6", configuration);
        }

        if (!useTrunkBased) fixture.ApplyTag("2.0.1");
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-1+1", configuration);
    }

    /// <summary>
    /// GitHubFlow - Release branch (Increment major on main and minor on release)
    /// </summary>
    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/1.0.0")]
    [TestCase(true, "release/1.0.0")]
    [TestCase(false, "release/1.0.1")]
    [TestCase(true, "release/1.0.1")]
    [TestCase(false, "release/1.1.0")]
    [TestCase(true, "release/1.1.0")]
    public void EnsureReleaseBranchWithIncrementMajorOnMainAndMinorOnReleaseBranch(bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Major)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Minor)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MergeTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-beta.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-beta.1+3", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-beta.1+4", configuration);

        fixture.ApplyTag("2.1.0-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-beta.2+0", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-beta.2+1", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("2.1.0-1+6", configuration);
        }
        else
        {
            // ❔ expected: "2.1.0-1+6"
            fixture.AssertFullSemver("3.0.0-1+6", configuration);
        }

        if (!useTrunkBased) fixture.ApplyTag("2.1.0");
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-1+1", configuration);
    }

    /// <summary>
    /// GitHubFlow - Release branch (Increment major on main and major on release)
    /// </summary>
    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.1.0")]
    [TestCase(true, "release/0.1.0")]
    [TestCase(false, "release/0.1.1")]
    [TestCase(true, "release/0.1.1")]
    [TestCase(false, "release/1.0.0")]
    [TestCase(true, "release/1.0.0")]
    [TestCase(false, "release/1.0.1")]
    [TestCase(true, "release/1.0.1")]
    [TestCase(false, "release/1.1.0")]
    [TestCase(true, "release/1.1.0")]
    [TestCase(false, "release/1.1.1")]
    [TestCase(true, "release/1.1.1")]
    public void EnsureReleaseBranchWithIncrementMajorOnMainAndMajorOnReleaseBranch(bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Major)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Major)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MergeTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.1+3", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.1+4", configuration);

        fixture.ApplyTag("3.0.0-beta.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.2+0", configuration);

        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-beta.2+1", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("3.0.0");
        fixture.MakeACommit("G");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("4.0.0-1+1", configuration);
    }

    [TestCase(true, IncrementStrategy.Inherit)]
    [TestCase(false, IncrementStrategy.None)]
    [TestCase(true, IncrementStrategy.None)]
    [TestCase(false, IncrementStrategy.Patch)]
    [TestCase(true, IncrementStrategy.Patch)]
    [TestCase(false, IncrementStrategy.Minor)]
    [TestCase(true, IncrementStrategy.Minor)]
    [TestCase(false, IncrementStrategy.Major)]
    [TestCase(true, IncrementStrategy.Major)]
    public void EnsureReleaseAndFeatureBranchWithIncrementInheritOnMain(
        bool useTrunkBased, IncrementStrategy incrementOnReleaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Inherit)
            ).WithBranch("release", _ => _
                .WithIncrement(incrementOnReleaseBranch)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo("release/2.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+3", configuration);

        fixture.ApplyTag("2.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.2+1", configuration);

        fixture.Checkout("release/2.0.0");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo("release/2.0.0", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-1+1", configuration);
    }

    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.0.2")]
    [TestCase(true, "release/0.0.2")]
    [TestCase(false, "release/0.1.0")]
    [TestCase(true, "release/0.1.0")]
    [TestCase(false, "release/0.1.1")]
    [TestCase(true, "release/0.1.1")]
    [TestCase(false, "release/0.2.0")]
    [TestCase(true, "release/0.2.0")]
    [TestCase(false, "release/0.1.1")]
    [TestCase(true, "release/0.1.1")]
    [TestCase(false, "release/1.0.0")]
    [TestCase(true, "release/1.0.0")]
    public void EnsureReleaseAndFeatureBranchWithIncrementInheritOnMainAndInheritOnReleaseBranch(
        bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Inherit)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+3", configuration);

        fixture.ApplyTag("2.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.2+1", configuration);

        fixture.Checkout(releaseBranch);
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo(releaseBranch, removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-1+1", configuration);
    }

    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.0.2")]
    [TestCase(true, "release/0.0.2")]
    [TestCase(false, "release/0.1.0")]
    [TestCase(true, "release/0.1.0")]
    [TestCase(false, "release/0.1.1")]
    [TestCase(true, "release/0.1.1")]
    [TestCase(false, "release/0.2.0")]
    [TestCase(true, "release/0.2.0")]
    [TestCase(false, "release/1.0.0")]
    [TestCase(true, "release/1.0.0")]
    public void EnsureReleaseAndFeatureBranchWithIncrementInheritOnMainAndNoneOnReleaseBranch(
        bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Inherit)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.None)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+3", configuration);

        fixture.ApplyTag("1.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.2+1", configuration);

        fixture.Checkout(releaseBranch);
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo(releaseBranch, removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("1.0.0-2+6", configuration);
        }
        else
        {
            // ❔ expected: "1.0.0-2+6"
            fixture.AssertFullSemver("2.0.0-1+6", configuration);
        }

        if (!useTrunkBased)
        {
            fixture.Repository.Tags.Remove("1.0.0");
            fixture.ApplyTag("1.0.0");
        }
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);
    }

    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.0.2")]
    [TestCase(true, "release/0.0.2")]
    [TestCase(false, "release/0.1.0")]
    [TestCase(true, "release/0.1.0")]
    [TestCase(false, "release/0.1.1")]
    [TestCase(true, "release/0.1.1")]
    [TestCase(false, "release/0.2.0")]
    [TestCase(true, "release/0.2.0")]
    [TestCase(false, "release/1.0.0")]
    [TestCase(true, "release/1.0.0")]
    [TestCase(false, "release/1.0.1")]
    [TestCase(true, "release/1.0.1")]
    public void EnsureReleaseAndFeatureBranchWithIncrementInheritOnMainAndPatchOnReleaseBranch(
        bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Inherit)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Patch)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-foo.1+3", configuration);

        fixture.ApplyTag("1.0.1-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-foo.2+1", configuration);

        fixture.Checkout(releaseBranch);
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo(releaseBranch, removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("1.0.1-1+6", configuration);
        }
        else
        {
            // ❔ expected: "1.0.1-1+6"
            fixture.AssertFullSemver("2.0.0-1+6", configuration);
        }

        if (!useTrunkBased) fixture.ApplyTag("1.0.1");
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);
    }

    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.0.2")]
    [TestCase(true, "release/0.0.2")]
    [TestCase(false, "release/0.1.0")]
    [TestCase(true, "release/0.1.0")]
    [TestCase(false, "release/0.1.1")]
    [TestCase(true, "release/0.1.1")]
    [TestCase(false, "release/0.2.0")]
    [TestCase(true, "release/0.2.0")]
    [TestCase(false, "release/1.0.0")]
    [TestCase(true, "release/1.0.0")]
    [TestCase(false, "release/1.0.1")]
    [TestCase(true, "release/1.0.1")]
    [TestCase(false, "release/1.0.2")]
    [TestCase(true, "release/1.0.2")]
    [TestCase(false, "release/1.1.0")]
    [TestCase(true, "release/1.1.0")]
    public void EnsureReleaseAndFeatureBranchWithIncrementInheritOnMainAndMinorOnReleaseBranch(
        bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Inherit)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Minor)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-foo.1+3", configuration);

        fixture.ApplyTag("1.1.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-foo.2+1", configuration);

        fixture.Checkout(releaseBranch);
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo(releaseBranch, removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("1.1.0-1+6", configuration);
        }
        else
        {
            // ❔ expected: "1.1.0-1+6"
            fixture.AssertFullSemver("2.0.0-1+6", configuration);
        }

        if (!useTrunkBased) fixture.ApplyTag("1.1.0");
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);
    }

    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.0.2")]
    [TestCase(true, "release/0.0.2")]
    [TestCase(false, "release/0.1.0")]
    [TestCase(true, "release/0.1.0")]
    [TestCase(false, "release/0.1.1")]
    [TestCase(true, "release/0.1.1")]
    [TestCase(false, "release/0.2.0")]
    [TestCase(true, "release/0.2.0")]
    [TestCase(false, "release/1.0.0")]
    [TestCase(true, "release/1.0.0")]
    [TestCase(false, "release/1.0.1")]
    [TestCase(true, "release/1.0.1")]
    [TestCase(false, "release/1.0.2")]
    [TestCase(true, "release/1.0.2")]
    [TestCase(false, "release/1.1.0")]
    [TestCase(true, "release/1.1.0")]
    [TestCase(false, "release/1.1.1")]
    [TestCase(true, "release/1.1.1")]
    [TestCase(false, "release/2.0.0")]
    [TestCase(true, "release/2.0.0")]
    public void EnsureReleaseAndFeatureBranchWithIncrementInheritOnMainAndMajorOnReleaseBranch(
        bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Inherit)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Major)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+3", configuration);

        fixture.ApplyTag("2.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.2+1", configuration);

        fixture.Checkout(releaseBranch);
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo(releaseBranch, removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-1+1", configuration);
    }

    [TestCase(true, IncrementStrategy.Inherit)]
    [TestCase(false, IncrementStrategy.None)]
    [TestCase(true, IncrementStrategy.None)]
    [TestCase(false, IncrementStrategy.Patch)]
    [TestCase(true, IncrementStrategy.Patch)]
    [TestCase(false, IncrementStrategy.Minor)]
    [TestCase(true, IncrementStrategy.Minor)]
    [TestCase(false, IncrementStrategy.Major)]
    [TestCase(true, IncrementStrategy.Major)]
    public void EnsureReleaseAndFeatureBranchWithIncrementNoneOnMain(
        bool useTrunkBased, IncrementStrategy incrementOnReleaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.None)
            ).WithBranch("release", _ => _
                .WithIncrement(incrementOnReleaseBranch)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.0");
        fixture.BranchTo("release/2.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+3", configuration);

        fixture.ApplyTag("2.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.2+1", configuration);

        fixture.Checkout("release/2.0.0");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo("release/2.0.0", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MakeACommit("F");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("2.0.0-2+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("2.0.0-1+1", configuration);
        }
    }

    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    public void EnsureReleaseAndFeatureBranchWithIncrementNoneOnMainAndInheritOnReleaseBranch(
        bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.None)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.1+3", configuration);

        fixture.ApplyTag("0.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.2+1", configuration);

        fixture.Checkout(releaseBranch);
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo(releaseBranch, removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-2+6", configuration);
        }
        else
        {
            // ❔ expected: "0.0.0-2+6"
            fixture.AssertFullSemver("0.0.0-1+6", configuration);
        }

        if (!useTrunkBased)
        {
            fixture.Repository.Tags.Remove("0.0.0");
            fixture.ApplyTag("0.0.0");
        }
        fixture.MakeACommit("F");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-3+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-1+1", configuration);
        }
    }

    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    public void EnsureReleaseAndFeatureBranchWithIncrementNoneOnMainAndNoneOnReleaseBranch(
        bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.None)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.None)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.1+3", configuration);

        fixture.ApplyTag("0.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.2+1", configuration);

        fixture.Checkout(releaseBranch);
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo(releaseBranch, removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-2+6", configuration);
        }
        else
        {
            // ❔ expected: "0.0.0-2+6"
            fixture.AssertFullSemver("0.0.0-1+6", configuration);
        }

        if (!useTrunkBased)
        {
            fixture.Repository.Tags.Remove("0.0.0");
            fixture.ApplyTag("0.0.0");
        }
        fixture.MakeACommit("F");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-3+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-1+1", configuration);
        }
    }

    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(true, "release/0.0.1")]
    public void EnsureReleaseAndFeatureBranchWithIncrementNoneOnMainAndPatchOnReleaseBranch(
        bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.None)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Patch)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-foo.1+3", configuration);

        fixture.ApplyTag("0.0.1-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-foo.2+1", configuration);

        fixture.Checkout(releaseBranch);
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo(releaseBranch, removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.MakeACommit("F");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.1-2+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.1-1+1", configuration);
        }
    }

    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(true, "release/0.0.2")]
    [TestCase(true, "release/0.1.0")]
    public void EnsureReleaseAndFeatureBranchWithIncrementNoneOnMainAndMinorOnReleaseBranch(
        bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.None)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Minor)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+3", configuration);

        fixture.ApplyTag("0.1.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.2+1", configuration);

        fixture.Checkout(releaseBranch);
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo(releaseBranch, removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.MakeACommit("F");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.1.0-2+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.1.0-1+1", configuration);
        }
    }

    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(true, "release/0.0.2")]
    [TestCase(true, "release/0.1.0")]
    [TestCase(true, "release/0.1.1")]
    [TestCase(true, "release/0.2.0")]
    [TestCase(true, "release/1.0.0")]
    public void EnsureReleaseAndFeatureBranchWithIncrementNoneOnMainAndMajorOnReleaseBranch(
        bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.None)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Major)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+3", configuration);

        fixture.ApplyTag("1.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.2+1", configuration);

        fixture.Checkout(releaseBranch);
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo(releaseBranch, removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("F");

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("1.0.0-2+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("1.0.0-1+1", configuration);
        }
    }

    [TestCase(true, IncrementStrategy.Inherit)]
    [TestCase(false, IncrementStrategy.None)]
    [TestCase(true, IncrementStrategy.None)]
    [TestCase(false, IncrementStrategy.Patch)]
    [TestCase(true, IncrementStrategy.Patch)]
    [TestCase(false, IncrementStrategy.Minor)]
    [TestCase(true, IncrementStrategy.Minor)]
    [TestCase(false, IncrementStrategy.Major)]
    [TestCase(true, IncrementStrategy.Major)]
    public void EnsureReleaseAndFeatureBranchWithIncrementPatchOnMain(
        bool useTrunkBased, IncrementStrategy incrementOnReleaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Patch)
            ).WithBranch("release", _ => _
                .WithIncrement(incrementOnReleaseBranch)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.BranchTo("release/2.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+3", configuration);

        fixture.ApplyTag("2.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.2+1", configuration);

        fixture.Checkout("release/2.0.0");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo("release/2.0.0", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.1-1+1", configuration);
    }

    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.0.2")]
    [TestCase(true, "release/0.0.2")]
    public void EnsureReleaseAndFeatureBranchWithIncrementPatchOnMainAndInheritOnReleaseBranch(
        bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Patch)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.1+3", configuration);

        fixture.ApplyTag("0.0.2-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.2+1", configuration);

        fixture.Checkout(releaseBranch);
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo(releaseBranch, removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.2");
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-1+1", configuration);
    }

    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    public void EnsureReleaseAndFeatureBranchWithIncrementPatchOnMainAndNoneOnReleaseBranch(
        bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Patch)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.None)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-foo.1+3", configuration);

        fixture.ApplyTag("0.0.1-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-foo.2+1", configuration);

        fixture.Checkout(releaseBranch);
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo(releaseBranch, removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.1-2+6", configuration);
        }
        else
        {
            // ❔ expected: "0.0.1-2+6"
            fixture.AssertFullSemver("0.0.2-1+6", configuration);
        }

        if (!useTrunkBased)
        {
            fixture.Repository.Tags.Remove("0.0.1");
            fixture.ApplyTag("0.0.1");
        }
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-1+1", configuration);
    }

    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.0.2")]
    [TestCase(true, "release/0.0.2")]
    public void EnsureReleaseAndFeatureBranchWithIncrementPatchOnMainAndPatchOnReleaseBranch(
        bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Patch)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Patch)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.1+3", configuration);

        fixture.ApplyTag("0.0.2-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-foo.2+1", configuration);

        fixture.Checkout(releaseBranch);
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo(releaseBranch, removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.2");
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3-1+1", configuration);
    }

    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(true, "release/0.1.0")]
    public void EnsureReleaseAndFeatureBranchWithIncrementPatchOnMainAndMinorOnReleaseBranch(
        bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Patch)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Minor)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+3", configuration);

        fixture.ApplyTag("0.1.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.2+1", configuration);

        fixture.Checkout(releaseBranch);
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo(releaseBranch, removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.1-1+1", configuration);
    }

    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.0.2")]
    [TestCase(true, "release/0.0.2")]
    [TestCase(true, "release/0.1.0")]
    [TestCase(true, "release/0.1.1")]
    [TestCase(true, "release/1.0.0")]
    public void EnsureReleaseAndFeatureBranchWithIncrementPatchOnMainAndMajorOnReleaseBranch(
        bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Patch)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Major)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.0.1");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+3", configuration);

        fixture.ApplyTag("1.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.2+1", configuration);

        fixture.Checkout(releaseBranch);
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo(releaseBranch, removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-1+1", configuration);
    }

    [TestCase(true, IncrementStrategy.Inherit)]
    [TestCase(false, IncrementStrategy.None)]
    [TestCase(true, IncrementStrategy.None)]
    [TestCase(false, IncrementStrategy.Patch)]
    [TestCase(true, IncrementStrategy.Patch)]
    [TestCase(false, IncrementStrategy.Minor)]
    [TestCase(true, IncrementStrategy.Minor)]
    [TestCase(false, IncrementStrategy.Major)]
    [TestCase(true, IncrementStrategy.Major)]
    public void EnsureReleaseAndFeatureBranchWithIncrementMinorOnMain(
        bool useTrunkBased, IncrementStrategy incrementOnReleaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("release", _ => _
                .WithIncrement(incrementOnReleaseBranch)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.BranchTo("release/2.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+3", configuration);

        fixture.ApplyTag("2.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.2+1", configuration);

        fixture.Checkout("release/2.0.0");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo("release/2.0.0", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.1.0-1+1", configuration);
    }

    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.0.2")]
    [TestCase(true, "release/0.0.2")]
    [TestCase(false, "release/0.1.0")]
    [TestCase(true, "release/0.1.0")]
    [TestCase(false, "release/0.1.1")]
    [TestCase(true, "release/0.1.1")]
    [TestCase(false, "release/0.2.0")]
    [TestCase(true, "release/0.2.0")]
    public void EnsureReleaseAndFeatureBranchWithIncrementMinorOnMainAndInheritOnReleaseBranch(
        bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-foo.1+3", configuration);

        fixture.ApplyTag("0.2.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-foo.2+1", configuration);

        fixture.Checkout(releaseBranch);
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo(releaseBranch, removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.2.0");
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-1+1", configuration);
    }

    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.0.2")]
    [TestCase(true, "release/0.0.2")]
    [TestCase(false, "release/0.1.0")]
    [TestCase(true, "release/0.1.0")]
    public void EnsureReleaseAndFeatureBranchWithIncrementMinorOnMainAndNoneOnReleaseBranch(
        bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.None)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+3", configuration);

        fixture.ApplyTag("0.1.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.2+1", configuration);

        fixture.Checkout(releaseBranch);
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo(releaseBranch, removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.1.0-2+6", configuration);
        }
        else
        {
            // ❔ not expected
            fixture.AssertFullSemver("0.2.0-1+6", configuration);
        }

        if (!useTrunkBased)
        {
            fixture.Repository.Tags.Remove("0.1.0");
            fixture.ApplyTag("0.1.0");
        }
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-1+1", configuration);
    }

    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.0.2")]
    [TestCase(true, "release/0.0.2")]
    [TestCase(false, "release/0.1.0")]
    [TestCase(true, "release/0.1.0")]
    [TestCase(false, "release/0.1.1")]
    [TestCase(true, "release/0.1.1")]
    public void EnsureReleaseAndFeatureBranchWithIncrementMinorOnMainAndPatchOnReleaseBranch(
        bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Patch)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.1-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.1-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.1-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.1-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.1-foo.1+3", configuration);

        fixture.ApplyTag("0.1.1-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.1-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.1-foo.2+1", configuration);

        fixture.Checkout(releaseBranch);
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.1-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo(releaseBranch, removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.1-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.1.1-1+6", configuration);
        }
        else
        {
            // ❔ not expected
            fixture.AssertFullSemver("0.2.0-1+6", configuration);
        }

        if (!useTrunkBased) fixture.ApplyTag("0.1.1");
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-1+1", configuration);
    }

    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.0.2")]
    [TestCase(true, "release/0.0.2")]
    [TestCase(false, "release/0.1.0")]
    [TestCase(true, "release/0.1.0")]
    [TestCase(false, "release/0.1.1")]
    [TestCase(true, "release/0.1.1")]
    [TestCase(false, "release/0.2.0")]
    [TestCase(true, "release/0.2.0")]
    public void EnsureReleaseAndFeatureBranchWithIncrementMinorOnMainAndMinorOnReleaseBranch(
        bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Minor)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-foo.1+3", configuration);

        fixture.ApplyTag("0.2.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-foo.2+1", configuration);

        fixture.Checkout(releaseBranch);
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo(releaseBranch, removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.2.0");
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.3.0-1+1", configuration);
    }

    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.0.2")]
    [TestCase(true, "release/0.0.2")]
    [TestCase(false, "release/0.1.0")]
    [TestCase(true, "release/0.1.0")]
    [TestCase(false, "release/0.1.1")]
    [TestCase(true, "release/0.1.1")]
    [TestCase(false, "release/0.2.0")]
    [TestCase(true, "release/0.2.0")]
    public void EnsureReleaseAndFeatureBranchWithIncrementMinorOnMainAndMajorOnReleaseBranch(
        bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Minor)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Major)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("0.1.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+3", configuration);

        fixture.ApplyTag("1.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.2+1", configuration);

        fixture.Checkout(releaseBranch);
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo(releaseBranch, removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-1+1", configuration);
    }

    [TestCase(true, IncrementStrategy.Inherit)]
    [TestCase(false, IncrementStrategy.None)]
    [TestCase(true, IncrementStrategy.None)]
    [TestCase(false, IncrementStrategy.Patch)]
    [TestCase(true, IncrementStrategy.Patch)]
    [TestCase(false, IncrementStrategy.Minor)]
    [TestCase(true, IncrementStrategy.Minor)]
    [TestCase(false, IncrementStrategy.Major)]
    [TestCase(true, IncrementStrategy.Major)]
    public void EnsureReleaseAndFeatureBranchWithIncrementMajorOnMain(
        bool useTrunkBased, IncrementStrategy incrementOnReleaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Major)
            ).WithBranch("release", _ => _
                .WithIncrement(incrementOnReleaseBranch)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo("release/2.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+3", configuration);

        fixture.ApplyTag("2.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.2+1", configuration);

        fixture.Checkout("release/2.0.0");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo("release/2.0.0", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-1+1", configuration);
    }

    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.0.2")]
    [TestCase(true, "release/0.0.2")]
    [TestCase(false, "release/0.1.0")]
    [TestCase(true, "release/0.1.0")]
    [TestCase(false, "release/0.1.1")]
    [TestCase(true, "release/0.1.1")]
    [TestCase(false, "release/0.2.0")]
    [TestCase(true, "release/0.2.0")]
    [TestCase(false, "release/0.1.1")]
    [TestCase(true, "release/0.1.1")]
    [TestCase(false, "release/1.0.0")]
    [TestCase(true, "release/1.0.0")]
    public void EnsureReleaseAndFeatureBranchWithIncrementMajorOnMainAndInheritOnReleaseBranch(
        bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Major)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+3", configuration);

        fixture.ApplyTag("2.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.2+1", configuration);

        fixture.Checkout(releaseBranch);
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo(releaseBranch, removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-1+1", configuration);
    }

    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.0.2")]
    [TestCase(true, "release/0.0.2")]
    [TestCase(false, "release/0.1.0")]
    [TestCase(true, "release/0.1.0")]
    [TestCase(false, "release/0.1.1")]
    [TestCase(true, "release/0.1.1")]
    [TestCase(false, "release/0.2.0")]
    [TestCase(true, "release/0.2.0")]
    [TestCase(false, "release/1.0.0")]
    [TestCase(true, "release/1.0.0")]
    public void EnsureReleaseAndFeatureBranchWithIncrementMajorOnMainAndNoneOnReleaseBranch(
        bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Major)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.None)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+3", configuration);

        fixture.ApplyTag("1.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.2+1", configuration);

        fixture.Checkout(releaseBranch);
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo(releaseBranch, removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("1.0.0-2+6", configuration);
        }
        else
        {
            // ❔ expected: "1.0.0-2+6"
            fixture.AssertFullSemver("2.0.0-1+6", configuration);
        }

        if (!useTrunkBased)
        {
            fixture.Repository.Tags.Remove("1.0.0");
            fixture.ApplyTag("1.0.0");
        }
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);
    }

    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.0.2")]
    [TestCase(true, "release/0.0.2")]
    [TestCase(false, "release/0.1.0")]
    [TestCase(true, "release/0.1.0")]
    [TestCase(false, "release/0.1.1")]
    [TestCase(true, "release/0.1.1")]
    [TestCase(false, "release/0.2.0")]
    [TestCase(true, "release/0.2.0")]
    [TestCase(false, "release/1.0.0")]
    [TestCase(true, "release/1.0.0")]
    [TestCase(false, "release/1.0.1")]
    [TestCase(true, "release/1.0.1")]
    public void EnsureReleaseAndFeatureBranchWithIncrementMajorOnMainAndPatchOnReleaseBranch(
        bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Major)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Patch)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-foo.1+3", configuration);

        fixture.ApplyTag("1.0.1-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-foo.2+1", configuration);

        fixture.Checkout(releaseBranch);
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo(releaseBranch, removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("1.0.1-1+6", configuration);
        }
        else
        {
            // ❔ expected: "1.0.1-1+6"
            fixture.AssertFullSemver("2.0.0-1+6", configuration);
        }

        if (!useTrunkBased) fixture.ApplyTag("1.0.1");
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);
    }

    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.0.2")]
    [TestCase(true, "release/0.0.2")]
    [TestCase(false, "release/0.1.0")]
    [TestCase(true, "release/0.1.0")]
    [TestCase(false, "release/0.1.1")]
    [TestCase(true, "release/0.1.1")]
    [TestCase(false, "release/0.2.0")]
    [TestCase(true, "release/0.2.0")]
    [TestCase(false, "release/1.0.0")]
    [TestCase(true, "release/1.0.0")]
    [TestCase(false, "release/1.0.1")]
    [TestCase(true, "release/1.0.1")]
    [TestCase(false, "release/1.0.2")]
    [TestCase(true, "release/1.0.2")]
    [TestCase(false, "release/1.1.0")]
    [TestCase(true, "release/1.1.0")]
    public void EnsureReleaseAndFeatureBranchWithIncrementMajorOnMainAndMinorOnReleaseBranch(
        bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Major)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Minor)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-foo.1+3", configuration);

        fixture.ApplyTag("1.1.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-foo.2+1", configuration);

        fixture.Checkout(releaseBranch);
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo(releaseBranch, removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        if (useTrunkBased)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("1.1.0-1+6", configuration);
        }
        else
        {
            // ❔ expected: "1.1.0-1+6"
            fixture.AssertFullSemver("2.0.0-1+6", configuration);
        }

        if (!useTrunkBased) fixture.ApplyTag("1.1.0");
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);
    }

    [TestCase(false, "release/next")]
    [TestCase(true, "release/next")]
    [TestCase(false, "release/0.0.0")]
    [TestCase(true, "release/0.0.0")]
    [TestCase(false, "release/0.0.1")]
    [TestCase(true, "release/0.0.1")]
    [TestCase(false, "release/0.0.2")]
    [TestCase(true, "release/0.0.2")]
    [TestCase(false, "release/0.1.0")]
    [TestCase(true, "release/0.1.0")]
    [TestCase(false, "release/0.1.1")]
    [TestCase(true, "release/0.1.1")]
    [TestCase(false, "release/0.2.0")]
    [TestCase(true, "release/0.2.0")]
    [TestCase(false, "release/1.0.0")]
    [TestCase(true, "release/1.0.0")]
    [TestCase(false, "release/1.0.1")]
    [TestCase(true, "release/1.0.1")]
    [TestCase(false, "release/1.0.2")]
    [TestCase(true, "release/1.0.2")]
    [TestCase(false, "release/1.1.0")]
    [TestCase(true, "release/1.1.0")]
    [TestCase(false, "release/1.1.1")]
    [TestCase(true, "release/1.1.1")]
    [TestCase(false, "release/2.0.0")]
    [TestCase(true, "release/2.0.0")]
    public void EnsureReleaseAndFeatureBranchWithIncrementMajorOnMainAndMajorOnReleaseBranch(
        bool useTrunkBased, string releaseBranch)
    {
        var builder = useTrunkBased
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.TrunkBased)
            : configurationBuilder;
        var configuration = builder
            .WithIncrement(IncrementStrategy.Major)
            .WithBranch("main", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
                .WithIncrement(IncrementStrategy.Major)
            ).WithBranch("release", _ => _
                .WithIncrement(IncrementStrategy.Major)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).WithBranch("feature", _ => _
                .WithIncrement(IncrementStrategy.Inherit)
                .WithDeploymentMode(DeploymentMode.ManualDeployment)
            ).Build();

        using var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("A");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-1+1", configuration);

        if (!useTrunkBased) fixture.ApplyTag("1.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+1", configuration);

        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+1", configuration);

        fixture.MakeACommit("C");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+2", configuration);

        fixture.MakeACommit("D");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.1+3", configuration);

        fixture.ApplyTag("2.0.0-foo.1");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.2+0", configuration);

        fixture.MakeACommit("E");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-foo.2+1", configuration);

        fixture.Checkout(releaseBranch);
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-PullRequest2.5", configuration);

        fixture.Checkout("feature/foo");
        fixture.MergeTo(releaseBranch, removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+5", configuration);

        fixture.MergeTo("main", removeBranchAfterMerging: true);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+6", configuration);

        if (!useTrunkBased) fixture.ApplyTag("2.0.0");
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-1+1", configuration);
    }
}
