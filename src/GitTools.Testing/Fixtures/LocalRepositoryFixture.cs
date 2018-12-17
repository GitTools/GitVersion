// ReSharper disable once CheckNamespace
namespace GitTools.Testing
{
    using LibGit2Sharp;

    public class LocalRepositoryFixture : RepositoryFixtureBase
    {
        public LocalRepositoryFixture(IRepository repository) : base(repository)
        {
        }
    }
}