using GitVersion.Agents;
using GitVersion.Core.Tests;
using GitVersion.Helpers;
using GitVersion.MsBuild.Tests.Mocks;

namespace GitVersion.MsBuild.Tests.Helpers;

public class MsBuildTaskFixture
{
    private readonly RepositoryFixtureBase fixture;
    private KeyValuePair<string, string?>[]? environmentVariables;

    public MsBuildTaskFixture(RepositoryFixtureBase fixture) => this.fixture = fixture;

    public void WithEnv(params KeyValuePair<string, string?>[] envs) => this.environmentVariables = envs;

    public MsBuildTaskFixtureResult<T> Execute<T>(T task) where T : GitVersionTaskBase =>
        UsingEnv(() =>
        {
            var buildEngine = new MockEngine();

            task.BuildEngine = buildEngine;

            var versionFile = PathHelper.Combine(task.SolutionDirectory, "gitversion.json");
            this.fixture.WriteVersionVariables(versionFile);

            task.VersionFile = versionFile;

            var result = task.Execute();

            return new MsBuildTaskFixtureResult<T>(this.fixture)
            {
                Success = result,
                Task = task,
                Errors = buildEngine.Errors,
                Warnings = buildEngine.Warnings,
                Messages = buildEngine.Messages,
                Log = buildEngine.Log
            };
        });

    private T UsingEnv<T>(Func<T> func)
    {
        ResetEnvironment();
        SetEnvironmentVariables(this.environmentVariables);

        try
        {
            return func();
        }
        finally
        {
            ResetEnvironment();
        }
    }

    private static void ResetEnvironment()
    {
        var environmentalVariables = new Dictionary<string, string?>
        {
            { TeamCity.EnvironmentVariableName, null },
            { AppVeyor.EnvironmentVariableName, null },
            { TravisCi.EnvironmentVariableName, null },
            { Jenkins.EnvironmentVariableName, null },
            { AzurePipelines.EnvironmentVariableName, null },
            { GitHubActions.EnvironmentVariableName, null },
            { SpaceAutomation.EnvironmentVariableName, null }
        };

        SetEnvironmentVariables(environmentalVariables.ToArray());
    }

    private static void SetEnvironmentVariables(KeyValuePair<string, string?>[]? envs)
    {
        if (envs == null) return;
        foreach (var (key, value) in envs)
        {
            SysEnv.SetEnvironmentVariable(key, value);
        }
    }
}
