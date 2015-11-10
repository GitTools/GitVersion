using System;
using System.IO;
using GitVersion;
using GitVersion.Helpers;

using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class FileSystemTests
{
    string workDirectory;
    string gitDirectory;
    IFileSystem fileSystem;

    [SetUp]
    public void CreateTemporaryRepository()
    {
        this.fileSystem = new FileSystem();
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
        Assert.AreEqual(gitDirectory, fileSystem.TreeWalkForDotGitDir(workDirectory));
    }

    [Test]
    public void From_WorkingDirectory_Parent()
    {
        var parentDirectory = Directory.GetParent(workDirectory).FullName;
        Assert.Null(fileSystem.TreeWalkForDotGitDir(parentDirectory));
    }

    [Test]
    public void From_GitDirectory()
    {
        Assert.AreEqual(gitDirectory, fileSystem.TreeWalkForDotGitDir(gitDirectory));
    }

    [Test]
    public void From_RefsDirectory()
    {
        var refsDirectory = Path.Combine(gitDirectory, "refs");
        Assert.AreEqual(gitDirectory, fileSystem.TreeWalkForDotGitDir(refsDirectory));
    }
}
