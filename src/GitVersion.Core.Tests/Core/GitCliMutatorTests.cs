using GitVersion.Git;
using GitVersion.Helpers;
using GitVersion.Testing.Extensions;

namespace GitVersion.Tests;

[TestFixture]
public class GitCliMutatorTests : TestBase
{
    private static GitCliMutator CreateMutator() =>
        new(NullLogger<GitCliMutator>.Instance, new GitCliExecutor(NullLogger<GitCliExecutor>.Instance));

    [Test]
    public void CheckoutSwitchesToTheGivenBranch()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        fixture.Repository.CreateBranch("feature/cli");

        CreateMutator().Checkout(fixture.RepositoryPath, "feature/cli");

        fixture.Repository.Head.FriendlyName.ShouldBe("feature/cli");
    }

    [Test]
    public void CheckoutOfUnknownSpecThrows()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();

        Should.Throw<InvalidOperationException>(() => CreateMutator().Checkout(fixture.RepositoryPath, "does-not-exist"));
    }

    [Test]
    public void FetchBringsNewRemoteCommitsIntoTheLocalRepository()
    {
        using var fixture = new RemoteRepositoryFixture();
        var newCommit = fixture.Repository.MakeACommit();
        var localRepository = fixture.LocalRepositoryFixture.Repository;
        localRepository.Lookup(newCommit.Sha).ShouldBeNull();

        CreateMutator().Fetch(fixture.LocalRepositoryFixture.RepositoryPath, "origin", [], new AuthenticationInfo());

        localRepository.Lookup(newCommit.Sha).ShouldNotBeNull();
    }

    [Test]
    public void CloneCreatesRepositoryWithoutCheckingOutTheWorkingDirectory()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        var targetPath = FileSystemHelper.Path.GetRepositoryTempPath();

        try
        {
            CreateMutator().Clone(fixture.RepositoryPath, targetPath, new AuthenticationInfo());

            using var clonedRepository = new TestRepository(targetPath);
            clonedRepository.Head.Tip.ShouldNotBeNull();
            Directory.EnumerateFileSystemEntries(targetPath)
                .Where(entry => FileSystemHelper.Path.GetFileName(entry) != ".git")
                .ShouldBeEmpty();
        }
        finally
        {
            FileSystemHelper.Directory.DeleteDirectory(targetPath);
        }
    }

    [Test]
    public void CloneOfMissingRepositoryThrows()
    {
        var missingSource = FileSystemHelper.Path.Combine(FileSystemHelper.Path.GetTempPathLegacy(), "gitversion-does-not-exist");
        var targetPath = FileSystemHelper.Path.GetRepositoryTempPath();

        Should.Throw<InvalidOperationException>(() => CreateMutator().Clone(missingSource, targetPath, new AuthenticationInfo()));
    }

    [Test]
    public void ListRemoteReferencesReturnsBranchTips()
    {
        using var fixture = new RemoteRepositoryFixture();
        var remoteTipSha = fixture.Repository.Head.Tip.Sha;

        var references = CreateMutator().ListRemoteReferences(fixture.LocalRepositoryFixture.RepositoryPath, "origin", new AuthenticationInfo());

        references.Single(r => r.CanonicalName == "refs/heads/main").TargetSha.ShouldBe(remoteTipSha);
        references.ShouldNotContain(r => r.CanonicalName == "HEAD");
    }
}
