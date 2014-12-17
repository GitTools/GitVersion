namespace GitVersionCore.Tests.Fixtures
{
    using GitVersion;
    using LibGit2Sharp;

    public class RemoteRepositoryFixture:RepositoryFixtureBase 
    {
        public RemoteRepositoryFixture( Config configuration) : base(CloneTestRepository, configuration)
        {
        }

        private static IRepository CloneTestRepository(string path)
        {
            LibGit2Sharp.Repository.Clone("https://github.com/grufffta/GitVersionTest", path);
            return new Repository(path);
        }
    }
}
