using Buildalyzer;
using Buildalyzer.Environment;
using GitTools.Testing;
using GitVersion.Core.Tests;
using GitVersion.Core.Tests.Helpers;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using Microsoft.Build.Utilities.ProjectCreation;
using StringWriter = System.IO.StringWriter;

namespace GitVersion.MsBuild.Tests.Helpers;

public class MsBuildExeFixture
{
    private readonly RepositoryFixtureBase fixture;
    private KeyValuePair<string, string>[] environmentVariables;

    public void WithEnv(params KeyValuePair<string, string>[] envs) => this.environmentVariables = envs;

    public const string OutputTarget = "GitVersionOutput";

    private readonly AnalyzerManager manager = new();
    private readonly string ProjectPath;

    public MsBuildExeFixture(RepositoryFixtureBase fixture, string workingDirectory = "")
    {
        this.fixture = fixture;
        this.ProjectPath = Path.Combine(workingDirectory, "app.csproj");

        var versionFile = Path.Combine(workingDirectory, "gitversion.json");

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
            foreach (var pair in this.environmentVariables)
            {
                analyzer.SetEnvironmentVariable(pair.Key, pair.Value);
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
        var project = RuntimeHelper.IsCoreClr()
            ? ProjectCreator.Templates.SdkCsproj(this.ProjectPath)
            : ProjectCreator.Templates.LegacyCsproj(this.ProjectPath, defaultTargets: null, targetFrameworkVersion: "v4.8", toolsVersion: "15.0");

        if (project == null) return;

        extendProject(project);

        project.Save();
    }
}
