namespace GitVersionCore.Tests
{
    using System;
    using GitTools;
    using GitTools.Testing;
    using GitVersion;
    using LibGit2Sharp;
    using Shouldly;

    public static class GitToolsTestingExtensions
    {
        public static Config ApplyDefaults(this Config config)
        {
            ConfigurationProvider.ApplyDefaultsTo(config);
            return config;
        }

        public static VersionVariables GetVersion(this RepositoryFixtureBase fixture, Config configuration = null, IRepository repository = null, string commitId = null, bool isForTrackedBranchOnly = true)
        {
            if (configuration == null)
            {
                configuration = new Config();
                ConfigurationProvider.ApplyDefaultsTo(configuration);
            }
            var gitVersionContext = new GitVersionContext(repository ?? fixture.Repository, configuration, isForTrackedBranchOnly, commitId);
            var executeGitVersion = ExecuteGitVersion(gitVersionContext);
            var variables = VariableProvider.GetVariablesFor(executeGitVersion, gitVersionContext.Configuration, gitVersionContext.IsCurrentCommitTagged);
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

        public static void AssertFullSemver(this RepositoryFixtureBase fixture, string fullSemver, IRepository repository = null, string commitId = null, bool isForTrackedBranchOnly = true)
        {
            fixture.AssertFullSemver(new Config(), fullSemver, repository, commitId, isForTrackedBranchOnly);
        }

        public static void AssertFullSemver(this RepositoryFixtureBase fixture, Config configuration, string fullSemver, IRepository repository = null, string commitId = null, bool isForTrackedBranchOnly = true)
        {
            ConfigurationProvider.ApplyDefaultsTo(configuration);
            Console.WriteLine("---------");

            try
            {
                var variables = fixture.GetVersion(configuration, repository, commitId, isForTrackedBranchOnly);
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

        static SemanticVersion ExecuteGitVersion(GitVersionContext context)
        {
            var vf = new GitVersionFinder();
            return vf.FindVersion(context);
        }

        /// <summary>
        /// Simulates running on build server
        /// </summary>
        public static void InitialiseRepo(this RemoteRepositoryFixture fixture)
        {
            new GitPreparer(null, null, new Authentication(), false, fixture.LocalRepositoryFixture.RepositoryPath).Initialise(true, null);
        }
    }
}