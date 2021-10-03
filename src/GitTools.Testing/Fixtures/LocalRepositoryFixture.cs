using LibGit2Sharp;

namespace GitTools.Testing;

public class LocalRepositoryFixture : RepositoryFixtureBase
{
    public LocalRepositoryFixture(IRepository repository) : base(repository)
    {
    }
}
