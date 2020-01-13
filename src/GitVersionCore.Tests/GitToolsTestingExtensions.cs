using System;
using GitTools.Testing;
using GitVersion;
using GitVersion.Configuration;
using GitVersion.OutputVariables;
using LibGit2Sharp;
using Shouldly;
using GitVersion.Logging;
using Microsoft.Extensions.Options;
using GitVersion.Extensions;
using GitVersionCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersionCore.Tests
{
    public static class GitToolsTestingExtensions
    {
        private static readonly IServiceProvider sp;

        static GitToolsTestingExtensions() => sp = ConfigureService();

        public static VersionVariables GetVersion(this RepositoryFixtureBase fixture, Config configuration = null, IRepository repository = null, string commitId = null, bool isForTrackedBranchOnly = true, string targetBranch = null)
        {
            if (configuration == null)
            {
                configuration = new Config();
                configuration.Reset();
            }

            var log = sp.GetService<ILog>();
            var variableProvider = sp.GetService<IVariableProvider>();
            var versionFinder = sp.GetService<IGitVersionFinder>();

            var gitVersionContext = new GitVersionContext(repository ?? fixture.Repository, log, targetBranch, configuration, isForTrackedBranchOnly, commitId);
            var executeGitVersion = versionFinder.FindVersion(gitVersionContext);
            var variables = variableProvider.GetVariablesFor(executeGitVersion, gitVersionContext.Configuration, gitVersionContext.IsCurrentCommitTagged);

            try
            {
                return variables;
            }
            catch (Exception)
            {
                Console.WriteLine("Test failing, dumping repository graph");
                gitVersionContext.Repository.DumpGraph();
                throw;
            }
        }

        public static void AssertFullSemver(this RepositoryFixtureBase fixture, string fullSemver, IRepository repository = null, string commitId = null, bool isForTrackedBranchOnly = true, string targetBranch = null)
        {
            fixture.AssertFullSemver(new Config(), fullSemver, repository, commitId, isForTrackedBranchOnly, targetBranch);
        }

        public static void AssertFullSemver(this RepositoryFixtureBase fixture, Config configuration, string fullSemver, IRepository repository = null, string commitId = null, bool isForTrackedBranchOnly = true, string targetBranch = null)
        {
            configuration.Reset();
            Console.WriteLine("---------");

            try
            {
                var variables = fixture.GetVersion(configuration, repository, commitId, isForTrackedBranchOnly, targetBranch);
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
                Authentication = new Authentication(),
                TargetPath = fixture.LocalRepositoryFixture.RepositoryPath
            };
            var options = Options.Create(arguments);

            var serviceProvider = ConfigureService(services =>
            {
                services.AddSingleton(options); 
            });

            var gitPreparer = serviceProvider.GetService<IGitPreparer>();
            gitPreparer.Prepare(true, null);
        }

        private static IServiceProvider ConfigureService(Action<IServiceCollection> servicesOverrides = null)
        {
            var services = new ServiceCollection()
                .AddModule(new GitVersionCoreTestModule());

            servicesOverrides?.Invoke(services);
            return services.BuildServiceProvider(); 
        }
    }
}
