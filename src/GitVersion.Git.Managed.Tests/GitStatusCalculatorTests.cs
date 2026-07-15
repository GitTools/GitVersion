using LibGit2Sharp;

namespace GitVersion.Git.Managed.Tests;

/// <summary>
/// Validates <see cref="GitStatusCalculator"/> against the exact expression the
/// LibGit2Sharp adapter uses for <c>IGitRepository.UncommittedChangesCount</c>.
/// </summary>
[TestFixture]
public class GitStatusCalculatorTests
{
    [Test]
    public void ACleanWorkingDirectoryHasNoChanges()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("a.txt", "one\n");
        repository.WriteFile("sub/b.txt", "two\n");
        repository.Commit("commit");

        AssertParity(repository, expectedCount: 0);
    }

    [Test]
    public void CountsModifiedStagedDeletedAndUntrackedPathsOnce()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("modified.txt", "one\n");
        repository.WriteFile("both.txt", "one\n");
        repository.WriteFile("staged.txt", "one\n");
        repository.WriteFile("deleted.txt", "one\n");
        repository.WriteFile("removed.txt", "one\n");
        repository.Commit("commit");

        repository.WriteFile("modified.txt", "two\n");            // modified, unstaged
        repository.WriteFile("staged.txt", "two\n");
        repository.Run("add", "staged.txt");                       // modified, staged
        repository.WriteFile("both.txt", "two\n");
        repository.Run("add", "both.txt");
        repository.WriteFile("both.txt", "three\n");               // staged and modified again
        File.Delete(Path.Combine(repository.RepositoryPath, "deleted.txt")); // deleted, unstaged
        repository.Run("rm", "-q", "removed.txt");                 // deleted, staged
        repository.WriteFile("untracked.txt", "new\n");            // untracked
        repository.WriteFile("sub/untracked.txt", "new\n");        // untracked in new directory

        AssertParity(repository, expectedCount: 7);
    }

    [Test]
    public void ContentRestoredToTheCommittedStateIsClean()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("a.txt", "one\n");
        repository.Commit("commit");

        // Touch the file (mtime changes, content identical): requires re-hashing to prove clean.
        repository.WriteFile("a.txt", "one\n");

        AssertParity(repository, expectedCount: 0);
    }

    [Test]
    public void RespectsGitIgnoreRules()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile(".gitignore", "*.log\nbuild/\n!important.log\n/anchored.txt\n");
        repository.WriteFile("sub/.gitignore", "local-*\n");
        repository.WriteFile("a.txt", "one\n");
        repository.Commit("commit");

        repository.WriteFile("noise.log", "ignored\n");            // ignored by *.log
        repository.WriteFile("important.log", "kept\n");           // re-included by negation
        repository.WriteFile("build/out.txt", "ignored\n");        // ignored directory
        repository.WriteFile("anchored.txt", "ignored\n");         // anchored to root
        repository.WriteFile("sub/anchored.txt", "not anchored\n");// same name below root is not ignored
        repository.WriteFile("sub/local-cache", "ignored\n");      // nested .gitignore
        repository.WriteFile("sub/tracked-new.txt", "new\n");      // untracked

        AssertParity(repository, expectedCount: 3); // important.log, sub/anchored.txt, sub/tracked-new.txt
    }

    [Test]
    public void RespectsInfoExclude()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("a.txt", "one\n");
        repository.Commit("commit");

        Directory.CreateDirectory(Path.Combine(repository.GitDirectory, "info"));
        File.WriteAllText(Path.Combine(repository.GitDirectory, "info", "exclude"), "*.tmp\n");

        repository.WriteFile("scratch.tmp", "ignored\n");
        repository.WriteFile("kept.txt", "untracked\n");

        AssertParity(repository, expectedCount: 1);
    }

    [Test]
    public void HonorsCoreIgnoreCaseForIgnorePatterns()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile(".gitignore", "BIN/\n*.LOG\n");
        repository.Commit("commit");
        repository.Run("config", "core.ignorecase", "true");

        repository.WriteFile("bin/out.txt", "ignored\n");   // ignored by BIN/ under ignorecase
        repository.WriteFile("noise.log", "ignored\n");     // ignored by *.LOG under ignorecase
        repository.WriteFile("kept.txt", "untracked\n");

        AssertParity(repository, expectedCount: 1);
    }

    [Test]
    public void MatchesIgnorePatternsCaseSensitivelyWhenIgnoreCaseIsOff()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile(".gitignore", "BIN/\n*.LOG\n");
        repository.Commit("commit");
        repository.Run("config", "core.ignorecase", "false");

        repository.WriteFile("bin/out.txt", "kept\n");
        repository.WriteFile("noise.log", "kept\n");
        repository.WriteFile("kept.txt", "untracked\n");

        AssertParity(repository, expectedCount: 3);
    }

    [Test]
    public void CountsExecutableBitChanges()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Ignore("The executable bit does not exist on Windows.");
            return;
        }

        using var repository = new GitTestRepository();
        repository.WriteFile("script.sh", "#!/bin/sh\n");
        repository.Commit("commit");

        var scriptPath = Path.Combine(repository.RepositoryPath, "script.sh");
        File.SetUnixFileMode(scriptPath, File.GetUnixFileMode(scriptPath) | UnixFileMode.UserExecute);

        AssertParity(repository, expectedCount: 1);
    }

    [Test]
    public void CountsUntrackedFilesInAnEmptyRepository()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("untracked.txt", "new\n");
        repository.WriteFile("also-untracked.txt", "new\n");

        AssertParity(repository, expectedCount: 2);
    }

    [Test]
    public void WorksAgainstAnIndexInVersion4Format()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("a.txt", "one\n");
        repository.WriteFile("sub/b.txt", "two\n");
        repository.Commit("commit");
        repository.Run("update-index", "--index-version=4");

        repository.WriteFile("a.txt", "two\n");
        repository.WriteFile("untracked.txt", "new\n");

        AssertParity(repository, expectedCount: 2);
    }

    private static void AssertParity(GitTestRepository repository, int expectedCount)
    {
        var actual = ManagedCount(repository);
        var expected = LibGit2Count(repository);

        actual.ShouldBe(expected, "managed count should match the libgit2 adapter");
        actual.ShouldBe(expectedCount, "scenario expectation");
    }

    private static int ManagedCount(GitTestRepository repository)
    {
        var layout = GitRepositoryLayout.Discover(repository.RepositoryPath);
        using var store = layout.CreateObjectStore();
        var calculator = new GitStatusCalculator(layout, store);

        return layout.CreateReferenceStore().ResolveToObjectId("HEAD") is { } headId
            ? calculator.CountUncommittedChanges(store.GetCommit(headId).Tree)
            : calculator.CountChangesInEmptyRepository();
    }

    private static int LibGit2Count(GitTestRepository repository)
    {
        // The exact expression from GitVersion.LibGit2Sharp's GetUncommittedChangesCountInternal.
        using var libgit2 = new Repository(repository.RepositoryPath);

        if (libgit2.Head?.Tip == null)
        {
            var status = libgit2.RetrieveStatus();
            return status.Untracked.Count() + status.Staged.Count();
        }

        return libgit2.Diff
            .Compare<TreeChanges>(libgit2.Head.Tip.Tree, DiffTargets.Index | DiffTargets.WorkingDirectory)
            .Count;
    }
}
