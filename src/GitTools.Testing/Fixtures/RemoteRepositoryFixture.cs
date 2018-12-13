// ReSharper disable once CheckNamespace
namespace GitTools.Testing
{
    using System;
    using LibGit2Sharp;

    /// <summary>
    ///     Creates a remote repository then clones it
    ///     Remote = Repository
    ///     Local  = LocalRepositoryFixture
    /// </summary>
    public class RemoteRepositoryFixture : RepositoryFixtureBase
    {
        public RemoteRepositoryFixture(Func<string, IRepository> builder)
            : base(builder)
        {
            CreateLocalRepository();
        }

        public RemoteRepositoryFixture() : this(CreateNewRepository)
        {
        }

        /// <summary>
        ///     Fixture pointing at the local repository
        /// </summary>
        public LocalRepositoryFixture LocalRepositoryFixture { get; private set; }

        private static IRepository CreateNewRepository(string path)
        {
            LibGit2Sharp.Repository.Init(path);
            Console.WriteLine("Created git repository at '{0}'", path);

            var repo = new Repository(path);
            repo.MakeCommits(5);
            return repo;
        }

        private void CreateLocalRepository()
        {
            LocalRepositoryFixture = CloneRepository();
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            LocalRepositoryFixture.Dispose();
            base.Dispose();
        }
    }
}