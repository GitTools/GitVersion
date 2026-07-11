namespace GitVersion.Git.Managed.Tests;

[TestFixture]
public class PackedObjectTests
{
    [Test]
    public void ReadsCommitsFromAPackFile()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("file.txt", "hello\n");
        var firstSha = repository.Commit("first commit");
        repository.WriteFile("file.txt", "hello world\n");
        var secondSha = repository.Commit("second commit");

        repository.Run("gc", "-q");
        AssertHasNoLooseObjects(repository);

        using var store = repository.OpenObjectStore();
        var commit = store.GetCommit(GitObjectId.Parse(secondSha));

        commit.Tree.ToString().ShouldBe(repository.RevParse("HEAD^{tree}"));
        commit.Parents[0].ToString().ShouldBe(firstSha);
        commit.Message.ShouldBe("second commit\n");
        commit.Author.Name.ShouldBe(GitTestRepository.AuthorName);
        commit.Committer.When.ShouldBe(repository.CurrentDate);
    }

    [Test]
    public void ReadsDeltifiedObjectsFromAPackWithDeepDeltaChains()
    {
        using var repository = new GitTestRepository();
        var commits = CreateDeltaFriendlyHistory(repository);

        repository.Run("repack", "-a", "-d", "-q", "--depth=50", "--window=50");
        AssertHasNoLooseObjects(repository);

        AssertWholeHistoryIsReadable(repository, commits);
    }

    [Test]
    public void ReadsRefDeltaObjectsFromAPackFile()
    {
        using var repository = new GitTestRepository();
        var commits = CreateDeltaFriendlyHistory(repository);

        repository.Run("-c", "repack.useDeltaBaseOffset=false", "repack", "-a", "-d", "-q", "--depth=50", "--window=50");
        AssertHasNoLooseObjects(repository);

        AssertWholeHistoryIsReadable(repository, commits);
    }

    [Test]
    public void ReadsBlobsAndTreesFromAPackFile()
    {
        using var repository = new GitTestRepository();
        var expectedContent = new StringBuilder();
        for (var i = 0; i < 20; i++)
        {
            expectedContent.AppendLine($"line {i} of a delta friendly file with some repeating content");
            repository.WriteFile("file.txt", expectedContent.ToString());
            repository.Commit($"commit {i}");
        }

        repository.Run("repack", "-a", "-d", "-q", "--depth=50", "--window=50");

        using var store = repository.OpenObjectStore();
        using var blob = store.GetBlob(repository.ResolveId("HEAD:file.txt"));
        using var reader = new StreamReader(blob);
        reader.ReadToEnd().ShouldBe(expectedContent.ToString());

        var tree = store.GetTree(repository.ResolveId("HEAD^{tree}"));
        tree.Entries.Count.ShouldBe(1);
        tree.Entries[0].Name.ShouldBe("file.txt");
    }

    [Test]
    public void ReadsLooseObjectsCreatedAfterThePack()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("file.txt", "hello\n");
        var packedSha = repository.Commit("packed commit");
        repository.Run("gc", "-q");

        repository.WriteFile("file.txt", "hello again\n");
        var looseSha = repository.Commit("loose commit");

        using var store = repository.OpenObjectStore();
        store.GetCommit(GitObjectId.Parse(packedSha)).Message.ShouldBe("packed commit\n");
        store.GetCommit(GitObjectId.Parse(looseSha)).Message.ShouldBe("loose commit\n");
    }

    private static List<(string Sha, string Message)> CreateDeltaFriendlyHistory(GitTestRepository repository)
    {
        List<(string Sha, string Message)> commits = [];
        var content = new StringBuilder();

        for (var i = 0; i < 30; i++)
        {
            content.AppendLine($"line {i} of a delta friendly file with some repeating content");
            repository.WriteFile("file.txt", content.ToString());
            var message = $"commit {i}";
            commits.Add((repository.Commit(message), message));
        }

        return commits;
    }

    private static void AssertWholeHistoryIsReadable(GitTestRepository repository, List<(string Sha, string Message)> commits)
    {
        using var store = repository.OpenObjectStore();

        for (var i = 0; i < commits.Count; i++)
        {
            var commit = store.GetCommit(GitObjectId.Parse(commits[i].Sha));

            commit.Message.ShouldBe(commits[i].Message + "\n");
            commit.Parents.Count.ShouldBe(i == 0 ? 0 : 1);
            if (i > 0)
            {
                commit.Parents[0].ToString().ShouldBe(commits[i - 1].Sha);
            }

            using var blob = store.GetBlob(repository.ResolveId($"{commits[i].Sha}:file.txt"));
            using var reader = new StreamReader(blob);
            var lines = reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries);
            lines.Length.ShouldBe(i + 1);
        }
    }

    private static void AssertHasNoLooseObjects(GitTestRepository repository)
    {
        var looseObjects = Directory
            .EnumerateDirectories(repository.ObjectsDirectory)
            .Where(directory => Path.GetFileName(directory) is { Length: 2 })
            .SelectMany(Directory.EnumerateFiles);

        looseObjects.ShouldBeEmpty();
    }
}
