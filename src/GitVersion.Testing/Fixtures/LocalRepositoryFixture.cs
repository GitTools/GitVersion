using LibGit2Sharp;

namespace GitVersion.Testing;

public class LocalRepositoryFixture : RepositoryFixtureBase
{
    public LocalRepositoryFixture(IRepository repository) : base(repository)
    {
    }
}
