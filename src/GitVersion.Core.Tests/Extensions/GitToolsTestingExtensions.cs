using GitTools.Testing;
using GitVersion.BuildAgents;
using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using GitVersion.Model.Configuration;
using GitVersion.OutputVariables;
using GitVersion.VersionCalculation;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace GitVersion.Core.Tests;

public static class GitToolsTestingExtensions
{
    private static int commitCount = 1;
    private static readonly DateTimeOffset when = DateTimeOffset.Now;

    public static ICommit CreateMockCommit()
    {
        var objectId = Substitute.For<IObjectId>();
        objectId.Sha.Returns(Guid.NewGuid().ToString("n") + "00000000");

        var commit = Substitute.For<ICommit>();
        commit.Id.Returns(objectId);
        commit.Sha.Returns(objectId.Sha);
        commit.Message.Returns("Commit " + commitCount++);
        commit.Parents.Returns(Enumerable.Empty<ICommit>());
        commit.When.Returns(when.AddSeconds(1));
        return commit;
    }
    public static IBranch CreateMockBranch(string name, params ICommit[] commits)
    {
        var branch = Substitute.For<IBranch>();
        branch.Name.Returns(new ReferenceName(name));
        branch.IsTracking.Returns(true);
        branch.IsRemote.Returns(false);
        branch.IsDetachedHead.Returns(false);
        branch.Tip.Returns(commits.FirstOrDefault());

        var commitsCollection = Substitute.For<ICommitCollection>();
        commitsCollection.GetEnumerator().Returns(_ => ((IEnumerable<ICommit>)commits).GetEnumerator());
        commitsCollection.GetCommitsPriorTo(Arg.Any<DateTimeOffset>()).Returns(commits);
        branch.Commits.Returns(commitsCollection);
        return branch;
    }

    public static IBranch FindBranch(this IGitRepository repository, string branchName) => repository.Branches.FirstOrDefault(x => x.Name.WithoutRemote == branchName);

    public static void DumpGraph(this IGitRepository repository, Action<string> writer = null, int? maxCommits = null) => GitExtensions.DumpGraph(repository.Path, writer, maxCommits);

    public static void DumpGraph(this IRepository repository, Action<string> writer = null, int? maxCommits = null) => GitExtensions.DumpGraph(repository.ToGitRepository().Path, writer, maxCommits);

    public static VersionVariables GetVersion(this RepositoryFixtureBase fixture, Config configuration = null, IRepository repository = null, string commitId = null, bool onlyTrackedBranches = true, string branch = null)
    {
        configuration ??= new ConfigurationBuilder().Build();

        repository ??= fixture.Repository;

        var options = Options.Create(new GitVersionOptions
        {
            WorkingDirectory = repository.Info.WorkingDirectory,
            ConfigInfo = { OverrideConfig = configuration },
            RepositoryInfo =
            {
                TargetBranch = branch,
                CommitId = commitId,
            },
            Settings = { OnlyTrackedBranches = onlyTrackedBranches }
        });

        var sp = ConfigureServices(services => services.AddSingleton(options));

        var variableProvider = sp.GetService<IVariableProvider>();
        var nextVersionCalculator = sp.GetService<INextVersionCalculator>();
        var contextOptions = sp.GetService<Lazy<GitVersionContext>>();

        var context = contextOptions!.Value;

        try
        {
            var semanticVersion = nextVersionCalculator!.FindVersion();
            var variables = variableProvider!.GetVariablesFor(semanticVersion, context.Configuration, context.IsCurrentCommitTagged);

            return variables;
        }
        catch (Exception)
        {
            Console.WriteLine("Test failing, dumping repository graph");
            repository.DumpGraph();
            throw;
        }
    }

    public static void WriteVersionVariables(this RepositoryFixtureBase fixture, string versionFile)
    {
        var versionInfo = fixture.GetVersion();

        using var stream = File.Open(versionFile, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        using var writer = new StreamWriter(stream);
        writer.Write(versionInfo.ToString());
    }

    public static void AssertFullSemver(this RepositoryFixtureBase fixture, string fullSemver, Config configuration = null, IRepository repository = null, string commitId = null, bool onlyTrackedBranches = true, string targetBranch = null)
    {
        configuration ??= new Config();
        configuration = new ConfigurationBuilder().Add(configuration).Build();
        Console.WriteLine("---------");

        try
        {
            var variables = fixture.GetVersion(configuration, repository, commitId, onlyTrackedBranches, targetBranch);
            variables.FullSemVer.ShouldBe(fullSemver);
        }
        catch (Exception)
        {
            (repository ?? fixture.Repository).DumpGraph();
            throw;
        }
        if (commitId == null)
        {
            fixture.SequenceDiagram.NoteOver(fullSemver, fixture.Repository.Head.FriendlyName, color: "#D3D3D3");
        }
    }

    /// <summary>
    /// Simulates running on build server
    /// </summary>
    public static void InitializeRepo(this RemoteRepositoryFixture fixture)
    {
        var gitVersionOptions = new GitVersionOptions
        {
            WorkingDirectory = fixture.LocalRepositoryFixture.RepositoryPath
        };
        var options = Options.Create(gitVersionOptions);

        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable(AzurePipelines.EnvironmentVariableName, "true");

        var serviceProvider = ConfigureServices(services =>
        {
            services.AddSingleton(options);
            services.AddSingleton(environment);
        });

        var gitPreparer = serviceProvider.GetService<IGitPreparer>();
        gitPreparer!.Prepare();
    }

    private static IServiceProvider ConfigureServices(Action<IServiceCollection> servicesOverrides = null)
    {
        var services = new ServiceCollection()
            .AddModule(new GitVersionCoreTestModule());

        servicesOverrides?.Invoke(services);
        return services.BuildServiceProvider();
    }
}
