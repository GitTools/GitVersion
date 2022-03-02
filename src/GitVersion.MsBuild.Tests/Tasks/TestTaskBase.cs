using GitTools.Testing;
using GitVersion.BuildAgents;
using GitVersion.Core.Tests.Helpers;
using GitVersion.MsBuild.Tests.Helpers;
using LibGit2Sharp;
using Microsoft.Build.Utilities.ProjectCreation;

namespace GitVersion.MsBuild.Tests.Tasks;

public class TestTaskBase : TestBase
{
    private static readonly IDictionary<string, string> env = new Dictionary<string, string>
    {
        { AzurePipelines.EnvironmentVariableName, "true" },
        { "BUILD_SOURCEBRANCH", null }
    };

    protected static MsBuildTaskFixtureResult<T> ExecuteMsBuildTask<T>(T task) where T : GitVersionTaskBase
    {
        var fixture = CreateLocalRepositoryFixture();
        task.SolutionDirectory = fixture.RepositoryPath;

        var msbuildFixture = new MsBuildTaskFixture(fixture);
        var result = msbuildFixture.Execute(task);
        if (result.Success == false) Console.WriteLine(result.Log);
        return result;
    }

    protected static MsBuildExeFixtureResult ExecuteMsBuildExe(Action<ProjectCreator> extendProject)
    {
        var fixture = CreateLocalRepositoryFixture();

        var msbuildFixture = new MsBuildExeFixture(fixture, fixture.RepositoryPath);

        msbuildFixture.CreateTestProject(extendProject);

        var result = msbuildFixture.Execute();
        if (result.MsBuild.OverallSuccess == false) Console.WriteLine(result.Output);
        return result;
    }

    protected static MsBuildTaskFixtureResult<T> ExecuteMsBuildTaskInAzurePipeline<T>(T task, string buildNumber = null, string configurationText = null) where T : GitVersionTaskBase
    {
        var fixture = CreateRemoteRepositoryFixture();
        task.SolutionDirectory = fixture.LocalRepositoryFixture.RepositoryPath;
        var msbuildFixture = new MsBuildTaskFixture(fixture);
        var environmentVariables = new List<KeyValuePair<string, string>>(env.ToArray());
        if (buildNumber != null)
        {
            environmentVariables.Add(new KeyValuePair<string, string>("BUILD_BUILDNUMBER", buildNumber));
        }
        msbuildFixture.WithEnv(environmentVariables.ToArray());
        if (configurationText != null)
        {
            CreateConfiguration(task.SolutionDirectory, configurationText);
        }

        var result = msbuildFixture.Execute(task);

        if (result.Success == false) Console.WriteLine(result.Log);
        return result;
    }

    protected static MsBuildTaskFixtureResult<T> ExecuteMsBuildTaskInGitHubActions<T>(T task, string envFilePath) where T : GitVersionTaskBase
    {
        var fixture = CreateRemoteRepositoryFixture();
        task.SolutionDirectory = fixture.LocalRepositoryFixture.RepositoryPath;
        var msbuildFixture = new MsBuildTaskFixture(fixture);
        msbuildFixture.WithEnv(
            new KeyValuePair<string, string>("GITHUB_ACTIONS", "true"),
            new KeyValuePair<string, string>("GITHUB_ENV", envFilePath)
        );
        var result = msbuildFixture.Execute(task);
        if (result.Success == false)
            Console.WriteLine(result.Log);
        return result;
    }

    protected static MsBuildExeFixtureResult ExecuteMsBuildExeInAzurePipeline(Action<ProjectCreator> extendProject)
    {
        var fixture = CreateRemoteRepositoryFixture();

        var msbuildFixture = new MsBuildExeFixture(fixture, fixture.LocalRepositoryFixture.RepositoryPath);

        msbuildFixture.CreateTestProject(extendProject);
        msbuildFixture.WithEnv(env.ToArray());

        var result = msbuildFixture.Execute();
        if (result.MsBuild.OverallSuccess == false) Console.WriteLine(result.Output);
        return result;
    }

    private static EmptyRepositoryFixture CreateLocalRepositoryFixture()
    {
        var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.2.3");
        fixture.MakeACommit();

        return fixture;
    }

    private static RemoteRepositoryFixture CreateRemoteRepositoryFixture()
    {
        var fixture = new RemoteRepositoryFixture();
        fixture.Repository.MakeACommit();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.MakeACommit();
        fixture.Repository.CreateBranch("develop");

        Commands.Fetch((Repository)fixture.LocalRepositoryFixture.Repository, fixture.LocalRepositoryFixture.Repository.Network.Remotes.First().Name, Array.Empty<string>(), new FetchOptions(), null);
        Commands.Checkout(fixture.LocalRepositoryFixture.Repository, fixture.Repository.Head.Tip);
        fixture.LocalRepositoryFixture.Repository.Branches.Remove(MainBranch);
        fixture.InitializeRepo();
        return fixture;
    }

    private static void CreateConfiguration(string repoFolder, string content)
    {
        var configFilePath = Path.Combine(repoFolder, Configuration.ConfigFileLocator.DefaultFileName);
        File.WriteAllText(configFilePath, content);
    }
}
