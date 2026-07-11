using LibGit2Sharp;

namespace GitVersion.Git.Managed.Tests;

/// <summary>
/// Validates <see cref="GitTreeDiff"/> against libgit2's default <c>TreeChanges</c> path
/// list, which is what GitVersion exposes as <c>ICommit.DiffPaths</c>.
/// </summary>
[TestFixture]
public class GitTreeDiffTests
{
    [Test]
    public void ChangedPathsMatchLibGit2ForEveryCommit()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("a.txt", "one\n");
        repository.WriteFile("sub/b.txt", "one\n");
        repository.WriteFile("sub/deep/c.txt", "one\n");
        repository.Commit("root commit");

        repository.WriteFile("a.txt", "two\n");
        repository.WriteFile("sub/new.txt", "new\n");
        repository.Commit("modify and add");

        File.Delete(Path.Combine(repository.RepositoryPath, "sub", "b.txt"));
        repository.Commit("delete nested file");

        repository.WriteFile("d.txt", "d\n");
        repository.Run("update-index", "--chmod=+x", "a.txt");
        repository.Commit("exec bit and new file");

        AssertDiffPathsParityForAllCommits(repository);
    }

    [Test]
    public void ChangedPathsMatchLibGit2WhenAFileBecomesADirectory()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("thing", "file\n");
        repository.WriteFile("z.txt", "z\n");
        repository.Commit("file");

        File.Delete(Path.Combine(repository.RepositoryPath, "thing"));
        repository.WriteFile("thing/inner.txt", "dir\n");
        repository.WriteFile("thing/other.txt", "dir\n");
        repository.Commit("becomes a directory");

        File.Delete(Path.Combine(repository.RepositoryPath, "thing", "inner.txt"));
        File.Delete(Path.Combine(repository.RepositoryPath, "thing", "other.txt"));
        Directory.Delete(Path.Combine(repository.RepositoryPath, "thing"));
        repository.WriteFile("thing", "file again\n");
        repository.Commit("becomes a file again");

        AssertDiffPathsParityForAllCommits(repository);
    }

    [Test]
    public void ChangedPathsOfMergeCommitsUseTheFirstParent()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("main.txt", "0\n");
        repository.Commit("main 0");
        repository.Run("checkout", "-q", "-b", "feature");
        repository.WriteFile("feature.txt", "1\n");
        repository.Commit("feature 1");
        repository.Run("checkout", "-q", "main");
        repository.WriteFile("main.txt", "1\n");
        repository.Commit("main 1");
        repository.Merge("feature");

        AssertDiffPathsParityForAllCommits(repository);
    }

    [Test]
    public void IdenticalTreesProduceNoChangedPaths()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("a.txt", "one\n");
        repository.Commit("commit");

        using var store = repository.OpenObjectStore();
        var tree = store.GetCommit(repository.ResolveId("HEAD")).Tree;

        new GitTreeDiff(store).GetChangedPaths(tree, tree).ShouldBeEmpty();
    }

    [Test]
    public void FlattenTreeListsAllBlobPaths()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("a.txt", "one\n");
        repository.WriteFile("sub/b.txt", "one\n");
        repository.WriteFile("sub/deep/c.txt", "one\n");
        repository.Commit("commit");

        using var store = repository.OpenObjectStore();
        var tree = store.GetCommit(repository.ResolveId("HEAD")).Tree;

        var files = new GitTreeDiff(store).FlattenTree(tree);

        files.Keys.Order(StringComparer.Ordinal).ShouldBe(["a.txt", "sub/b.txt", "sub/deep/c.txt"]);
        files["a.txt"].Sha.ToString().ShouldBe(repository.RevParse("HEAD:a.txt"));
    }

    private static void AssertDiffPathsParityForAllCommits(GitTestRepository repository)
    {
        using var store = repository.OpenObjectStore();
        var treeDiff = new GitTreeDiff(store);
        var walker = new GitRevisionWalker(store);

        var options = new GitRevisionWalkOptions();
        options.Include.Add(repository.ResolveId("HEAD"));

        using var libgit2 = new Repository(repository.RepositoryPath);

        foreach (var commit in walker.Walk(options))
        {
            var parentTree = commit.Parents.Count > 0
                ? store.GetCommit(commit.Parents[0]).Tree
                : (GitObjectId?)null;

            var actual = treeDiff.GetChangedPaths(parentTree, commit.Tree);

            // The adapter's DiffPaths: Compare<TreeChanges>(commit.Tree, firstParent?.Tree).Paths
            var libgit2Commit = libgit2.Lookup<Commit>(commit.Sha.ToString())!;
            var expected = libgit2.Diff
                .Compare<TreeChanges>(libgit2Commit.Tree, libgit2Commit.Parents.FirstOrDefault()?.Tree)
                .Select(element => element.Path)
                .ToList();

            actual.ShouldBe(expected, $"commit: {commit.Message.TrimEnd()}");
        }
    }
}
