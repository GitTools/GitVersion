using System;
using GitTools.Testing;
using GitVersion;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Logging;
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
        private static readonly IServiceProvider sp;

        static GitToolsTestingExtensions() => sp = ConfigureService();

        public static VersionVariables GetVersion(this RepositoryFixtureBase fixture, Config configuration = null, IRepository repository = null, string commitId = null, bool onlyTrackedBranches = true, string targetBranch = null)
        {
            if (configuration == null)
            {
                configuration = new Config();
                configuration.Reset();
            }

            var log = sp.GetService<ILog>();
            var variableProvider = sp.GetService<IVariableProvider>();
            var nextVersionCalculator = sp.GetService<INextVersionCalculator>();

            var gitVersionContext = new GitVersionContext(repository ?? fixture.Repository, log, targetBranch, configuration, onlyTrackedBranches, commitId);
            var executeGitVersion = nextVersionCalculator.FindVersion(gitVersionContext);
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

            var serviceProvider = ConfigureService(services =>
            {
                services.AddSingleton(options);
            });

            var gitPreparer = serviceProvider.GetService<IGitPreparer>() as GitPreparer;
            gitPreparer?.Prepare(true, null);
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
