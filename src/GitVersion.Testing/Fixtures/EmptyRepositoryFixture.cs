namespace GitVersion.Testing;

public class EmptyRepositoryFixture(string branchName = RepositoryFixtureBase.MainBranch, bool deleteOnDispose = true) : RepositoryFixtureBase(path => CreateNewRepository(path, branchName), deleteOnDispose);
