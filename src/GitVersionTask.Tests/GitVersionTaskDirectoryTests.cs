using System;
using System.IO;
using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;
using GitVersionTask.Tests.Helpers;

namespace GitVersionTask.Tests
{
    [TestFixture]
    public class GitVersionTaskDirectoryTests : TestBase
    {
        ExecuteCore executeCore;
        string gitDirectory;
        string workDirectory;


        [SetUp]
        public void CreateTemporaryRepository()
        {
            workDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            gitDirectory = Repository.Init(workDirectory)
                .TrimEnd(Path.DirectorySeparatorChar);
            executeCore = new ExecuteCore(new TestFileSystem());
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
                executeCore.ExecuteGitVersion(null, null, null, null, true, workDirectory, null);
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
                executeCore.ExecuteGitVersion(null, null, null, null, true, childDir, null);
            }
            catch (Exception ex)
            {
                // TODO I think this test is wrong.. It throws a different exception
                // `RepositoryNotFoundException` means that it couldn't find the .git directory,
                // any other exception means that the .git was found but there was some other issue that this test doesn't care about.
                Assert.IsNotAssignableFrom<RepositoryNotFoundException>(ex);
            }
        }
    }
}