using System;
using GitTools.Testing;
using GitVersion;
using GitVersion.Configuration;
using GitVersion.OutputVariables;
using LibGit2Sharp;
using Shouldly;
using GitVersion.Logging;
using GitVersion.VersionCalculation;
using GitVersionCore.Tests.VersionCalculation;
using Microsoft.Extensions.Options;
using GitVersion.Extensions;

namespace GitVersionCore.Tests
{
    public static class GitToolsTestingExtensions
    {
        public static Config ApplyDefaults(this Config config)
        {
            config.Reset();
            return config;
        }

        public static VersionVariables GetVersion(this RepositoryFixtureBase fixture, Config configuration = null, IRepository repository = null, string commitId = null, bool isForTrackedBranchOnly = true, string targetBranch = null)
        {
            if (configuration == null)
            {
                configuration = new Config();
                configuration.Reset();
            }

            var log = new NullLog();
            var metaDataCalculator = new MetaDataCalculator();
            var baseVersionCalculator = new TestBaseVersionStrategiesCalculator(log);
            var mainlineVersionCalculator = new MainlineVersionCalculator(log, metaDataCalculator);
            var nextVersionCalculator = new NextVersionCalculator(log, metaDataCalculator, baseVersionCalculator, mainlineVersionCalculator);
            var variableProvider = new VariableProvider(nextVersionCalculator, new TestEnvironment());
            var gitVersionContext = new GitVersionContext(repository ?? fixture.Repository, log, targetBranch, configuration, isForTrackedBranchOnly, commitId);
            var executeGitVersion = ExecuteGitVersion(gitVersionContext);
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

        private static SemanticVersion ExecuteGitVersion(GitVersionContext context)
        {
            var log = new NullLog();
            var metadataCalculator = new MetaDataCalculator();
            var baseVersionCalculator = new TestBaseVersionStrategiesCalculator(log);
            var mainlineVersionCalculator = new MainlineVersionCalculator(log, metadataCalculator);
            var nextVersionCalculator = new NextVersionCalculator(log, metadataCalculator, baseVersionCalculator, mainlineVersionCalculator);
            var vf = new GitVersionFinder(log, nextVersionCalculator);
            return vf.FindVersion(context);
        }

        /// <summary>
        /// Simulates running on build server
        /// </summary>
        public static void InitializeRepo(this RemoteRepositoryFixture fixture)
        {
            var log = new NullLog();

            var arguments = new Arguments
            {
                Authentication = new Authentication(),
                TargetPath = fixture.LocalRepositoryFixture.RepositoryPath
            };
            new GitPreparer(log, new TestEnvironment(), Options.Create(arguments)).Prepare(true, null);
        }
    }
}
