namespace AcceptanceTests
{
    using System;
    using GitVersion;
    using Helpers;
    using LibGit2Sharp;
    using Shouldly;

    public abstract class RepositoryFixtureBase : IDisposable
    {
        public string RepositoryPath;
        public IRepository Repository;

        protected RepositoryFixtureBase(Func<string, IRepository> repoBuilder)
        {
            RepositoryPath = PathHelper.GetTempPath();
            Repository = repoBuilder(RepositoryPath);
            Repository.Config.Set("user.name", "Test");
            Repository.Config.Set("user.email", "test@email.com");
        }

        public SemanticVersion ExecuteGitVersion()
        {
            var vf = new GitVersionFinder();
            return vf.FindVersion(new GitVersionContext(Repository));
        }

        public void AssertFullSemver(string fullSemver)
        {
            ExecuteGitVersion().ToString("f").ShouldBe(fullSemver);
        }

        public void Dispose()
        {
            Repository.Dispose();

            try
            {
                DirectoryHelper.DeleteDirectory(RepositoryPath);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to clean up repository path at {0}. Received exception: {1}", RepositoryPath, e.Message);
            }
        }
    }
}