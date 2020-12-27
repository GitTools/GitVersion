using System;
using LibGit2Sharp;

namespace GitTools.Testing
{
    public class EmptyRepositoryFixture : RepositoryFixtureBase
    {
        public EmptyRepositoryFixture() : base(CreateNewRepository)
        {
        }

        private static IRepository CreateNewRepository(string path)
        {
            LibGit2Sharp.Repository.Init(path);
            Console.WriteLine("Created git repository at '{0}'", path);

            return new Repository(path);
        }
    }
}
