using GitVersion;
using LibGit2Sharp;

public class LocalRepositoryFixture : RepositoryFixtureBase
{
    public LocalRepositoryFixture(Config configuration, IRepository repository) : base(configuration, repository)
    {
    }
}