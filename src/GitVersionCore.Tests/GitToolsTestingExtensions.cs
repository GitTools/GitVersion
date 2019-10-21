using System;
using GitTools.Testing;
using GitVersion;
using GitVersion.Configuration;
using GitVersion.OutputVariables;
using LibGit2Sharp;
using Shouldly;
using GitVersion.Helpers;
using GitVersion.Logging;

namespace GitVersionCore.Tests
{
    public static class GitToolsTestingExtensions
    {
        public static Config ApplyDefaults(this Config config)
        {
            ConfigurationProvider.ApplyDefaultsTo(config);
            return config;
        }

        public static VersionVariables GetVersion(this RepositoryFixtureBase fixture, Config configuration = null, IRepository repository = null, string commitId = null, bool isForTrackedBranchOnly = true, string targetBranch = null)
        {
            if (configuration == null)
            {
                configuration = new Config();
                ConfigurationProvider.ApplyDefaultsTo(configuration);
            }

            var log = new NullLog();
            var variableProvider = new VariableProvider(log);
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
            ConfigurationProvider.ApplyDefaultsTo(configuration);
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
            var vf = new GitVersionFinder();
            return vf.FindVersion(new NullLog(), context);
        }

        /// <summary>
        /// Simulates running on build server
        /// </summary>
        public static void InitialiseRepo(this RemoteRepositoryFixture fixture)
        {
            new GitPreparer(new NullLog(), null, null, new Authentication(), false, fixture.LocalRepositoryFixture.RepositoryPath).Initialise(true, null);
        }
    }
}
