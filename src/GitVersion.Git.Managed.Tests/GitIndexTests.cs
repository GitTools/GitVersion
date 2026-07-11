namespace GitVersion.Git.Managed.Tests;

[TestFixture]
public class GitIndexTests
{
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void IndexEntriesMatchGitLsFiles(int indexVersion)
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("a.txt", "one\n");
        repository.WriteFile("sub/b.txt", "two\n");
        repository.WriteFile("sub/deep/c.txt", "three\n");
        repository.Commit("commit");
        repository.Run("update-index", "--chmod=+x", "a.txt");

        if (indexVersion == 3)
        {
            // Git only writes version 3 when an entry actually carries extended flags.
            repository.Run("update-index", "--skip-worktree", "sub/b.txt");
        }

        repository.Run("update-index", $"--index-version={indexVersion}");

        var index = GitIndex.Read(Path.Combine(repository.GitDirectory, "index"));

        index.Version.ShouldBe(indexVersion);

        // git ls-files --stage prints: <mode> SP <sha> SP <stage> TAB <path>
        var expected = repository
            .Run("ls-files", "--stage")
            .Split('\n')
            .Select(line =>
            {
                var parts = line.Split('\t');
                var meta = parts[0].Split(' ');
                return (Mode: Convert.ToUInt32(meta[0], 8), Sha: meta[1], Stage: int.Parse(meta[2]), Path: parts[1]);
            })
            .ToList();

        index.Entries.Count.ShouldBe(expected.Count);

        for (var i = 0; i < expected.Count; i++)
        {
            var entry = index.Entries[i];
            entry.Path.ShouldBe(expected[i].Path);
            entry.Mode.ShouldBe(expected[i].Mode);
            entry.ObjectId.ToString().ShouldBe(expected[i].Sha);
            entry.Stage.ShouldBe(expected[i].Stage);
        }

        index.Entries.Single(entry => entry.Path == "a.txt").IsExecutable.ShouldBeTrue();
        index.Entries.Single(entry => entry.Path == "sub/b.txt").IsExecutable.ShouldBeFalse();
    }

    [Test]
    public void ReadsStatDataUsedForCleanDetection()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("a.txt", "content\n");
        repository.Commit("commit");

        var index = GitIndex.Read(Path.Combine(repository.GitDirectory, "index"));
        var entry = index.Entries.ShouldHaveSingleItem();

        entry.Size.ShouldBe((uint)"content\n".Length);
        entry.ModificationTimeSeconds.ShouldBeGreaterThan(0u);
        entry.AssumeValid.ShouldBeFalse();
        entry.SkipWorktree.ShouldBeFalse();
        entry.IntentToAdd.ShouldBeFalse();
    }

    [Test]
    public void ReadsSkipWorktreeExtendedFlags()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("a.txt", "one\n");
        repository.WriteFile("b.txt", "two\n");
        repository.Commit("commit");
        repository.Run("update-index", "--skip-worktree", "a.txt");

        var index = GitIndex.Read(Path.Combine(repository.GitDirectory, "index"));

        index.Entries.Single(entry => entry.Path == "a.txt").SkipWorktree.ShouldBeTrue();
        index.Entries.Single(entry => entry.Path == "b.txt").SkipWorktree.ShouldBeFalse();
    }

    [Test]
    public void AMissingIndexFileYieldsAnEmptyIndex()
    {
        var index = GitIndex.Read(Path.Combine(Path.GetTempPath(), "does-not-exist", "index"));

        index.Entries.ShouldBeEmpty();
    }
}
