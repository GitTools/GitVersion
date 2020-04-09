using System;
using GitTools.Testing;
using GitVersion;
using GitVersion.BuildAgents;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Model.Configuration;
using GitVersion.OutputVariables;
using GitVersion.VersionCalculation;
using GitVersionCore.Tests.Helpers;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace GitVersionCore.Tests
{
    public static class GitToolsTestingExtensions
    {
        public static VersionVariables GetVersion(this RepositoryFixtureBase fixture, Config configuration = null, IRepository repository = null, string commitId = null, bool onlyTrackedBranches = true, string branch = null)
        {
            if (configuration == null)
            {
                configuration = new Config();
                configuration.Reset();
            }

            repository ??= fixture.Repository;

            var options = Options.Create(new GitVersionOptions
            {
                WorkingDirectory = repository.GetRepositoryDirectory(),
                ConfigInfo = { OverrideConfig = configuration },
                RepositoryInfo =
                {
                    TargetBranch = branch,
                    CommitId = commitId,
                },
                Settings = { OnlyTrackedBranches = onlyTrackedBranches }
            });

            var sp = ConfigureServices(services =>
            {
                services.AddSingleton(options);
            });

            var variableProvider = sp.GetService<IVariableProvider>();
            var nextVersionCalculator = sp.GetService<INextVersionCalculator>();
            var contextOptions = sp.GetService<Lazy<GitVersionContext>>();

            var context = contextOptions.Value;

            try
            {
                var executeGitVersion = nextVersionCalculator.FindVersion();
                var variables = variableProvider.GetVariablesFor(executeGitVersion, context.Configuration, context.IsCurrentCommitTagged);

                return variables;
            }
            catch (Exception)
            {
                Console.WriteLine("Test failing, dumping repository graph");
                repository.DumpGraph();
                throw;
            }
        }

        public static void AssertFullSemver(this RepositoryFixtureBase fixture, string fullSemver, Config configuration = null, IRepository repository = null, string commitId = null, bool onlyTrackedBranches = true, string targetBranch = null)
        {
            configuration ??= new Config();
            configuration.Reset();
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
            gitPreparer.Prepare();
        }

        private static IServiceProvider ConfigureServices(Action<IServiceCollection> servicesOverrides = null)
        {
            var services = new ServiceCollection()
                .AddModule(new GitVersionCoreTestModule());

            servicesOverrides?.Invoke(services);
            return services.BuildServiceProvider();
        }
    }
}
