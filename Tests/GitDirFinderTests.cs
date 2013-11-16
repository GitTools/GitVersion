using System;
using System.IO;
using GitFlowVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class GitDirFinderTests
{
    string workDirectory;
    string gitDirectory;

    [SetUp]
    public void CreateTemporaryRepository()
    {
        workDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        gitDirectory = Repository.Init(workDirectory)
                        .TrimEnd(new[] { Path.DirectorySeparatorChar });

        Assert.NotNull(gitDirectory);
    }

    [TearDown]
    public void Cleanup()
    {
        Directory.Delete(workDirectory, true);
    }

    [Test]
    public void From_WorkingDirectory()
    {
        Assert.AreEqual(gitDirectory, GitDirFinder.TreeWalkForGitDir(workDirectory));
    }

    [Test]
    public void From_WorkingDirectory_Parent()
    {
        var parentDirectory = Directory.GetParent(workDirectory).FullName;
        Assert.Null(GitDirFinder.TreeWalkForGitDir(parentDirectory));
    }

    [Test]
    public void From_GitDirectory()
    {
        Assert.AreEqual(gitDirectory, GitDirFinder.TreeWalkForGitDir(gitDirectory));
    }

    [Test]
    public void From_RefsDirectory()
    {
        var refsDirectory = Path.Combine(gitDirectory, "refs");
        Assert.AreEqual(gitDirectory, GitDirFinder.TreeWalkForGitDir(refsDirectory));
    }
}
