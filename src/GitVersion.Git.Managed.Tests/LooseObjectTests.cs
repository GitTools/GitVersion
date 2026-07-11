namespace GitVersion.Git.Managed.Tests;

[TestFixture]
public class LooseObjectTests
{
    [Test]
    public void ReadsACommitFromLooseObjects()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("file.txt", "hello\n");
        var firstSha = repository.Commit("first commit");
        repository.WriteFile("file.txt", "hello world\n");
        var secondSha = repository.Commit("second commit");

        using var store = repository.OpenObjectStore();
        var commit = store.GetCommit(GitObjectId.Parse(secondSha));

        commit.Sha.ToString().ShouldBe(secondSha);
        commit.Tree.ToString().ShouldBe(repository.RevParse("HEAD^{tree}"));
        commit.Parents.Count.ShouldBe(1);
        commit.Parents[0].ToString().ShouldBe(firstSha);
        commit.Message.ShouldBe("second commit\n");

        commit.Author.Name.ShouldBe(GitTestRepository.AuthorName);
        commit.Author.Email.ShouldBe(GitTestRepository.AuthorEmail);
        commit.Committer.Name.ShouldBe(GitTestRepository.CommitterName);
        commit.Committer.Email.ShouldBe(GitTestRepository.CommitterEmail);
    }

    [Test]
    public void ReadsARootCommitWithoutParents()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("file.txt", "hello\n");
        var sha = repository.Commit("initial");

        using var store = repository.OpenObjectStore();
        var commit = store.GetCommit(GitObjectId.Parse(sha));

        commit.Parents.ShouldBeEmpty();
        commit.Message.ShouldBe("initial\n");
    }

    [Test]
    public void CommitMatchesGitLogOutput()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("file.txt", "content\n");
        var sha = repository.Commit("subject line\n\nbody line one\nbody line two");

        using var store = repository.OpenObjectStore();
        var commit = store.GetCommit(GitObjectId.Parse(sha));

        var expected = repository
            .Run("log", "-1", "--format=%an%n%ae%n%at%n%cn%n%ce%n%ct", sha)
            .Split('\n');

        commit.Author.Name.ShouldBe(expected[0]);
        commit.Author.Email.ShouldBe(expected[1]);
        commit.Author.When.ToUnixTimeSeconds().ShouldBe(long.Parse(expected[2]));
        commit.Committer.Name.ShouldBe(expected[3]);
        commit.Committer.Email.ShouldBe(expected[4]);
        commit.Committer.When.ToUnixTimeSeconds().ShouldBe(long.Parse(expected[5]));

        commit.Author.When.Offset.ShouldBe(TimeSpan.FromHours(2));
        commit.Author.When.ShouldBe(repository.CurrentDate);
        commit.Message.ShouldBe("subject line\n\nbody line one\nbody line two\n");
    }

    [Test]
    public void CommitMessageMatchesGitCatFileOutput()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("file.txt", "content\n");
        var sha = repository.Commit("a message with special characters: äöü — ✓");

        using var store = repository.OpenObjectStore();
        var commit = store.GetCommit(GitObjectId.Parse(sha));

        var raw = repository.Run("cat-file", "commit", sha);
        var expectedMessage = raw[(raw.IndexOf("\n\n", StringComparison.Ordinal) + 2)..];

        commit.Message.TrimEnd('\n').ShouldBe(expectedMessage);
    }

    [Test]
    public void ReadsACommitMessageHonoringTheEncodingHeader()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("file.txt", "content\n");
        var messageBytes = Encoding.Latin1.GetBytes("café à la crème\n");
        var sha = repository.CommitWithMessageBytes(messageBytes, "ISO-8859-1");

        repository.Run("cat-file", "commit", sha).ShouldContain("encoding ISO-8859-1");

        using var store = repository.OpenObjectStore();
        var commit = store.GetCommit(GitObjectId.Parse(sha));

        commit.Message.ShouldBe("café à la crème\n");
    }

    [Test]
    public void FallsBackToLatin1ForInvalidUtf8WithoutAnEncodingHeader()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("file.txt", "content\n");
        var messageBytes = Encoding.Latin1.GetBytes("café\n");
        var sha = repository.CommitWithMessageBytes(messageBytes);

        using var store = repository.OpenObjectStore();
        var commit = store.GetCommit(GitObjectId.Parse(sha));

        commit.Message.ShouldBe("café\n");
    }

    [Test]
    public void ReadsABlobFromLooseObjects()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("file.txt", "blob content\n");
        repository.Commit("add file");

        using var store = repository.OpenObjectStore();
        using var blob = store.GetBlob(repository.ResolveId("HEAD:file.txt"));
        using var reader = new StreamReader(blob);

        reader.ReadToEnd().ShouldBe("blob content\n");
    }

    [Test]
    public void ReportsTheObjectTypeWhenItIsNotKnownUpFront()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("file.txt", "content\n");
        var sha = repository.Commit("a commit");

        using var store = repository.OpenObjectStore();
        store.TryGetObject(GitObjectId.Parse(sha), out var stream, out var objectType).ShouldBeTrue();

        using (stream)
        {
            objectType.ShouldBe("commit");
        }
    }

    [Test]
    public void DoesNotFindAMissingObject()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("file.txt", "content\n");
        repository.Commit("a commit");

        var missing = GitObjectId.Parse("0123456789abcdef0123456789abcdef01234567");

        using var store = repository.OpenObjectStore();
        store.TryGetObject(missing, "commit", out _).ShouldBeFalse();

        var exception = Should.Throw<GitObjectStoreException>(() => store.GetCommit(missing));
        exception.ObjectNotFound.ShouldBeTrue();
    }

    [Test]
    public void DoesNotFindAnObjectWhenRequestedWithTheWrongType()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("file.txt", "content\n");
        var sha = repository.Commit("a commit");

        using var store = repository.OpenObjectStore();
        store.TryGetObject(GitObjectId.Parse(sha), "blob", out _).ShouldBeFalse();
    }

    [Test]
    public void ReadsObjectsFromAnAlternateObjectDatabase()
    {
        using var upstream = new GitTestRepository();
        upstream.WriteFile("file.txt", "shared content\n");
        var sha = upstream.Commit("shared commit");

        using var dependent = new GitTestRepository();
        var infoDirectory = Path.Combine(dependent.ObjectsDirectory, "info");
        Directory.CreateDirectory(infoDirectory);
        File.WriteAllText(Path.Combine(infoDirectory, "alternates"), upstream.ObjectsDirectory + "\n");

        using var store = dependent.OpenObjectStore();
        var commit = store.GetCommit(GitObjectId.Parse(sha));

        commit.Message.ShouldBe("shared commit\n");
    }
}
