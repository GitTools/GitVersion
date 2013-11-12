using System;
using System.IO;
using GitFlowVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class GitDirFinderTests
{
    private string workDir;
    private string gitDir;

    [SetUp]
    public void CreateTemporaryRepository()
    {
        workDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        gitDir = Repository.Init(workDir)
                        .TrimEnd(new[] { Path.DirectorySeparatorChar });

        Assert.NotNull(gitDir);
    }

    [TearDown]
    public void Cleanup()
    {
        Directory.Delete(workDir, true);
    }

    [Test]
    public void From_Workdir()
    {
        Assert.AreEqual(gitDir, GitDirFinder.TreeWalkForGitDir(workDir));
    }

    [Test]
    public void From_Workdir_Parent()
    {
        string parentDir = Directory.GetParent(workDir).FullName;
        Assert.Null(GitDirFinder.TreeWalkForGitDir(parentDir));
    }

    [Test]
    public void From_GitDir()
    {
        Assert.AreEqual(gitDir, GitDirFinder.TreeWalkForGitDir(gitDir));
    }

    [Test]
    public void From_RefsDir()
    {
        string refsDir = Path.Combine(gitDir, "refs");
        Assert.AreEqual(gitDir, GitDirFinder.TreeWalkForGitDir(refsDir));
    }
}
