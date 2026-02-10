using GitVersion.Agents;
using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.Helpers;
using GitVersion.OutputVariables;
using GitVersion.Testing.Extensions;
using GitVersion.VersionCalculation;
using LibGit2Sharp;

namespace GitVersion.Core.Tests;

public static class GitRepositoryTestingExtensions
{
    private static int commitCount = 1;
    private static readonly DateTimeOffset when = DateTimeOffset.Now;

    public static ICommit CreateMockCommit()
    {
        var objectId = Substitute.For<IObjectId>();
        var sha = Guid.NewGuid().ToString("n") + "00000000";
        objectId.Sha.Returns(sha);
        var commit = Substitute.For<ICommit>();
        commit.Id.Returns(objectId);
        commit.Sha.Returns(sha);
        commit.Message.Returns("Commit " + commitCount++);
        commit.Parents.Returns([]);
        commit.When.Returns(when.AddSeconds(1));
        return commit;
    }

    public static ICommit CreateMockCommit(List<string> diffPaths)
    {
        var commit = CreateMockCommit();
        commit.DiffPaths.Returns(diffPaths);
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
        commitsCollection.MockCollectionReturn(commits);
        commitsCollection.GetCommitsPriorTo(Arg.Any<DateTimeOffset>()).Returns(commits);
        branch.Commits.Returns(commitsCollection);
        return branch;
    }

    extension(IServiceProvider sp)
    {
        public void DiscoverRepository()
        {
            var gitRepository = sp.GetRequiredService<IGitRepository>();
            var gitRepositoryInfo = sp.GetRequiredService<IGitRepositoryInfo>();
            gitRepository.DiscoverRepository(gitRepositoryInfo.GitRootPath);
        }
    }

    extension(IGitRepository repository)
    {
        public IBranch FindBranch(string branchName)
            => repository.Branches.FirstOrDefault(branch => branch.Name.WithoutOrigin == branchName)
               ?? throw new GitVersionException($"Branch {branchName} not found");

        public void DumpGraph(Action<string>? writer = null, int? maxCommits = null)
            => DumpGraph(repository.Path, writer, maxCommits);
    }

    extension(IRepository repository)
    {
        public void DumpGraph(Action<string>? writer = null, int? maxCommits = null)
            => DumpGraph(repository.ToGitRepository().Path, writer, maxCommits);
    }

    extension(RemoteCollection remotes)
    {
        public void RenameRemote(string oldName, string newName)
        {
            if (oldName.IsEquivalentTo(newName)) return;
            if (remotes.Any(remote => remote.Name == newName))
            {
                throw new InvalidOperationException($"A remote with the name '{newName}' already exists.");
            }
            if (remotes.All(remote => remote.Name != oldName))
            {
                throw new InvalidOperationException($"A remote with the name '{oldName}' does not exist.");
            }
            remotes.Add(newName, remotes[oldName].Url);
            remotes.Remove(oldName);
        }
    }

    extension(RepositoryFixtureBase fixture)
    {
        public GitVersionVariables GetVersion(IGitVersionConfiguration? configuration = null,
                                              IRepository? repository = null, string? commitId = null, bool onlyTrackedBranches = true, string? targetBranch = null)
        {
            repository ??= fixture.Repository;
            configuration ??= GitFlowConfigurationBuilder.New.Build();

            var overrideConfiguration = new Dictionary<object, object?>();
            var options = Options.Create(new GitVersionOptions
            {
                WorkingDirectory = repository.Info.WorkingDirectory,
                ConfigurationInfo = { OverrideConfiguration = overrideConfiguration },
                RepositoryInfo =
                {
                    TargetBranch = targetBranch,
                    CommitId = commitId
                },
                Settings = { OnlyTrackedBranches = onlyTrackedBranches }
            });

            try
            {
                var configurationProviderMock = Substitute.For<IConfigurationProvider>();
                configurationProviderMock.Provide(overrideConfiguration).Returns(configuration);
                var sp = ConfigureServices(services =>
                {
                    services.AddSingleton(options);
                    services.AddSingleton(configurationProviderMock);
                });

                sp.DiscoverRepository();

                var variableProvider = sp.GetRequiredService<IVariableProvider>();
                var nextVersionCalculator = sp.GetRequiredService<INextVersionCalculator>();
                var contextOptions = sp.GetRequiredService<Lazy<GitVersionContext>>();

                var context = contextOptions.Value;

                var semanticVersion = nextVersionCalculator.FindVersion();

                var effectiveConfiguration = context.Configuration.GetEffectiveConfiguration(context.CurrentBranch.Name);
                return variableProvider.GetVariablesFor(semanticVersion, context.Configuration, effectiveConfiguration.PreReleaseWeight);
            }
            catch (Exception)
            {
                repository.DumpGraph();
                throw;
            }
        }

        public void WriteVersionVariables(string versionFile)
        {
            var versionVariables = fixture.GetVersion();

            FileSystemHelper.File.WriteAllText(versionFile, versionVariables.ToJson());
        }

        public void AssertFullSemver(string fullSemver,
                                     IGitVersionConfiguration? configuration = null, IRepository? repository = null, string? commitId = null, bool onlyTrackedBranches = true, string? targetBranch = null)
        {
            repository ??= fixture.Repository;

            var variables = fixture.GetVersion(configuration, repository, commitId, onlyTrackedBranches, targetBranch);
            variables.FullSemVer.ShouldBe(fullSemver);
            if (commitId == null)
            {
                fixture.SequenceDiagram.NoteOver(fullSemver, repository.Head.FriendlyName, color: "#D3D3D3");
            }
        }

        public IServiceProvider ConfigureServices(Action<IServiceCollection, RepositoryFixtureBase>? overrideServices = null)
        {
            var services = new ServiceCollection()
                .AddModule(new GitVersionCoreTestModule());

            overrideServices?.Invoke(services, fixture);

            return services.BuildServiceProvider();
        }
    }

    extension(RemoteRepositoryFixture fixture)
    {
        /// <summary>
        /// Simulates running on a build server
        /// </summary>
        public void InitializeRepository()
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

            var gitPreparer = serviceProvider.GetRequiredService<IGitPreparer>();
            gitPreparer.Prepare();
        }
    }

    private static ServiceProvider ConfigureServices(Action<IServiceCollection>? servicesOverrides = null)
    {
        var services = new ServiceCollection()
            .AddModule(new GitVersionCoreTestModule());

        servicesOverrides?.Invoke(services);
        return services.BuildServiceProvider();
    }

    private static void DumpGraph(string workingDirectory, Action<string>? writer = null, int? maxCommits = null)
        => GitTestExtensions.ExecuteGitCmd(GitExtensions.CreateGitLogArgs(maxCommits), workingDirectory, writer);
}
