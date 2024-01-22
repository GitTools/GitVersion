using LibGit2Sharp;

namespace GitVersion.Testing;

public class LocalRepositoryFixture(Repository repository) : RepositoryFixtureBase(repository);
