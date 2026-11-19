using GitVersion.Testing.Git;

namespace GitVersion.Testing;

public class LocalRepositoryFixture(Repository repository) : RepositoryFixtureBase(repository);
