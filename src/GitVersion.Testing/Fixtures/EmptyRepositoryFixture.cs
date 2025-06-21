namespace GitVersion.Testing;

public class EmptyRepositoryFixture(string branchName = RepositoryFixtureBase.MainBranch) : RepositoryFixtureBase(path => CreateNewRepository(path, branchName));
