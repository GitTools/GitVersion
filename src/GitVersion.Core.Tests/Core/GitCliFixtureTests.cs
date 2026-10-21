using GitVersion.Testing.Extensions;
using GitVersion.Testing.Git;

namespace GitVersion.Tests;

[TestFixture]
public class GitCliFixtureTests
{
    [Test]
    public void LiveManagedReaderSeesGitReset()
    {
        using var fixture = new EmptyRepositoryFixture();
        var first = fixture.Repository.MakeACommit("first");
        var second = fixture.Repository.MakeACommit("second");
        using var reader = fixture.Repository.ToGitRepository();
        reader.Head.Tip.ShouldNotBeNull().Sha.ShouldBe(second.Sha);
        fixture.Repository.Run("reset", "--hard", first.Sha);
        reader.Head.Tip.ShouldNotBeNull().Sha.ShouldBe(first.Sha);
    }

    [Test]
    public void PullUsesTheSuppliedMergeSignature()
    {
        using var remote = new EmptyRepositoryFixture();
        remote.MakeACommit("base");
        using var local = remote.CloneRepository();
        local.MakeACommit("local");
        remote.MakeACommit("remote");
        var signature = new Signature("Merge Author", "merge@example.com", new DateTimeOffset(2023, 6, 1, 10, 0, 0, TimeSpan.Zero));
        Commands.Pull(local.Repository, signature, new());
        local.Repository.Head.Tip.Parents.Count().ShouldBe(2);
        local.Repository.Head.Tip.Author.ShouldBe(signature);
        local.Repository.Head.Tip.Committer.ShouldBe(signature);
    }

    [Test]
    public void BlobCreationDoesNotNormalizeLineEndings()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.Config.Set("core.autocrlf", true);
        byte[] bytes = [97, 13, 10, 98, 13, 10];
        using var content = new MemoryStream(bytes);
        var blob = fixture.Repository.ObjectDatabase.CreateBlob(content);
        var expected = System.Security.Cryptography.SHA1.HashData([.. Encoding.ASCII.GetBytes("blob 6\0"), .. bytes]);
        blob.Sha.ShouldBe(Convert.ToHexStringLower(expected));
    }

    [Test]
    public void BlobCreationPreservesNonUtf8Bytes()
    {
        using var fixture = new EmptyRepositoryFixture();
        byte[] bytes = [0, 128, 255, 13, 10];
        using var content = new MemoryStream(bytes);
        var blob = fixture.Repository.ObjectDatabase.CreateBlob(content);
        var expected = System.Security.Cryptography.SHA1.HashData([.. Encoding.ASCII.GetBytes("blob 5\0"), .. bytes]);
        blob.Sha.ShouldBe(Convert.ToHexStringLower(expected));
    }

    [Test]
    public void MissingObjectsReturnNullEvenForAFullObjectId()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit("initial");
        const string missing = "0123456789012345678901234567890123456789";
        fixture.Repository.Lookup(missing).ShouldBeNull();
        fixture.Repository.Lookup<Commit>(missing).ShouldBeNull();
    }

    [Test]
    public void UpdatingAReferenceToAShaKeepsItDirect()
    {
        using var fixture = new EmptyRepositoryFixture();
        var first = fixture.Repository.MakeACommit("first");
        fixture.Repository.CreateBranch("target");
        var second = fixture.Repository.MakeACommit("second");
        fixture.Repository.Refs.UpdateTarget(fixture.Repository.Refs["refs/heads/target"], second.Sha);
        fixture.Repository.Run("rev-parse", "target").Trim().ShouldBe(second.Sha);
        File.ReadAllText(Path.Combine(fixture.Repository.Info.Path, "refs", "heads", "target")).Trim().ShouldBe(second.Sha);
        fixture.Repository.Head.Tip.Sha.ShouldNotBe(first.Sha);
    }

    [Test]
    public void FetchIncludesObjectsReachableOnlyThroughPullRequestReferences()
    {
        using var remote = new EmptyRepositoryFixture();
        remote.MakeACommit("base");
        Commands.Checkout(remote.Repository, remote.Repository.Head.Tip);
        var pullRequest = remote.Repository.MakeACommit("PR-only commit");
        remote.Repository.Refs.Add("refs/pull/42/merge", pullRequest.Id);
        remote.Checkout("main");
        using var local = new EmptyRepositoryFixture();
        local.Repository.Network.Remotes.Add("origin", remote.RepositoryPath);
        Commands.Fetch(local.Repository, "origin", [], null, null);
        local.Repository.Lookup<Commit>(pullRequest.Sha).ShouldNotBeNull().MessageShort.ShouldBe("PR-only commit");
        local.Repository.Branches["origin/main"].ShouldNotBeNull().Tip.Sha.ShouldBe(remote.Repository.Head.Tip.Sha);
    }

    [Test]
    public void CommitTreePreservesParentOrderAndSignatures()
    {
        using var fixture = new EmptyRepositoryFixture();
        var first = fixture.Repository.MakeACommit("first");
        var second = fixture.Repository.MakeACommit("second");
        var author = new Signature("Test Author", "author@example.com", new DateTimeOffset(2023, 6, 1, 10, 0, 0, TimeSpan.FromHours(2)));
        var committer = author with { Name = "Test Committer", Email = "committer@example.com", When = author.When.AddMinutes(1) };
        var commit = fixture.Repository.ObjectDatabase.CreateCommit(author, committer, "synthetic merge", first.Tree, [second, first]);
        commit.Parents.Select(parent => parent.Sha).ShouldBe([second.Sha, first.Sha]);
        commit.Author.ShouldBe(author);
        commit.Committer.ShouldBe(committer);
        commit.Tree.Sha.ShouldBe(first.Tree.Sha);
    }
}
