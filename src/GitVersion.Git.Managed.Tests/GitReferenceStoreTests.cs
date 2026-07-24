namespace GitVersion.Git.Managed.Tests;

[TestFixture]
public class GitReferenceStoreTests
{
    [Test]
    public void ResolvesALooseBranchToItsCommit()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("file.txt", "content\n");
        var sha = repository.Commit("a commit");

        var store = repository.OpenReferenceStore();
        var reference = store.GetReference("refs/heads/main");

        reference.ShouldNotBeNull();
        reference.IsSymbolic.ShouldBeFalse();
        reference.ObjectId.ShouldBe(GitObjectId.Parse(sha));
        store.ResolveToObjectId("refs/heads/main").ShouldBe(GitObjectId.Parse(sha));
    }

    [Test]
    public void EnumerationMatchesGitForEachRef()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("file.txt", "content\n");
        repository.Commit("first");
        repository.Run("branch", "feature/a");
        repository.Run("tag", "lightweight");
        repository.Run("tag", "-a", "v1.0.0", "-m", "annotated");
        repository.WriteFile("file.txt", "more content\n");
        repository.Commit("second");

        var store = repository.OpenReferenceStore();
        var actual = store.EnumerateReferences()
            .Select(reference => $"{reference.CanonicalName} {reference.ObjectId}")
            .ToList();

        var expected = repository
            .Run("for-each-ref", "--format=%(refname) %(objectname)")
            .Split('\n');

        actual.ShouldBe(expected);
    }

    [Test]
    public void EnumerationSkipsTransientLockFiles()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("file.txt", "content\n");
        var sha = repository.Commit("a commit");

        // Simulate git being in the middle of updating a reference.
        File.WriteAllText(Path.Combine(repository.GitDirectory, "refs", "heads", "main.lock"), sha + "\n");

        var references = repository.OpenReferenceStore().EnumerateReferences().ToList();

        references.ShouldNotContain(reference => reference.CanonicalName.EndsWith(".lock"));
        references.ShouldHaveSingleItem().CanonicalName.ShouldBe("refs/heads/main");
    }

    [Test]
    public void ReadsPackedRefsIncludingPeeledTargets()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("file.txt", "content\n");
        var commitSha = repository.Commit("a commit");
        repository.Run("tag", "-a", "v1.0.0", "-m", "annotated");
        var tagSha = repository.RevParse("v1.0.0");

        repository.Run("pack-refs", "--all", "--prune");
        File.Exists(Path.Combine(repository.GitDirectory, "refs", "heads", "main")).ShouldBeFalse();

        var store = repository.OpenReferenceStore();

        store.ResolveToObjectId("refs/heads/main").ShouldBe(GitObjectId.Parse(commitSha));

        var tag = store.GetReference("refs/tags/v1.0.0");
        tag.ShouldNotBeNull();
        tag.ObjectId.ShouldBe(GitObjectId.Parse(tagSha));
        tag.PeeledObjectId.ShouldBe(GitObjectId.Parse(repository.RevParse("v1.0.0^{}")));
    }

    [Test]
    public void LooseReferencesWinOverPackedReferences()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("file.txt", "content\n");
        repository.Commit("first");
        repository.Run("pack-refs", "--all", "--prune");

        repository.WriteFile("file.txt", "more content\n");
        var newSha = repository.Commit("second");

        var store = repository.OpenReferenceStore();

        store.ResolveToObjectId("refs/heads/main").ShouldBe(GitObjectId.Parse(newSha));
        store.EnumerateReferences("refs/heads/")
            .ShouldHaveSingleItem()
            .ObjectId.ShouldBe(GitObjectId.Parse(newSha));
    }

    [Test]
    public void HeadIsSymbolicWhenABranchIsCheckedOut()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("file.txt", "content\n");
        var sha = repository.Commit("a commit");

        var store = repository.OpenReferenceStore();
        var head = store.GetHead();

        head.ShouldNotBeNull();
        head.IsSymbolic.ShouldBeTrue();
        head.SymbolicTargetName.ShouldBe("refs/heads/main");

        var resolved = store.Resolve("HEAD");
        resolved.ShouldNotBeNull();
        resolved.CanonicalName.ShouldBe("refs/heads/main");
        resolved.ObjectId.ShouldBe(GitObjectId.Parse(sha));
    }

    [Test]
    public void HeadIsDirectWhenDetached()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("file.txt", "content\n");
        var sha = repository.Commit("a commit");
        repository.Run("checkout", "-q", "--detach");

        var head = repository.OpenReferenceStore().GetHead();

        head.ShouldNotBeNull();
        head.IsSymbolic.ShouldBeFalse();
        head.ObjectId.ShouldBe(GitObjectId.Parse(sha));
    }

    [Test]
    public void HeadOfAnUnbornBranchResolvesToNull()
    {
        using var repository = new GitTestRepository();

        var store = repository.OpenReferenceStore();

        store.GetHead().ShouldNotBeNull().IsSymbolic.ShouldBeTrue();
        store.Resolve("HEAD").ShouldBeNull();
        store.ResolveToObjectId("HEAD").ShouldBeNull();
    }

    [Test]
    public void FollowsChainsOfSymbolicReferences()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("file.txt", "content\n");
        var sha = repository.Commit("a commit");
        repository.Run("symbolic-ref", "refs/sym/one", "refs/heads/main");
        repository.Run("symbolic-ref", "refs/sym/two", "refs/sym/one");

        var store = repository.OpenReferenceStore();
        var resolved = store.Resolve("refs/sym/two");

        resolved.ShouldNotBeNull();
        resolved.CanonicalName.ShouldBe("refs/heads/main");
        resolved.ObjectId.ShouldBe(GitObjectId.Parse(sha));
    }

    [Test]
    public void ReturnsNullForAMissingReference()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("file.txt", "content\n");
        repository.Commit("a commit");

        var store = repository.OpenReferenceStore();

        store.GetReference("refs/heads/does-not-exist").ShouldBeNull();
        store.Resolve("refs/heads/does-not-exist").ShouldBeNull();
    }

    [Test]
    public void EnumerationCanBeFilteredByPrefix()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("file.txt", "content\n");
        repository.Commit("a commit");
        repository.Run("branch", "feature/a");
        repository.Run("tag", "v1.0.0");
        repository.Run("tag", "v2.0.0");

        var store = repository.OpenReferenceStore();

        store.EnumerateReferences("refs/tags/")
            .Select(reference => reference.CanonicalName)
            .ShouldBe(["refs/tags/v1.0.0", "refs/tags/v2.0.0"]);

        store.EnumerateReferences("refs/heads/")
            .Select(reference => reference.CanonicalName)
            .ShouldBe(["refs/heads/feature/a", "refs/heads/main"]);
    }

    [Test]
    public void ResolvedHeadCanBeReadFromTheObjectStore()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("file.txt", "content\n");
        repository.Commit("readable through refs");

        var store = repository.OpenReferenceStore();
        var headId = store.ResolveToObjectId("HEAD");

        headId.ShouldNotBeNull();

        using var objectStore = repository.OpenObjectStore();
        objectStore.GetCommit(headId.Value).Message.ShouldBe("readable through refs\n");
    }
}
