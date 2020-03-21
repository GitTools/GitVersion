using System;
using GitTools.Testing;
using GitVersion;
using GitVersion.Configuration;
using GitVersion.Extensions;
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

            var options = Options.Create(new Arguments { OverrideConfig = configuration, TargetPath = repository.GetRepositoryDirectory() });

            var sp = ConfigureServices(services =>
            {
                services.AddSingleton(options);
            });

            var variableProvider = sp.GetService<IVariableProvider>();
            var nextVersionCalculator = sp.GetService<INextVersionCalculator>();
            var contextOptions = sp.GetService<IOptions<GitVersionContext>>();

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
                context.Repository.DumpGraph();
                throw;
            }
        }

        public static void AssertFullSemver(this RepositoryFixtureBase fixture, string fullSemver, IRepository repository = null, string commitId = null, bool onlyTrackedBranches = true, string targetBranch = null)
        {
            fixture.AssertFullSemver(new Config(), fullSemver, repository, commitId, onlyTrackedBranches, targetBranch);
        }

        public static void AssertFullSemver(this RepositoryFixtureBase fixture, Config configuration, string fullSemver, IRepository repository = null, string commitId = null, bool onlyTrackedBranches = true, string targetBranch = null)
        {
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
            var arguments = new Arguments
            {
                Authentication = new AuthenticationInfo(),
                TargetPath = fixture.LocalRepositoryFixture.RepositoryPath
            };
            var options = Options.Create(arguments);

            var serviceProvider = ConfigureServices(services =>
            {
                services.AddSingleton(options);
            });

            var gitPreparer = serviceProvider.GetService<IGitPreparer>() as GitPreparer;
            gitPreparer?.PrepareInternal(true, null);
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
