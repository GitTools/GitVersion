using System;
using System.IO;
using GitVersion;
using GitVersion.Cache;
using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Logging;
using LibGit2Sharp;
using NUnit.Framework;
using GitVersionTask.Tests.Helpers;

namespace GitVersionTask.Tests
{
    [TestFixture]
    public class GitVersionTaskDirectoryTests : TestBase
    {
        private IGitVersionComputer gitVersionComputer;
        private string gitDirectory;
        private string workDirectory;

        [SetUp]
        public void CreateTemporaryRepository()
        {
            workDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            gitDirectory = Repository.Init(workDirectory)
                .TrimEnd(Path.DirectorySeparatorChar);

            var testFileSystem = new TestFileSystem();
            var log = new NullLog();
            var configFileLocator = new DefaultConfigFileLocator(testFileSystem, log);
            var gitVersionCache = new GitVersionCache(testFileSystem, log);

            var buildServerResolver = new BuildServerResolver(null, log);

            gitVersionComputer = new GitVersionComputer(testFileSystem, log, configFileLocator, buildServerResolver, gitVersionCache);
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
                var arguments = new Arguments { TargetPath = workDirectory, NoFetch = true };

                gitVersionComputer.ComputeVersionVariables(arguments);
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
                var arguments = new Arguments { TargetPath = childDir, NoFetch = true };
                gitVersionComputer.ComputeVersionVariables(arguments);
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
