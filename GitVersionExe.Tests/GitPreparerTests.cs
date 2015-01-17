namespace GitVersionExe.Tests
{
    using System.IO;
    using GitVersion;
    using LibGit2Sharp;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public class GitPreparerTests
    {
        public GitPreparerTests()
        {
            Logger.WriteInfo = s => { };
            Logger.WriteWarning = s => { };
            Logger.WriteError = s => { };
        }

        const string DefaultBranchName = "master";
        const string SpecificBranchName = "feature/foo";

        [Test]
        [TestCase(null, DefaultBranchName, false)]
        [TestCase(SpecificBranchName, SpecificBranchName, false)]
        [TestCase(null, DefaultBranchName, true)]
        [TestCase(SpecificBranchName, SpecificBranchName, true)]
        public void WorksCorrectlyWithRemoteRepository(string branchName, string expectedBranchName, bool checkConfig)
        {
            var tempDir = Path.GetTempPath();

            using (var fixture = new EmptyRepositoryFixture(new Config()))
            {
                fixture.Repository.MakeCommits(5);

                if (checkConfig)
                {
                    fixture.Repository.CreateFileAndCommit("GitVersionConfig.yaml");
                }

                fixture.Repository.CreateBranch(SpecificBranchName);

                if (checkConfig)
                {
                    fixture.Repository.Refs.UpdateTarget(fixture.Repository.Refs.Head, fixture.Repository.Refs["refs/heads/" + SpecificBranchName]);

                    fixture.Repository.CreateFileAndCommit("GitVersionConfig.yaml");

                    fixture.Repository.Refs.UpdateTarget(fixture.Repository.Refs.Head, fixture.Repository.Refs["refs/heads/" + DefaultBranchName]);
                }

                var arguments = new Arguments
                {
                    TargetPath = tempDir,
                    TargetUrl = fixture.RepositoryPath
                };

                if (!string.IsNullOrWhiteSpace(branchName))
                {
                    arguments.TargetBranch = branchName;
                }

                var gitPreparer = new GitPreparer(arguments);
                var dynamicRepositoryPath = gitPreparer.Prepare();

                Assert.AreEqual(Path.Combine(tempDir, "_dynamicrepository", ".git"), dynamicRepositoryPath);
                Assert.IsTrue(gitPreparer.IsDynamicGitRepository);

                using (var repository = new Repository(dynamicRepositoryPath))
                {
                    var currentBranch = repository.Head.CanonicalName;

                    Assert.IsTrue(currentBranch.EndsWith(expectedBranchName));

                    if (checkConfig)
                    {
                        var expectedConfigPath = Path.Combine(dynamicRepositoryPath, "GitVersionConfig.yaml");
                        Assert.IsTrue(File.Exists(expectedConfigPath));
                    }
                }
            }
        }

        [Test]
        public void WorksCorrectlyWithLocalRepository()
        {
            var tempDir = Path.GetTempPath();

            var arguments = new Arguments
            {
                TargetPath = tempDir
            };

            var gitPreparer = new GitPreparer(arguments);
            var dynamicRepositoryPath = gitPreparer.Prepare();

            Assert.AreEqual(null, dynamicRepositoryPath);
            Assert.IsFalse(gitPreparer.IsDynamicGitRepository);
        }
    }
}
