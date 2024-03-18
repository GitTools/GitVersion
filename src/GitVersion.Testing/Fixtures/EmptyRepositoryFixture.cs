namespace GitVersion.Testing;

public class EmptyRepositoryFixture(string branchName = "main") : RepositoryFixtureBase(path => CreateNewRepository(path, branchName));
