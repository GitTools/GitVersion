using GitVersion.Helpers;
using GitVersion.MsBuild.Tasks;
using GitVersion.MsBuild.Tests.Mocks;
using GitVersion.Tests;

namespace GitVersion.MsBuild.Tests.Helpers;

public class MsBuildTaskFixture(RepositoryFixtureBase fixture)
{
    private KeyValuePair<string, string?>[]? environmentVariables;

    public void WithEnv(params KeyValuePair<string, string?>[] envs) => this.environmentVariables = envs;

    public MsBuildTaskFixtureResult<T> Execute<T>(T task) where T : GitVersionTaskBase =>
        UsingEnv(() =>
        {
            var buildEngine = new MockEngine();

            task.BuildEngine = buildEngine;

            var versionFile = FileSystemHelper.Path.Combine(task.SolutionDirectory, "gitversion.json");
            fixture.WriteVersionVariables(versionFile);

            task.VersionFile = versionFile;

            var result = GitVersionTasks.Execute(task);

            return new MsBuildTaskFixtureResult<T>(fixture)
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
        // The task reads the process-wide environment to detect the build agent, so serialize the
        // whole set/execute/reset window against every other environment-sensitive fixture.
        using var environment = MsBuildProcessEnvironment.Enter(this.environmentVariables);
        return func();
    }
}
