using System;
using System.IO;

using LibGit2Sharp;

using NUnit.Framework;

[TestFixture]
public class GitVersionTaskDirectoryTests
{
    string gitDirectory;
    VersionAndBranchFinder versionAndBranchFinder;
    string workDirectory;


    [SetUp]
    public void CreateTemporaryRepository()
    {
        this.workDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        this.gitDirectory = Repository.Init(this.workDirectory)
            .TrimEnd(Path.DirectorySeparatorChar);
        this.versionAndBranchFinder = new VersionAndBranchFinder(new TestFileSystem());
        Assert.NotNull(this.gitDirectory);
    }


    [TearDown]
    public void Cleanup()
    {
        Directory.Delete(this.workDirectory, true);
    }


    [Test]
    public void Finds_GitDirectory()
    {
        try
        {
            this.versionAndBranchFinder.GetVersion(this.workDirectory, null, true);
        }
        catch (Exception ex)
        {
            // `RepositoryNotFoundException` means that it couldn't find the .git directory,
            // any other exception means that the .git was found but there was some other issue that this test doesn't care about.
            Assert.IsNotAssignableFrom<RepositoryNotFoundException>(ex);
        }
    }


    [Test]
    public void Finds_GitDirectory_In_Parent()
    {
        var childDir = Path.Combine(this.workDirectory, "child");
        Directory.CreateDirectory(childDir);

        try
        {
            this.versionAndBranchFinder.GetVersion(childDir, null, true);
        }
        catch (Exception ex)
        {
            // `RepositoryNotFoundException` means that it couldn't find the .git directory,
            // any other exception means that the .git was found but there was some other issue that this test doesn't care about.
            Assert.IsNotAssignableFrom<RepositoryNotFoundException>(ex);
        }
    }
}