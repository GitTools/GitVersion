namespace GitVersionCore.Tests
{
    using System;
    using GitTools.Testing.Fixtures;
    using GitVersion;
    using LibGit2Sharp;
    using Shouldly;

    public static class GitToolsTestingExtensions
    {
        public static VersionVariables GetVersion(this RepositoryFixtureBase fixture, Config configuration, IRepository repository = null, string commitId = null, bool isForTrackedBranchOnly = true)
        {
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


        public static void AssertFullSemver(this RepositoryFixtureBase fixture, string fullSemver, IRepository repository = null, string commitId = null)
        {
            fixture.AssertFullSemver(new Config(), fullSemver, repository, commitId);
        }

        public static void AssertFullSemver(this RepositoryFixtureBase fixture, Config configuration, string fullSemver, IRepository repository = null, string commitId = null)
        {
            Console.WriteLine("---------");

            try
            {
                var variables = fixture.GetVersion(configuration, repository, commitId);
                variables.FullSemVer.ShouldBe(fullSemver);
            }
            catch (Exception)
            {
                (repository ?? fixture.Repository).DumpGraph();
                throw;
            }
            if (commitId == null)
            {
                // TODO Restore color: #D3D3D3
               fixture.SequenceDiagram.NoteOver(fullSemver, fixture.Repository.Head.FriendlyName);
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