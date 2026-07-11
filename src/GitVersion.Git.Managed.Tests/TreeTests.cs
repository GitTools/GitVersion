namespace GitVersion.Git.Managed.Tests;

[TestFixture]
public class TreeTests
{
    [Test]
    public void ReadsTheRootTreeEntriesMatchingGitLsTree()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("a.txt", "a\n");
        repository.WriteFile("b.txt", "b\n");
        repository.WriteFile("sub/c.txt", "c\n");
        repository.Commit("add files");

        using var store = repository.OpenObjectStore();
        var treeId = repository.ResolveId("HEAD^{tree}");
        var tree = store.GetTree(treeId);

        tree.Sha.ShouldBe(treeId);

        // git ls-tree prints: <mode> SP <type> SP <sha> TAB <name>
        var expectedEntries = repository
            .Run("ls-tree", treeId.ToString())
            .Split('\n')
            .Select(line =>
            {
                var parts = line.Split('\t');
                var meta = parts[0].Split(' ');
                return (Mode: meta[0], Type: meta[1], Sha: meta[2], Name: parts[1]);
            })
            .ToList();

        tree.Entries.Count.ShouldBe(expectedEntries.Count);

        for (var i = 0; i < expectedEntries.Count; i++)
        {
            var entry = tree.Entries[i];
            var expected = expectedEntries[i];

            entry.Name.ShouldBe(expected.Name);
            entry.Mode.PadLeft(6, '0').ShouldBe(expected.Mode);
            entry.Sha.ToString().ShouldBe(expected.Sha);
            entry.IsTree.ShouldBe(expected.Type == "tree");
        }
    }

    [Test]
    public void ReadsANestedTree()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("sub/c.txt", "c\n");
        repository.Commit("add nested file");

        using var store = repository.OpenObjectStore();
        var rootTree = store.GetTree(repository.ResolveId("HEAD^{tree}"));

        var subEntry = rootTree.FindEntry("sub");
        subEntry.ShouldNotBeNull();
        subEntry.IsTree.ShouldBeTrue();

        var subTree = store.GetTree(subEntry.Sha);
        subTree.Entries.Count.ShouldBe(1);
        subTree.Entries[0].Name.ShouldBe("c.txt");
        subTree.Entries[0].IsTree.ShouldBeFalse();
        subTree.Entries[0].Sha.ToString().ShouldBe(repository.RevParse("HEAD:sub/c.txt"));
    }

    [Test]
    public void FindsANodeUsingTheStreamingReader()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("a.txt", "a\n");
        repository.WriteFile("sub/c.txt", "c\n");
        repository.Commit("add files");

        using var store = repository.OpenObjectStore();
        var treeId = repository.ResolveId("HEAD^{tree}");

        using (var treeStream = store.GetObject(treeId, "tree"))
        {
            var node = GitTreeStreamingReader.FindNode(treeStream, "sub"u8);
            node.ToString().ShouldBe(repository.RevParse("HEAD:sub"));
        }

        using (var treeStream = store.GetObject(treeId, "tree"))
        {
            GitTreeStreamingReader.FindNode(treeStream, "missing"u8).ShouldBe(GitObjectId.Empty);
        }
    }
}
