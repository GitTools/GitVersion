using Buildalyzer;
using Buildalyzer.Environment;
using GitTools.Testing;
using GitVersion.Core.Tests;
using GitVersion.Helpers;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using Microsoft.Build.Utilities.ProjectCreation;

namespace GitVersion.MsBuild.Tests.Helpers;

public class MsBuildExeFixture
{
    private readonly RepositoryFixtureBase fixture;
    private KeyValuePair<string, string?>[]? environmentVariables;

    public void WithEnv(params KeyValuePair<string, string?>[] envs) => this.environmentVariables = envs;

    public const string OutputTarget = "GitVersionOutput";

    private readonly AnalyzerManager manager = new();
    private readonly string ProjectPath;

    public MsBuildExeFixture(RepositoryFixtureBase fixture, string workingDirectory = "")
    {
        this.fixture = fixture;
        this.ProjectPath = PathHelper.Combine(workingDirectory, "app.csproj");

        var versionFile = PathHelper.Combine(workingDirectory, "gitversion.json");

        fixture.WriteVersionVariables(versionFile);
    }

    public MsBuildExeFixtureResult Execute()
    {
        var analyzer = this.manager.GetProject(this.ProjectPath);

        var output = new StringWriter();
        analyzer.AddBuildLogger(new ConsoleLogger(LoggerVerbosity.Normal, output.Write, null, null));

        var environmentOptions = new EnvironmentOptions { DesignTime = false };
        environmentOptions.TargetsToBuild.Clear();
        environmentOptions.TargetsToBuild.Add(OutputTarget);

        if (this.environmentVariables != null)
        {
            foreach (var (key, value) in this.environmentVariables)
            {
                analyzer.SetEnvironmentVariable(key, value);
            }
        }

        var results = analyzer.Build(environmentOptions);

        return new MsBuildExeFixtureResult(this.fixture)
        {
            ProjectPath = ProjectPath,
            Output = output.ToString(),
            MsBuild = results
        };
    }

    public void CreateTestProject(Action<ProjectCreator> extendProject)
    {
        var project = ProjectCreator.Templates.SdkCsproj(this.ProjectPath);
        extendProject(project);

        project.Save();
    }
}
