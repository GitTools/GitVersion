using GitVersion.Configuration;
using GitVersion.Testing.Extensions;
using GitVersion.VersionCalculation;

namespace GitVersion.Tests.IntegrationTests;

[TestFixture]
public class VersionBumpOverrideScenarios
{
    [TestCase(
        Workflow.TrunkBased,
        "message one =semver: none",
        "message two =semver: none",
        "message three =semver: none",
        "1.0.0-3"
    )]
    [TestCase(
        Workflow.TrunkBased,
        "message one +semver: minor",
        "message two =semver: none",
        "message three =semver: none",
        "1.1.0-3"
    )]
    [TestCase(
        Workflow.TrunkBased,
        "message one =semver: none",
        "message two +semver: minor",
        "message three =semver: none",
        "1.1.0-2"
    )]
    [TestCase(
        Workflow.TrunkBased,
        "message one =semver: none",
        "message two =semver: none",
        "message three +semver: minor",
        "1.1.0-1"
    )]
    [TestCase(
        Workflow.GitFlow,
        "message one =semver: none",
        "message two =semver: none",
        "message three =semver: none",
        "1.0.0-3"
    )]
    [TestCase(
        Workflow.GitFlow,
        "message one +semver: minor",
        "message two =semver: none",
        "message three =semver: none",
        "1.0.0-3"
    )]
    [TestCase(
        Workflow.GitFlow,
        "message one =semver: none",
        "message two +semver: minor",
        "message three =semver: none",
        "1.0.0-3"
    )]
    [TestCase(
        Workflow.GitFlow,
        "message one =semver: none",
        "message two =semver: none",
        "message three +semver: minor",
        "1.1.0-3"
    )]
    public void VersionBumpOverrideResetsTheCalculatedIncrement(
        Workflow workflow, string firstMessage, string secondMessage, string thirdMessage, string expectedVersion)
    {
        var configuration = BuildConfiguration(workflow, IncrementStrategy.Patch);

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit(firstMessage);
        fixture.MakeACommit(secondMessage);
        fixture.MakeACommit(thirdMessage);

        fixture.AssertFullSemver(expectedVersion, configuration);
    }

    [TestCase(Workflow.TrunkBased, "=semver: none", "1.0.0-1")]
    [TestCase(Workflow.TrunkBased, "=semver: patch", "1.0.1-1")]
    [TestCase(Workflow.TrunkBased, "=semver: minor", "1.1.0-1")]
    [TestCase(Workflow.TrunkBased, "=semver: major", "2.0.0-1")]
    [TestCase(Workflow.GitFlow, "=semver: none", "1.0.0-1")]
    [TestCase(Workflow.GitFlow, "=semver: patch", "1.0.1-1")]
    [TestCase(Workflow.GitFlow, "=semver: minor", "1.1.0-1")]
    [TestCase(Workflow.GitFlow, "=semver: major", "2.0.0-1")]
    [TestCase(Workflow.GitHubFlow, "=semver: none", "1.0.0-1")]
    [TestCase(Workflow.GitHubFlow, "=semver: patch", "1.0.1-1")]
    [TestCase(Workflow.GitHubFlow, "=semver: minor", "1.1.0-1")]
    [TestCase(Workflow.GitHubFlow, "=semver: major", "2.0.0-1")]
    public void VersionBumpOverrideCanLowerOrRaiseTheConfiguredIncrement(
        Workflow workflow, string commitMessage, string expectedVersion)
    {
        var configuration = BuildConfiguration(workflow, IncrementStrategy.Major);

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit(commitMessage);

        fixture.AssertFullSemver(expectedVersion, configuration);
    }

    [TestCase(Workflow.TrunkBased)]
    [TestCase(Workflow.GitFlow)]
    [TestCase(Workflow.GitHubFlow)]
    public void VersionBumpOverrideTakesPrecedenceWithinACommit(Workflow workflow)
    {
        var configuration = BuildConfiguration(workflow, IncrementStrategy.Patch);

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit("+semver: major =semver: patch");

        fixture.AssertFullSemver("1.0.1-1", configuration);
    }

    [TestCase(Workflow.TrunkBased)]
    [TestCase(Workflow.GitFlow)]
    [TestCase(Workflow.GitHubFlow)]
    public void VersionBumpOverrideUsesTheConfiguredPattern(Workflow workflow)
    {
        const string pattern = @"bump-override:\s?(?<increment>none|patch|minor|major)";
        var configuration = BuildConfiguration(
            workflow, IncrementStrategy.Patch, overrideVersionBumpMessage: pattern);

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit("bump-override: none");

        fixture.AssertFullSemver("1.0.0-1", configuration);
    }

    [TestCase(Workflow.TrunkBased)]
    [TestCase(Workflow.GitFlow)]
    [TestCase(Workflow.GitHubFlow)]
    public void VersionBumpOverrideHonorsDisabledCommitMessageIncrementing(Workflow workflow)
    {
        var configuration = BuildConfiguration(
            workflow, IncrementStrategy.Patch, CommitMessageIncrementMode.Disabled);

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        fixture.MakeACommit("=semver: none");

        fixture.AssertFullSemver("1.0.1-1", configuration);
    }

    private static IGitVersionConfiguration BuildConfiguration(
        Workflow workflow,
        IncrementStrategy increment,
        CommitMessageIncrementMode commitMessageIncrementing = CommitMessageIncrementMode.Enabled,
        string? overrideVersionBumpMessage = null) => workflow switch
        {
            Workflow.TrunkBased => BuildConfiguration(
                TrunkBasedConfigurationBuilder.New, increment, commitMessageIncrementing, overrideVersionBumpMessage),
            Workflow.GitFlow => BuildConfiguration(
                GitFlowConfigurationBuilder.New, increment, commitMessageIncrementing, overrideVersionBumpMessage),
            Workflow.GitHubFlow => BuildConfiguration(
                GitHubFlowConfigurationBuilder.New, increment, commitMessageIncrementing, overrideVersionBumpMessage),
            _ => throw new ArgumentOutOfRangeException(nameof(workflow), workflow, null)
        };

    private static IGitVersionConfiguration BuildConfiguration<TConfigurationBuilder>(
        ConfigurationBuilderBase<TConfigurationBuilder> builder,
        IncrementStrategy increment,
        CommitMessageIncrementMode commitMessageIncrementing,
        string? overrideVersionBumpMessage)
        where TConfigurationBuilder : ConfigurationBuilderBase<TConfigurationBuilder>
    {
        if (overrideVersionBumpMessage is not null)
        {
            builder.WithOverrideVersionBumpMessage(overrideVersionBumpMessage);
        }

        return builder.WithBranch("main", branchBuilder => branchBuilder
            .WithDeploymentMode(DeploymentMode.ContinuousDelivery)
            .WithIncrement(increment)
            .WithCommitMessageIncrementing(commitMessageIncrementing)
        ).Build();
    }

    public enum Workflow
    {
        TrunkBased,
        GitFlow,
        GitHubFlow
    }
}
