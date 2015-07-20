using System;
using System.IO;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class GitVersionTaskDirectoryTests
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
    public void Finds_GitDirectory()
    {
        try
        {
            VersionAndBranchFinder.GetVersion(workDirectory, null, true, null);
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
        var childDir = Path.Combine(workDirectory, "child");
        Directory.CreateDirectory(childDir);

        try
        {
            VersionAndBranchFinder.GetVersion(childDir, null, true, null);
        }
        catch (Exception ex)
        {
            // `RepositoryNotFoundException` means that it couldn't find the .git directory,
            // any other exception means that the .git was found but there was some other issue that this test doesn't care about.
            Assert.IsNotAssignableFrom<RepositoryNotFoundException>(ex);
        }
    }
}
