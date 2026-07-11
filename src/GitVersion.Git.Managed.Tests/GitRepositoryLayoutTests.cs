namespace GitVersion.Git.Managed.Tests;

[TestFixture]
public class GitRepositoryLayoutTests
{
    [Test]
    public void DiscoversARepositoryFromItsRootAndFromANestedDirectory()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("sub/dir/file.txt", "content\n");
        repository.Commit("a commit");

        foreach (var startPath in new[] { repository.RepositoryPath, Path.Combine(repository.RepositoryPath, "sub", "dir") })
        {
            var layout = GitRepositoryLayout.Discover(startPath);

            layout.GitDirectory.ShouldBe(Path.GetFullPath(repository.GitDirectory));
            layout.CommonDirectory.ShouldBe(layout.GitDirectory);
            layout.WorkingDirectory.ShouldBe(Path.GetFullPath(repository.RepositoryPath));
            layout.ObjectsDirectory.ShouldBe(Path.GetFullPath(repository.ObjectsDirectory));
            layout.IsShallow.ShouldBeFalse();
        }
    }

    [Test]
    public void ReturnsNullOutsideOfARepository()
    {
        using var directory = new TempDirectory();
        Directory.CreateDirectory(directory.FullPath);

        GitRepositoryLayout.TryDiscover(directory.FullPath).ShouldBeNull();
        Should.Throw<GitObjectStoreException>(() => GitRepositoryLayout.Discover(directory.FullPath));
    }

    [Test]
    public void ResolvesLinkedWorktrees()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("file.txt", "content\n");
        var sha = repository.Commit("a commit");

        using var worktree = new TempDirectory();
        repository.Run("worktree", "add", "-q", "-b", "wt-branch", worktree.FullPath);

        var layout = GitRepositoryLayout.Discover(worktree.FullPath);

        layout.WorkingDirectory.ShouldBe(Path.GetFullPath(worktree.FullPath));
        layout.GitDirectory.ShouldContain(Path.Combine(".git", "worktrees"));
        layout.CommonDirectory.ShouldBe(Path.GetFullPath(repository.GitDirectory));

        // HEAD is per-worktree; the refs are shared with the main repository.
        var store = layout.CreateReferenceStore();
        var head = store.GetHead();
        head.ShouldNotBeNull();
        head.SymbolicTargetName.ShouldBe("refs/heads/wt-branch");
        store.ResolveToObjectId("HEAD").ShouldBe(GitObjectId.Parse(sha));
        store.EnumerateReferences("refs/heads/")
            .Select(reference => reference.CanonicalName)
            .ShouldBe(["refs/heads/main", "refs/heads/wt-branch"]);

        using var objectStore = layout.CreateObjectStore();
        objectStore.GetCommit(GitObjectId.Parse(sha)).Message.ShouldBe("a commit\n");
    }

    [Test]
    public void DetectsShallowClones()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("file.txt", "one\n");
        repository.Commit("first");
        repository.WriteFile("file.txt", "two\n");
        var tipSha = repository.Commit("second");

        using var clone = new TempDirectory();
        repository.Run("clone", "-q", "--depth", "1", "file://" + repository.RepositoryPath, clone.FullPath);

        var layout = GitRepositoryLayout.Discover(clone.FullPath);

        layout.IsShallow.ShouldBeTrue();
        var shallowCommits = layout.ReadShallowCommits();
        shallowCommits.ShouldHaveSingleItem().ShouldBe(GitObjectId.Parse(tipSha));

        // The boundary commit object still records its parent (the shallow file, not the
        // object, cuts the history), but the parent object is absent from the clone.
        using var objectStore = layout.CreateObjectStore();
        var boundary = objectStore.GetCommit(shallowCommits[0]);
        boundary.Parents.ShouldHaveSingleItem();
        objectStore.TryGetObject(boundary.Parents[0], "commit", out _).ShouldBeFalse();
    }

    [Test]
    public void DiscoversABareRepository()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("file.txt", "content\n");
        var sha = repository.Commit("a commit");

        using var bare = new TempDirectory();
        repository.Run("clone", "-q", "--bare", "file://" + repository.RepositoryPath, bare.FullPath);

        var layout = GitRepositoryLayout.Discover(bare.FullPath);

        layout.WorkingDirectory.ShouldBeNull();
        layout.GitDirectory.ShouldBe(Path.GetFullPath(bare.FullPath));
        layout.CommonDirectory.ShouldBe(layout.GitDirectory);

        layout.CreateReferenceStore().ResolveToObjectId("refs/heads/main").ShouldBe(GitObjectId.Parse(sha));
    }

    [Test]
    public void ThrowsForReftableRepositories()
    {
        using var directory = new TempDirectory();

        using var repository = new GitTestRepository();

        try
        {
            repository.Run("init", "-q", "--ref-format=reftable", "-b", "main", directory.FullPath);
        }
        catch (InvalidOperationException)
        {
            Assert.Ignore("The installed git version does not support the reftable ref format.");
        }

        Should.Throw<NotSupportedException>(() => GitRepositoryLayout.Discover(directory.FullPath));
    }
}
