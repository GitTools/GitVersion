using System;
using System.IO;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class FileSystemTests
{
    string workDirectory;
    string gitDirectory;

    [SetUp]
    public void CreateTemporaryRepository()
    {
        workDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        gitDirectory = Repository.Init(workDirectory);

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
        Assert.AreEqual(gitDirectory, Repository.Discover(workDirectory));
    }

    [Test]
    public void From_WorkingDirectory_Parent()
    {
        var parentDirectory = Directory.GetParent(workDirectory).FullName;
        Assert.Null(Repository.Discover(parentDirectory));
    }

    [Test]
    public void From_GitDirectory()
    {
        Assert.AreEqual(gitDirectory, Repository.Discover(gitDirectory));
    }

    [Test]
    public void From_RefsDirectory()
    {
        var refsDirectory = Path.Combine(gitDirectory, "refs");
        Assert.AreEqual(gitDirectory, Repository.Discover(refsDirectory));
    }
}
