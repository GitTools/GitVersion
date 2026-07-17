using Buildalyzer;
using Buildalyzer.Environment;
using GitVersion.Helpers;
using GitVersion.Tests;
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
    private readonly string projectPath;

    public MsBuildExeFixture(RepositoryFixtureBase fixture, string workingDirectory = "", string language = "C#")
    {
        var projectExtension = AssemblyInfoFileHelper.GetProjectExtension(language);
        this.fixture = fixture;
        this.projectPath = FileSystemHelper.Path.Combine(workingDirectory, $"app.{projectExtension}");

        var versionFile = FileSystemHelper.Path.Combine(workingDirectory, "gitversion.json");

        fixture.WriteVersionVariables(versionFile);
    }

    public MsBuildExeFixtureResult Execute()
    {
        var analyzer = this.manager.GetProject(this.projectPath);

        var output = new StringWriter();
        analyzer.AddBuildLogger(new ConsoleLogger(LoggerVerbosity.Normal, output.Write, null, null));

        var environmentOptions = new EnvironmentOptions { DesignTime = false };
        environmentOptions.TargetsToBuild.Clear();
        environmentOptions.TargetsToBuild.Add(OutputTarget);

        if (this.environmentVariables != null)
        {
            foreach (var (key, value) in this.environmentVariables)
            {
                analyzer.SetEnvironmentVariable(key, value!);
            }
        }

        // The MSBuild child process inherits the current process environment, so a build-agent
        // variable set by a concurrent MsBuildTaskFixture (e.g. TF_BUILD) would otherwise leak into
        // it and be wrongly detected. Entering the scope serializes against those fixtures and
        // resets the process environment so the child starts from a clean, non-CI baseline.
        IAnalyzerResults results;
        using (MsBuildProcessEnvironment.Enter())
        {
            results = analyzer.Build(environmentOptions);
        }

        return new MsBuildExeFixtureResult(this.fixture)
        {
            ProjectPath = this.projectPath,
            Output = output.ToString(),
            MsBuild = results
        };
    }

    public void CreateTestProject(Action<ProjectCreator> extendProject)
    {
        var project = ProjectCreator.Templates.SdkCsproj(this.projectPath);
        extendProject(project);

        project.Save();
    }
}
