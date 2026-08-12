namespace GitVersion.Git.Managed.Tests;

[TestFixture]
public class MultiPackIndexTests
{
    [Test]
    public void ReadsObjectsFromMultiplePacksThroughTheMultiPackIndex()
    {
        using var repository = new GitTestRepository();

        repository.WriteFile("file.txt", "first\n");
        var firstSha = repository.Commit("first commit");
        repository.Run("repack", "-a", "-d", "-q");

        repository.WriteFile("file.txt", "second\n");
        var secondSha = repository.Commit("second commit");
        repository.Run("repack", "-d", "-q");

        repository.Run("multi-pack-index", "write");

        var packDirectory = Path.Combine(repository.ObjectsDirectory, "pack");
        File.Exists(Path.Combine(packDirectory, "multi-pack-index")).ShouldBeTrue();
        Directory.GetFiles(packDirectory, "*.pack").Length.ShouldBe(2);

        using var store = repository.OpenObjectStore();
        store.GetCommit(GitObjectId.Parse(firstSha)).Message.ShouldBe("first commit\n");

        var second = store.GetCommit(GitObjectId.Parse(secondSha));
        second.Message.ShouldBe("second commit\n");
        second.Parents[0].ToString().ShouldBe(firstSha);
    }

    [Test]
    public void TheMultiPackIndexReaderLooksUpObjectsAcrossPacks()
    {
        using var repository = new GitTestRepository();

        repository.WriteFile("file.txt", "first\n");
        var firstSha = repository.Commit("first commit");
        repository.Run("repack", "-a", "-d", "-q");

        repository.WriteFile("file.txt", "second\n");
        var secondSha = repository.Commit("second commit");
        repository.Run("repack", "-d", "-q");

        repository.Run("multi-pack-index", "write");

        using var stream = File.OpenRead(Path.Combine(repository.ObjectsDirectory, "pack", "multi-pack-index"));
        using var reader = new GitMultiPackIndexReader(stream);

        reader.PackNames.Count.ShouldBe(2);
        reader.PackNames.ShouldAllBe(name => name.StartsWith("pack-"));

        var first = reader.GetOffset(GitObjectId.Parse(firstSha));
        var second = reader.GetOffset(GitObjectId.Parse(secondSha));

        first.ShouldNotBeNull();
        second.ShouldNotBeNull();
        first.Value.PackIndex.ShouldNotBe(second.Value.PackIndex, "the two commits live in different packs");
        first.Value.Offset.ShouldBeGreaterThan(0);
        second.Value.Offset.ShouldBeGreaterThan(0);

        reader.GetOffset(GitObjectId.Parse("0123456789abcdef0123456789abcdef01234567")).ShouldBeNull();
    }

    [Test]
    public void ReadsLooseObjectsWhenAMultiPackIndexIsPresent()
    {
        using var repository = new GitTestRepository();

        repository.WriteFile("file.txt", "first\n");
        repository.Commit("first commit");
        repository.Run("repack", "-a", "-d", "-q");
        repository.Run("multi-pack-index", "write");

        repository.WriteFile("file.txt", "second\n");
        var looseSha = repository.Commit("loose commit");

        using var store = repository.OpenObjectStore();
        store.GetCommit(GitObjectId.Parse(looseSha)).Message.ShouldBe("loose commit\n");
    }
}
