using GitVersion.OutputVariables;

namespace GitVersion.App.Tests;

[TestFixture]
public class TelemetryContextEnricherTests
{
    [Test]
    public void EnrichMapsSupportedContinuousIntegrationProviders()
    {
        var enricher = new TelemetryContextEnricher(new GitHubActions(), new TestEnvironment());

        var telemetry = enricher.Enrich(CreateTelemetry());

        telemetry.ContinuousIntegrationProvider.ShouldBe("github-actions");
        telemetry.InvocationSource.ShouldBe(TelemetryContextValues.Direct);
    }

    [Test]
    public void EnrichDetectsGitVersionMsBuildInvocations()
    {
        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable(TelemetryContextValues.InternalCallerEnvironmentVariable, "GitVersion.MsBuild");
        var enricher = new TelemetryContextEnricher(new LocalBuild(), environment);

        var telemetry = enricher.Enrich(CreateTelemetry());

        telemetry.ContinuousIntegrationProvider.ShouldBe(TelemetryContextValues.Unknown);
        telemetry.InvocationSource.ShouldBe(TelemetryContextValues.GitVersionMsBuild);
    }

    [Test]
    public void EnrichDefaultsInvocationSourceToDirectForUnsupportedContext()
    {
        var enricher = new TelemetryContextEnricher(new UnsupportedBuildAgent(), new TestEnvironment());

        var telemetry = enricher.Enrich(CreateTelemetry());

        telemetry.ContinuousIntegrationProvider.ShouldBe(TelemetryContextValues.Unknown);
        telemetry.InvocationSource.ShouldBe(TelemetryContextValues.Direct);
    }

    private static CommandLineTelemetry CreateTelemetry() => new(
        "1.2.3",
        nameof(ArgumentParser),
        TelemetryContextValues.Unknown,
        TelemetryContextValues.Direct,
        "gitversion",
        null,
        []);

    private sealed class TestEnvironment : IEnvironment
    {
        private readonly Dictionary<string, string?> variables = new(StringComparer.Ordinal);

        public string? GetEnvironmentVariable(string variableName) =>
            this.variables.TryGetValue(variableName, out var value) ? value : null;

        public void SetEnvironmentVariable(string variableName, string? value)
        {
            if (value == null)
            {
                this.variables.Remove(variableName);
                return;
            }

            this.variables[variableName] = value;
        }
    }

    private sealed class GitHubActions : GitVersion.Agents.ICurrentBuildAgent
    {
        public bool IsDefault => false;
        public bool CanApplyToCurrentContext() => true;
        public string? GetCurrentBranch(bool usingDynamicRepos) => null;
        public bool PreventFetch() => true;
        public bool ShouldCleanUpRemotes() => false;
        public void WriteIntegration(Action<string?> writer, GitVersionVariables variables, bool updateBuildNumber = true)
        {
        }
    }

    private sealed class LocalBuild : GitVersion.Agents.ICurrentBuildAgent
    {
        public bool IsDefault => true;
        public bool CanApplyToCurrentContext() => true;
        public string? GetCurrentBranch(bool usingDynamicRepos) => null;
        public bool PreventFetch() => true;
        public bool ShouldCleanUpRemotes() => false;
        public void WriteIntegration(Action<string?> writer, GitVersionVariables variables, bool updateBuildNumber = true)
        {
        }
    }

    private sealed class UnsupportedBuildAgent : GitVersion.Agents.ICurrentBuildAgent
    {
        public bool IsDefault => false;
        public bool CanApplyToCurrentContext() => true;
        public string? GetCurrentBranch(bool usingDynamicRepos) => null;
        public bool PreventFetch() => true;
        public bool ShouldCleanUpRemotes() => false;
        public void WriteIntegration(Action<string?> writer, GitVersionVariables variables, bool updateBuildNumber = true)
        {
        }
    }
}
