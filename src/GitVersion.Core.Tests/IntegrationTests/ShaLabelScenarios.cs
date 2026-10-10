using GitVersion.Configuration;
using GitVersion.Git;
using GitVersion.Testing.Extensions;
using GitVersion.VersionCalculation;

namespace GitVersion.Tests.IntegrationTests;

[TestFixture]
[NonParallelizable]
public class ShaLabelScenarios : TestBase
{
    [TestCase("managed", "ShortSha")]
    [TestCase("libgit2", "ShortSha")]
    [TestCase("managed", "Sha")]
    [TestCase("libgit2", "Sha")]
    public void FeatureLabelUsesCurrentCommitAndChangesOnNextCommit(string backend, string placeholder)
    {
        using var backendScope = new BackendScope(backend);
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        var sourceSha = fixture.Repository.Head.Tip.Sha;
        fixture.BranchTo("feature/topic");
        fixture.MakeACommit();
        var firstSha = fixture.Repository.Head.Tip.Sha;
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("feature", b => b.WithLabel($"ci.{{{placeholder}}}"))
            .Build();

        var first = fixture.GetVersion(configuration);

        first.Sha.ShouldBe(firstSha);
        first.ShortSha.ShouldBe(firstSha[..7]);
        first.VersionSourceSha.ShouldBe(sourceSha);
        first.PreReleaseLabelName.ShouldBe("ci." + (placeholder == "Sha" ? firstSha : firstSha[..7]));
        first.FullSemVer.ShouldBe($"1.0.1-{first.PreReleaseLabelName}.1+1");

        fixture.MakeACommit();
        var secondSha = fixture.Repository.Head.Tip.Sha;
        var second = fixture.GetVersion(configuration);

        second.Sha.ShouldBe(secondSha);
        second.ShortSha.ShouldBe(secondSha[..7]);
        second.PreReleaseLabelName.ShouldBe("ci." + (placeholder == "Sha" ? secondSha : secondSha[..7]));
        second.PreReleaseLabelName.ShouldNotBe(first.PreReleaseLabelName);
        second.FullSemVer.ShouldBe($"1.0.1-{second.PreReleaseLabelName}.1+2");

        // Explicit historical calculation must use the selected commit even when HEAD has advanced.
        var historical = fixture.GetVersion(configuration, commitId: firstSha);
        historical.FullSemVer.ShouldBe(first.FullSemVer);
        historical.Sha.ShouldBe(firstSha);
    }

    [TestCase("managed")]
    [TestCase("libgit2")]
    public void MainlineMergedHistoryUsesCalculationCommitForLabel(string backend)
    {
        using var backendScope = new BackendScope(backend);
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        var sourceSha = fixture.Repository.Head.Tip.Sha;
        fixture.BranchTo("feature/outer");
        fixture.MakeACommit("outer");
        fixture.BranchTo("feature/inner");
        fixture.MakeACommit("inner");
        fixture.MergeTo("feature/outer");
        fixture.MergeTo("main");
        var currentSha = fixture.Repository.Head.Tip.Sha;
        var configuration = GitFlowConfigurationBuilder.New
            .WithVersionStrategy(VersionStrategies.Mainline)
            .WithLabel("ci.{ShortSha}")
            .WithBranch("main", b => b.WithLabel("ci.{ShortSha}").WithDeploymentMode(DeploymentMode.ManualDeployment))
            .WithBranch("feature", b => b.WithLabel("ci.{ShortSha}").WithDeploymentMode(DeploymentMode.ManualDeployment))
            .Build();

        var actual = fixture.GetVersion(configuration);

        actual.Sha.ShouldBe(currentSha);
        actual.PreReleaseLabelName.ShouldBe("ci." + currentSha[..7]);
        actual.PreReleaseLabelName.ShouldNotBe("ci." + sourceSha[..7]);
        // A fixed literal label must produce identical version selection and prerelease counting.
        var literalConfiguration = GitFlowConfigurationBuilder.New
            .WithVersionStrategy(VersionStrategies.Mainline)
            .WithLabel("ci." + currentSha[..7])
            .WithBranch("main", b => b.WithLabel("ci." + currentSha[..7]).WithDeploymentMode(DeploymentMode.ManualDeployment))
            .WithBranch("feature", b => b.WithLabel("ci." + currentSha[..7]).WithDeploymentMode(DeploymentMode.ManualDeployment))
            .Build();
        actual.FullSemVer.ShouldBe(fixture.GetVersion(literalConfiguration).FullSemVer);
    }

    [TestCase("managed")]
    [TestCase("libgit2")]
    public void TaggedCurrentCommitPreservesStableVersionWithShaLabel(string backend)
    {
        using var backendScope = new BackendScope(backend);
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("main", b => b.WithLabel("ci.{ShortSha}"))
            .Build();

        var actual = fixture.GetVersion(configuration);

        actual.FullSemVer.ShouldBe("1.0.0");
        actual.PreReleaseLabel.ShouldBeEmpty();
        actual.Sha.ShouldBe(fixture.Repository.Head.Tip.Sha);
    }

    [TestCase("managed", "ShortSha")]
    [TestCase("libgit2", "ShortSha")]
    [TestCase("managed", "Sha")]
    [TestCase("libgit2", "Sha")]
    public void MergedSourceLabelUsesCalculationCommitForHistoryReset(string backend, string placeholder)
    {
        using var backendScope = new BackendScope(backend);
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.BranchTo("feature/topic");
        fixture.MakeACommit("source change +semver: major");
        var sourceSha = fixture.Repository.Head.Tip.Sha;
        fixture.MergeTo("main");
        var currentSha = fixture.Repository.Head.Tip.Sha;
        var label = "ci-sha" + (placeholder == "Sha" ? currentSha : currentSha[..7]);
        fixture.Repository.Tags.Add($"0.9.0-{label}.1", sourceSha);

        IGitVersionConfiguration Configuration(string sourceLabel) => GitFlowConfigurationBuilder.New
            .WithBranch("main", b => b
                .WithIncrement(IncrementStrategy.Patch)
                .WithPreventIncrementOfMergedBranch(true))
            .WithBranch("feature", b => b
                .WithLabel(sourceLabel)
                .WithIncrement(IncrementStrategy.Patch)
                .WithPreventIncrementWhenBranchMerged(false))
            .Build();

        var literal = fixture.GetVersion(Configuration(label));
        var actual = fixture.GetVersion(Configuration($"ci-sha{{{placeholder}}}"));

        currentSha.ShouldNotBe(sourceSha);
        literal.FullSemVer.ShouldBe("1.0.1-2");
        actual.FullSemVer.ShouldBe(literal.FullSemVer);

        fixture.MakeACommit();
        var historical = fixture.GetVersion(
            Configuration($"ci-sha{{{placeholder}}}"), commitId: currentSha);
        historical.Sha.ShouldBe(currentSha);
        historical.FullSemVer.ShouldBe(actual.FullSemVer);
    }

    private sealed class BackendScope : IDisposable
    {
        private readonly string? original = System.Environment.GetEnvironmentVariable(GitBackendSelector.EnvironmentVariableName);

        public BackendScope(string backend) => System.Environment.SetEnvironmentVariable(GitBackendSelector.EnvironmentVariableName, backend);

        public void Dispose() => System.Environment.SetEnvironmentVariable(GitBackendSelector.EnvironmentVariableName, this.original);
    }
}
