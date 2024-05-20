using GitVersion.Configuration;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class AlignGitHubFlowWithMainlineVersionStrategy
{
    private static GitHubFlowConfigurationBuilder configurationBuilder => GitHubFlowConfigurationBuilder.New;

    /// <summary>
    /// GitHubFlow - Feature branch (Increment inherit on main and inherit on feature)
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void EnsureFeatureWithIncrementInheritOnMainAndInheritOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useMainline) fixture.ApplyTag("2.0.0");
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
    public void EnsureFeatureWithIncrementInheritOnMainAndNoneOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useMainline) fixture.ApplyTag("2.0.0");
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

        if (useMainline)
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
    public void EnsureFeatureWithIncrementInheritOnMainAndPatchOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useMainline) fixture.ApplyTag("2.0.0");
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

        if (useMainline)
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
    public void EnsureFeatureWithIncrementInheritOnMainAndMinorOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useMainline) fixture.ApplyTag("2.0.0");
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

        if (useMainline)
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
    public void EnsureFeatureWithIncrementInheritOnMainAndMajorOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useMainline) fixture.ApplyTag("2.0.0");
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
    public void EnsureFeatureWithIncrementNoneOnMainAndInheritOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.0");
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

        if (useMainline)
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
    public void EnsureFeatureWithIncrementNoneOnMainAndNoneOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.0");
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

        if (useMainline)
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
    public void EnsureFeatureWithIncrementNoneOnMainAndPatchOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.0");
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
    public void EnsureFeatureWithIncrementNoneOnMainAndMinorOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.0");
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
    public void EnsureFeatureWithIncrementNoneOnMainAndMajorOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.0");
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
    public void EnsureFeatureWithIncrementPatchOnMainAndInheritOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.1");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-1+1", configuration);

        if (!useMainline) fixture.ApplyTag("0.0.2");
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
    public void EnsureFeatureWithIncrementPatchOnMainAndNoneOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.1");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-1+1", configuration);

        if (!useMainline) fixture.ApplyTag("0.0.2");
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

        if (useMainline)
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
    public void EnsureFeatureWithIncrementPatchOnMainAndPatchOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.1");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-1+1", configuration);

        if (!useMainline) fixture.ApplyTag("0.0.2");
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
    public void EnsureFeatureWithIncrementPatchOnMainAndMinorOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.1");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-1+1", configuration);

        if (!useMainline) fixture.ApplyTag("0.0.2");
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
    public void EnsureFeatureWithIncrementPatchOnMainAndMajorOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.1");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2-1+1", configuration);

        if (!useMainline) fixture.ApplyTag("0.0.2");
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
    public void EnsureFeatureWithIncrementMinorOnMainAndInheritOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-1+1", configuration);

        if (!useMainline) fixture.ApplyTag("0.2.0");
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
    public void EnsureFeatureWithIncrementMinorOnMainAndNoneOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-1+1", configuration);

        if (!useMainline) fixture.ApplyTag("0.2.0");
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

        if (useMainline)
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
    public void EnsureFeatureWithIncrementMinorOnMainAndPatchOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-1+1", configuration);

        if (!useMainline) fixture.ApplyTag("0.2.0");
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

        if (useMainline)
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
    public void EnsureFeatureWithIncrementMinorOnMainAndMinorOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-1+1", configuration);

        if (!useMainline) fixture.ApplyTag("0.2.0");
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
    public void EnsureFeatureWithIncrementMinorOnMainAndMajorOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.2.0-1+1", configuration);

        if (!useMainline) fixture.ApplyTag("0.2.0");
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
    public void EnsureFeatureWithIncrementMajorOnMainAndInheritOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useMainline) fixture.ApplyTag("2.0.0");
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
    public void EnsureFeatureWithIncrementMajorOnMainAndNoneOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useMainline) fixture.ApplyTag("2.0.0");
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

        if (useMainline)
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
    public void EnsureFeatureWithIncrementMajorOnMainAndPatchOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useMainline) fixture.ApplyTag("2.0.0");
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

        if (useMainline)
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
    public void EnsureFeatureWithIncrementMajorOnMainAndMinorOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useMainline) fixture.ApplyTag("2.0.0");
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

        if (useMainline)
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
    public void EnsureFeatureWithIncrementMajorOnMainAndMajorOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-1+1", configuration);

        if (!useMainline) fixture.ApplyTag("2.0.0");
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
    public void EnsureMergeMainToFeatureWithIncrementInheritOnMainAndInheritOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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
    public void EnsureMergeMainToFeatureWithIncrementInheritOnMainAndNoneOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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

        if (useMainline)
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
    public void EnsureMergeMainToFeatureWithIncrementInheritOnMainAndPatchOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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

        if (useMainline)
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
    public void EnsureMergeMainToFeatureWithIncrementInheritOnMainAndMinorOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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

        if (useMainline)
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
    public void EnsureMergeMainToFeatureWithIncrementInheritOnMainAndMajorOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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
    public void EnsureMergeMainToFeatureWithIncrementNoneOnMainAndInheritOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-2+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-1+1", configuration);
        }

        if (!useMainline)
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

        if (useMainline)
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
    public void EnsureMergeMainToFeatureWithIncrementNoneOnMainAndNoneOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-2+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-1+1", configuration);
        }

        if (!useMainline)
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

        if (useMainline)
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
    public void EnsureMergeMainToFeatureWithIncrementNoneOnMainAndPatchOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-2+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-1+1", configuration);
        }

        if (!useMainline) fixture.ApplyTag("0.0.1");
        fixture.MergeTo("feature/foo");

        if (useMainline)
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

        if (useMainline)
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
    public void EnsureMergeMainToFeatureWithIncrementNoneOnMainAndMinorOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-2+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-1+1", configuration);
        }

        if (!useMainline) fixture.ApplyTag("0.0.1");
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
    public void EnsureMergeMainToFeatureWithIncrementNoneOnMainAndMajorOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-2+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-1+1", configuration);
        }

        if (!useMainline) fixture.ApplyTag("0.0.1");
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
    public void EnsureMergeMainToFeatureWithIncrementPatchOnMainAndInheritOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.1");
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

        if (!useMainline) fixture.ApplyTag("0.0.2");
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
    public void EnsureMergeMainToFeatureWithIncrementPatchOnMainAndNoneOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.1");
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

        if (!useMainline) fixture.ApplyTag("0.0.2");
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

        if (useMainline)
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
    public void EnsureMergeMainToFeatureWithIncrementPatchOnMainAndPatchOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.1");
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

        if (!useMainline) fixture.ApplyTag("0.0.2");
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
    public void EnsureMergeMainToFeatureWithIncrementPatchOnMainAndMinorOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.1");
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

        if (!useMainline) fixture.ApplyTag("0.0.2");
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
    public void EnsureMergeMainToFeatureWithIncrementPatchOnMainAndMajorOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.1");
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

        if (!useMainline) fixture.ApplyTag("0.0.2");
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
    public void EnsureMergeMainToFeatureWithIncrementMinorOnMainAndInheritOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
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

        if (!useMainline) fixture.ApplyTag("0.2.0");
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
    public void EnsureMergeMainToFeatureWithIncrementMinorOnMainAndNoneOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
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

        if (!useMainline) fixture.ApplyTag("0.2.0");
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

        if (useMainline)
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
    public void EnsureMergeMainToFeatureWithIncrementMinorOnMainAndPatchOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
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

        if (!useMainline) fixture.ApplyTag("0.2.0");
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

        if (useMainline)
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
    public void EnsureMergeMainToFeatureWithIncrementMinorOnMainAndMinorOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
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

        if (!useMainline) fixture.ApplyTag("0.2.0");
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
    public void EnsureMergeMainToFeatureWithIncrementMinorOnMainAndMajorOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
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

        if (!useMainline) fixture.ApplyTag("0.2.0");
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
    public void EnsureMergeMainToFeatureWithIncrementMajorOnMainAndInheritOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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
    public void EnsureMergeMainToFeatureWithIncrementMajorOnMainAndNoneOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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

        if (useMainline)
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
    public void EnsureMergeMainToFeatureWithIncrementMajorOnMainAndPatchOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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

        if (useMainline)
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
    public void EnsureMergeMainToFeatureWithIncrementMajorOnMainAndMinorOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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

        if (useMainline)
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
    public void EnsureMergeMainToFeatureWithIncrementMajorOnMainAndMajorOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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
    public void EnsurePullRequestWithIncrementInheritOnMainAndInheritOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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
    public void EnsurePullRequestWithIncrementInheritOnMainAndNoneOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useMainline)
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

        if (useMainline)
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
    public void EnsurePullRequestWithIncrementInheritOnMainAndPatchOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useMainline)
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

        if (useMainline)
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
    public void EnsurePullRequestWithIncrementInheritOnMainAndMinorOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useMainline)
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

        if (useMainline)
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
    public void EnsurePullRequestWithIncrementInheritOnMainAndMajorOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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
    public void EnsurePullRequestWithIncrementNoneOnMainAndInheritOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.0");
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

        if (useMainline)
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
    public void EnsurePullRequestWithIncrementNoneOnMainAndNoneOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.0");
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

        if (useMainline)
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
    public void EnsurePullRequestWithIncrementNoneOnMainAndPatchOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.0");
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

        if (useMainline)
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
    public void EnsurePullRequestWithIncrementNoneOnMainAndMinorOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.0");
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

        if (useMainline)
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
    public void EnsurePullRequestWithIncrementNoneOnMainAndMajorOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.0");
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

        if (useMainline)
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
    public void EnsurePullRequestWithIncrementPatchOnMainAndInheritOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.1");
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
    public void EnsurePullRequestWithIncrementPatchOnMainAndNoneOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.1");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useMainline)
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

        if (useMainline)
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
    public void EnsurePullRequestWithIncrementPatchOnMainAndPatchOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.1");
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
    public void EnsurePullRequestWithIncrementPatchOnMainAndMinorOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.1");
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

        if (useMainline)
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
    public void EnsurePullRequestWithIncrementPatchOnMainAndMajorOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.1");
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

        if (useMainline)
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
    public void EnsurePullRequestWithIncrementMinorOnMainAndInheritOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
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
    public void EnsurePullRequestWithIncrementMinorOnMainAndNoneOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useMainline)
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

        if (useMainline)
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
    public void EnsurePullRequestWithIncrementMinorOnMainAndPatchOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.1-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.1-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useMainline)
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

        if (useMainline)
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
    public void EnsurePullRequestWithIncrementMinorOnMainAndMinorOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
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
    public void EnsurePullRequestWithIncrementMinorOnMainAndMajorOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
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

        if (useMainline)
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
    public void EnsurePullRequestWithIncrementMajorOnMainAndInheritOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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
    public void EnsurePullRequestWithIncrementMajorOnMainAndNoneOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useMainline)
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

        if (useMainline)
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
    public void EnsurePullRequestWithIncrementMajorOnMainAndPatchOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.1-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useMainline)
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

        if (useMainline)
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
    public void EnsurePullRequestWithIncrementMajorOnMainAndMinorOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
        fixture.BranchTo("feature/foo");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-foo.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.1.0-foo.1+1", configuration);

        fixture.Checkout("main");
        fixture.BranchTo("pull/2/merge");
        fixture.MergeNoFF("feature/foo");

        if (useMainline)
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

        if (useMainline)
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
    public void EnsurePullRequestWithIncrementMajorOnMainAndMajorOnFeatureBranch(bool useMainline)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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
    public void EnsureReleaseBranchWithIncrementInheritOnMain(bool useMainline, IncrementStrategy incrementOnReleaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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

        if (!useMainline) fixture.ApplyTag("3.0.0");
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
    public void EnsureReleaseBranchWithIncrementInheritOnMainAndInheritOnReleaseBranch(bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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

        if (!useMainline) fixture.ApplyTag("3.0.0");
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
    public void EnsureReleaseBranchWithIncrementInheritOnMainAndNoneOnReleaseBranch(bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("2.0.0-2+6", configuration);
        }
        else
        {
            // ❔ expected: "2.0.0-2+6"
            fixture.AssertFullSemver("3.0.0-1+6", configuration);
        }

        if (!useMainline)
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
    public void EnsureReleaseBranchWithIncrementInheritOnMainAndPatchOnReleaseBranch(bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("2.0.1-1+6", configuration);
        }
        else
        {
            // ❔ expected: "2.0.1-1+6"
            fixture.AssertFullSemver("3.0.0-1+6", configuration);
        }

        if (!useMainline) fixture.ApplyTag("2.0.1");
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
    public void EnsureReleaseBranchWithIncrementInheritOnMainAndMinorOnReleaseBranch(bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("2.1.0-1+6", configuration);
        }
        else
        {
            // ❔ expected: "2.1.0-1+6"
            fixture.AssertFullSemver("3.0.0-1+6", configuration);
        }

        if (!useMainline) fixture.ApplyTag("2.1.0");
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
    public void EnsureReleaseBranchWithIncrementInheritOnMainAndMajorOnReleaseBranch(bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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

        if (!useMainline) fixture.ApplyTag("3.0.0");
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
    public void EnsureReleaseBranchWithIncrementNoneOnMain(bool useMainline, IncrementStrategy incrementOnReleaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.0");
        fixture.BranchTo("release/2.0.0");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("2.0.0-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-2+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-1+1", configuration);
        }

        if (!useMainline)
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
        fixture.MakeACommit("G");

        if (useMainline)
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
    public void EnsureReleaseBranchWithIncrementNoneOnMainAndInheritOnReleaseBranch(bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-2+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-1+1", configuration);
        }

        if (!useMainline)
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

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-3+6", configuration);
        }
        else
        {
            // ❔ expected: "0.0.0-3+6"
            fixture.AssertFullSemver("0.0.0-1+6", configuration);
        }

        if (!useMainline)
        {
            fixture.Repository.Tags.Remove("0.0.0");
            fixture.ApplyTag("0.0.0");
        }
        fixture.MakeACommit("G");

        if (useMainline)
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
    public void EnsureReleaseBranchWithIncrementNoneOnMainAndNoneOnReleaseBranch(bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.0-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-2+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-1+1", configuration);
        }

        if (!useMainline)
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

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-3+6", configuration);
        }
        else
        {
            // ❔ expected: "0.0.0-3+6"
            fixture.AssertFullSemver("0.0.0-1+6", configuration);
        }

        if (!useMainline)
        {
            fixture.Repository.Tags.Remove("0.0.0");
            fixture.ApplyTag("0.0.0");
        }
        fixture.MakeACommit("G");

        if (useMainline)
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
    public void EnsureReleaseBranchWithIncrementNoneOnMainAndPatchOnReleaseBranch(bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-2+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-1+1", configuration);
        }

        if (!useMainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.1");
        fixture.MakeACommit("G");

        if (useMainline)
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
    public void EnsureReleaseBranchWithIncrementNoneOnMainAndMinorOnReleaseBranch(bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.1.0-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-2+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-1+1", configuration);
        }

        if (!useMainline)
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
        fixture.MakeACommit("G");

        if (useMainline)
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
    public void EnsureReleaseBranchWithIncrementNoneOnMainAndMajorOnReleaseBranch(bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.0");
        fixture.BranchTo(releaseBranch);

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+0", configuration);

        fixture.MakeACommit("B");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit("C");

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-2+1", configuration);
        }
        else
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-1+1", configuration);
        }

        if (!useMainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("G");

        if (useMainline)
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
    public void EnsureReleaseBranchWithIncrementPatchOnMain(bool useMainline, IncrementStrategy incrementOnReleaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.1");
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

        if (!useMainline) fixture.ApplyTag("0.0.2");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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
    public void EnsureReleaseBranchWithIncrementPatchOnMainAndInheritOnReleaseBranch(bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.1");
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

        if (!useMainline) fixture.ApplyTag("0.0.2");
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

        if (!useMainline) fixture.ApplyTag("0.0.3");
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
    public void EnsureReleaseBranchWithIncrementPatchOnMainAndNoneOnReleaseBranch(bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.1");
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

        if (!useMainline) fixture.ApplyTag("0.0.2");
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

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.2-2+6", configuration);
        }
        else
        {
            // ❔ expected: "0.0.2-2+6"
            fixture.AssertFullSemver("0.0.3-1+6", configuration);
        }

        if (!useMainline)
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
    public void EnsureReleaseBranchWithIncrementPatchOnMainAndPatchOnReleaseBranch(bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.1");
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

        if (!useMainline) fixture.ApplyTag("0.0.2");
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

        if (!useMainline) fixture.ApplyTag("0.0.3");
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
    public void EnsureReleaseBranchWithIncrementPatchOnMainAndMinorOnReleaseBranch(bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.1");
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

        if (!useMainline) fixture.ApplyTag("0.0.2");
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
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
    public void EnsureReleaseBranchWithIncrementPatchOnMainAndMajorOnReleaseBranch(bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.1");
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

        if (!useMainline) fixture.ApplyTag("0.0.2");
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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
    public void EnsureReleaseBranchWithIncrementMinorOnMain(bool useMainline, IncrementStrategy incrementOnReleaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
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

        if (!useMainline) fixture.ApplyTag("0.2.0");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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
    public void EnsureReleaseBranchWithIncrementMinorOnMainAndInheritOnReleaseBranch(bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
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

        if (!useMainline) fixture.ApplyTag("0.2.0");
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

        if (!useMainline) fixture.ApplyTag("0.3.0");
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
    public void EnsureReleaseBranchWithIncrementMinorOnMainAndNoneOnReleaseBranch(bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
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

        if (!useMainline) fixture.ApplyTag("0.2.0");
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

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.2.0-2+6", configuration);
        }
        else
        {
            // ❔ expected: "0.2.0-2+6"
            fixture.AssertFullSemver("0.3.0-1+6", configuration);
        }

        if (!useMainline)
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
    public void EnsureReleaseBranchWithIncrementMinorOnMainAndPatchOnReleaseBranch(bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
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

        if (!useMainline) fixture.ApplyTag("0.2.0");
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

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.2.1-1+6", configuration);
        }
        else
        {
            // ❔ expected: "0.2.1-1+6"
            fixture.AssertFullSemver("0.3.0-1+6", configuration);
        }

        if (!useMainline) fixture.ApplyTag("0.2.1");
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
    public void EnsureReleaseBranchWithIncrementMinorOnMainAndMinorOnReleaseBranch(bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
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

        if (!useMainline) fixture.ApplyTag("0.2.0");
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

        if (!useMainline) fixture.ApplyTag("0.3.0");
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
    public void EnsureReleaseBranchWithIncrementMinorOnMainAndMajorOnReleaseBranch(bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
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

        if (!useMainline) fixture.ApplyTag("0.2.0");
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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
    public void EnsureReleaseBranchWithIncrementMajorOnMain(bool useMainline, IncrementStrategy incrementOnReleaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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

        if (!useMainline) fixture.ApplyTag("3.0.0");
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
    public void EnsureReleaseBranchWithIncrementMajorOnMainAndInheritOnReleaseBranch(bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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

        if (!useMainline) fixture.ApplyTag("3.0.0");
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
    public void EnsureReleaseBranchWithIncrementMajorOnMainAndNoneOnReleaseBranch(bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("2.0.0-2+6", configuration);
        }
        else
        {
            // ❔ expected: "2.0.0-2+6"
            fixture.AssertFullSemver("3.0.0-1+6", configuration);
        }

        if (!useMainline)
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
    public void EnsureReleaseBranchWithIncrementMajorOnMainAndPatchOnReleaseBranch(bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("2.0.1-1+6", configuration);
        }
        else
        {
            // ❔ expected: "2.0.1-1+6"
            fixture.AssertFullSemver("3.0.0-1+6", configuration);
        }

        if (!useMainline) fixture.ApplyTag("2.0.1");
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
    public void EnsureReleaseBranchWithIncrementMajorOnMainAndMinorOnReleaseBranch(bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("2.1.0-1+6", configuration);
        }
        else
        {
            // ❔ expected: "2.1.0-1+6"
            fixture.AssertFullSemver("3.0.0-1+6", configuration);
        }

        if (!useMainline) fixture.ApplyTag("2.1.0");
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
    public void EnsureReleaseBranchWithIncrementMajorOnMainAndMajorOnReleaseBranch(bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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

        if (!useMainline) fixture.ApplyTag("3.0.0");
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
        bool useMainline, IncrementStrategy incrementOnReleaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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
        bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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
        bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("1.0.0-2+6", configuration);
        }
        else
        {
            // ❔ expected: "1.0.0-2+6"
            fixture.AssertFullSemver("2.0.0-1+6", configuration);
        }

        if (!useMainline)
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
        bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("1.0.1-1+6", configuration);
        }
        else
        {
            // ❔ expected: "1.0.1-1+6"
            fixture.AssertFullSemver("2.0.0-1+6", configuration);
        }

        if (!useMainline) fixture.ApplyTag("1.0.1");
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
        bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("1.1.0-1+6", configuration);
        }
        else
        {
            // ❔ expected: "1.1.0-1+6"
            fixture.AssertFullSemver("2.0.0-1+6", configuration);
        }

        if (!useMainline) fixture.ApplyTag("1.1.0");
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
        bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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
        bool useMainline, IncrementStrategy incrementOnReleaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.0");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
        fixture.MakeACommit("F");

        if (useMainline)
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
        bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.0");
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

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-2+6", configuration);
        }
        else
        {
            // ❔ expected: "0.0.0-2+6"
            fixture.AssertFullSemver("0.0.0-1+6", configuration);
        }

        if (!useMainline)
        {
            fixture.Repository.Tags.Remove("0.0.0");
            fixture.ApplyTag("0.0.0");
        }
        fixture.MakeACommit("F");

        if (useMainline)
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
        bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.0");
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

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.0-2+6", configuration);
        }
        else
        {
            // ❔ expected: "0.0.0-2+6"
            fixture.AssertFullSemver("0.0.0-1+6", configuration);
        }

        if (!useMainline)
        {
            fixture.Repository.Tags.Remove("0.0.0");
            fixture.ApplyTag("0.0.0");
        }
        fixture.MakeACommit("F");

        if (useMainline)
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
        bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.0");
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

        if (!useMainline) fixture.ApplyTag("0.0.1");
        fixture.MakeACommit("F");

        if (useMainline)
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
        bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.0");
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
        fixture.MakeACommit("F");

        if (useMainline)
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
        bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.0");
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("F");

        if (useMainline)
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
        bool useMainline, IncrementStrategy incrementOnReleaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.1");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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
        bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.1");
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

        if (!useMainline) fixture.ApplyTag("0.0.2");
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
        bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.1");
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

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.0.1-2+6", configuration);
        }
        else
        {
            // ❔ expected: "0.0.1-2+6"
            fixture.AssertFullSemver("0.0.2-1+6", configuration);
        }

        if (!useMainline)
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
        bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.1");
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

        if (!useMainline) fixture.ApplyTag("0.0.2");
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
        bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.1");
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
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
        bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.0.1");
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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
        bool useMainline, IncrementStrategy incrementOnReleaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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
        bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
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

        if (!useMainline) fixture.ApplyTag("0.2.0");
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
        bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
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

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.1.0-2+6", configuration);
        }
        else
        {
            // ❔ not expected
            fixture.AssertFullSemver("0.2.0-1+6", configuration);
        }

        if (!useMainline)
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
        bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
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

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("0.1.1-1+6", configuration);
        }
        else
        {
            // ❔ not expected
            fixture.AssertFullSemver("0.2.0-1+6", configuration);
        }

        if (!useMainline) fixture.ApplyTag("0.1.1");
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
        bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
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

        if (!useMainline) fixture.ApplyTag("0.2.0");
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
        bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("0.1.0");
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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
        bool useMainline, IncrementStrategy incrementOnReleaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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
        bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
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
        bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("1.0.0-2+6", configuration);
        }
        else
        {
            // ❔ expected: "1.0.0-2+6"
            fixture.AssertFullSemver("2.0.0-1+6", configuration);
        }

        if (!useMainline)
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
        bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("1.0.1-1+6", configuration);
        }
        else
        {
            // ❔ expected: "1.0.1-1+6"
            fixture.AssertFullSemver("2.0.0-1+6", configuration);
        }

        if (!useMainline) fixture.ApplyTag("1.0.1");
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
        bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (useMainline)
        {
            // ✅ succeeds as expected
            fixture.AssertFullSemver("1.1.0-1+6", configuration);
        }
        else
        {
            // ❔ expected: "1.1.0-1+6"
            fixture.AssertFullSemver("2.0.0-1+6", configuration);
        }

        if (!useMainline) fixture.ApplyTag("1.1.0");
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
        bool useMainline, string releaseBranch)
    {
        var builder = useMainline
            ? configurationBuilder.WithVersionStrategy(VersionStrategies.Mainline)
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

        if (!useMainline) fixture.ApplyTag("1.0.0");
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

        if (!useMainline) fixture.ApplyTag("2.0.0");
        fixture.MakeACommit("F");

        // ✅ succeeds as expected
        fixture.AssertFullSemver("3.0.0-1+1", configuration);
    }
}
