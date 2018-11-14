namespace GitTools.Tests.Git
{
    using System;
    using System.IO;
    using System.Linq;
    using GitTools.Git;
    using IO;
    using LibGit2Sharp;
    using NUnit.Framework;
    using Shouldly;
    using Testing;

    [TestFixture]
    public class DynamicRepositoriesTests
    {
        const string DefaultBranchName = "master";
        const string SpecificBranchName = "feature/foo";

        [Test]
        [TestCase(DefaultBranchName, DefaultBranchName)]
        [TestCase(SpecificBranchName, SpecificBranchName)]
        [Category("NoMono")]
        public void WorksCorrectlyWithRemoteRepository(string branchName, string expectedBranchName)
        {
            var repoName = Guid.NewGuid().ToString();
            var tempPath = Path.GetTempPath();
            var tempDir = Path.Combine(tempPath, repoName);
            Directory.CreateDirectory(tempDir);
            string dynamicRepositoryPath = null;

            try
            {
                using (var fixture = new EmptyRepositoryFixture())
                {
                    var expectedDynamicRepoLocation = Path.Combine(tempPath, fixture.RepositoryPath.Split(Path.DirectorySeparatorChar).Last());

                    fixture.Repository.MakeCommits(5);
                    fixture.Repository.CreateFileAndCommit("TestFile.txt");

                    var branch = fixture.Repository.CreateBranch(SpecificBranchName);

                    // Copy contents into working directory
                    File.Copy(Path.Combine(fixture.RepositoryPath, "TestFile.txt"), Path.Combine(tempDir, "TestFile.txt"));

                    var repositoryInfo = new RepositoryInfo
                    {
                        Url = fixture.RepositoryPath
                    };

                    using (var dynamicRepository = DynamicRepositories.CreateOrOpen(repositoryInfo, tempPath, branchName, branch.Tip.Sha))
                    {
                        dynamicRepositoryPath = dynamicRepository.Repository.Info.Path;
                        dynamicRepository.Repository.Info.Path.ShouldBe(Path.Combine(expectedDynamicRepoLocation, ".git\\"));

                        var currentBranch = dynamicRepository.Repository.Head.CanonicalName;

                        currentBranch.ShouldEndWith(expectedBranchName);
                    }
                }
            }
            finally
            {
                Directory.Delete(tempDir, true);

                if (dynamicRepositoryPath != null)
                {
                    DeleteHelper.DeleteGitRepository(dynamicRepositoryPath);
                }
            }
        }

        [Test]
        public void UpdatesExistingDynamicRepository()
        {
            var repoName = Guid.NewGuid().ToString();
            var tempPath = Path.GetTempPath();
            var tempDir = Path.Combine(tempPath, repoName);
            Directory.CreateDirectory(tempDir);
            string dynamicRepositoryPath = null;

            try
            {
                using (var mainRepositoryFixture = new EmptyRepositoryFixture())
                {
                    var commit = mainRepositoryFixture.Repository.MakeACommit();

                    var repositoryInfo = new RepositoryInfo
                    {
                        Url = mainRepositoryFixture.RepositoryPath
                    };

                    using (var dynamicRepository = DynamicRepositories.CreateOrOpen(repositoryInfo, tempPath, "master", commit.Sha))
                    {
                        dynamicRepositoryPath = dynamicRepository.Repository.Info.Path;
                    }

                    var newCommit = mainRepositoryFixture.Repository.MakeACommit();

                    using (var dynamicRepository = DynamicRepositories.CreateOrOpen(repositoryInfo, tempPath, "master", newCommit.Sha))
                    {
                        dynamicRepository.Repository.Info.Path.ShouldBe(dynamicRepositoryPath);
                        dynamicRepository.Repository.Commits.ShouldContain(c => c.Sha == newCommit.Sha);
                    }
                }
            }
            finally
            {
                Directory.Delete(tempDir, true);

                if (dynamicRepositoryPath != null)
                {
                    DeleteHelper.DeleteGitRepository(dynamicRepositoryPath);
                }
            }
        }

        [Test]
        [Category("NoMono")]
        public void PicksAnotherDirectoryNameWhenDynamicRepoFolderTaken()
        {
            var repoName = Guid.NewGuid().ToString();
            var tempPath = Path.GetTempPath();
            var tempDir = Path.Combine(tempPath, repoName);
            Directory.CreateDirectory(tempDir);
            string expectedDynamicRepoLocation = null;

            try
            {
                using (var fixture = new EmptyRepositoryFixture())
                {
                    var head = fixture.Repository.CreateFileAndCommit("TestFile.txt");
                    File.Copy(Path.Combine(fixture.RepositoryPath, "TestFile.txt"), Path.Combine(tempDir, "TestFile.txt"));
                    expectedDynamicRepoLocation = Path.Combine(tempPath, fixture.RepositoryPath.Split(Path.DirectorySeparatorChar).Last());
                    Directory.CreateDirectory(expectedDynamicRepoLocation);

                    var repositoryInfo = new RepositoryInfo
                    {
                        Url = fixture.RepositoryPath
                    };

                    using (var dynamicRepository = DynamicRepositories.CreateOrOpen(repositoryInfo, tempPath, "master", head.Sha))
                    {
                        dynamicRepository.Repository.Info.Path.ShouldBe(Path.Combine(expectedDynamicRepoLocation + "_1", ".git\\"));
                    }
                }
            }
            finally
            {
                DeleteHelper.DeleteDirectory(tempDir, true);
                if (expectedDynamicRepoLocation != null)
                {
                    DeleteHelper.DeleteDirectory(expectedDynamicRepoLocation, true);
                }

                if (expectedDynamicRepoLocation != null)
                {
                    DeleteHelper.DeleteGitRepository(expectedDynamicRepoLocation + "_1");
                }
            }
        }

        [Test]
        [Category("NoMono")]
        public void PicksAnotherDirectoryNameWhenDynamicRepoFolderIsInUse()
        {
            var tempPath = Path.GetTempPath();
            var expectedDynamicRepoLocation = default(string);
            var expectedDynamicRepo2Location = default(string);

            try
            {
                using (var fixture = new EmptyRepositoryFixture())
                {
                    var head = fixture.Repository.CreateFileAndCommit("TestFile.txt");
                    var repositoryInfo = new RepositoryInfo
                    {
                        Url = fixture.RepositoryPath
                    };

                    using (var dynamicRepository = DynamicRepositories.CreateOrOpen(repositoryInfo, tempPath, "master", head.Sha))
                    using (var dynamicRepository2 = DynamicRepositories.CreateOrOpen(repositoryInfo, tempPath, "master", head.Sha))
                    {
                        expectedDynamicRepoLocation = dynamicRepository.Repository.Info.Path;
                        expectedDynamicRepo2Location = dynamicRepository2.Repository.Info.Path;
                        dynamicRepository.Repository.Info.Path.ShouldNotBe(dynamicRepository2.Repository.Info.Path);
                    }
                }
            }
            finally
            {
                if (expectedDynamicRepoLocation != null)
                {
                    DeleteHelper.DeleteDirectory(expectedDynamicRepoLocation, true);
                }

                if (expectedDynamicRepo2Location != null)
                {
                    DeleteHelper.DeleteGitRepository(expectedDynamicRepo2Location);
                }
            }
        }

        [Test]
        public void ThrowsExceptionWhenNotEnoughInfo()
        {
            var tempDir = Path.GetTempPath();

            var repositoryInfo = new RepositoryInfo
            {
                Url = tempDir
            };

            Should.Throw<Exception>(() => DynamicRepositories.CreateOrOpen(repositoryInfo, tempDir, null, null));
        }

        [Test]
        public void UsingDynamicRepositoryWithFeatureBranchWorks()
        {
            var repoName = Guid.NewGuid().ToString();
            var tempPath = Path.GetTempPath();
            var tempDir = Path.Combine(tempPath, repoName);
            Directory.CreateDirectory(tempDir);

            try
            {
                using (var mainRepositoryFixture = new EmptyRepositoryFixture())
                {
                    var commit = mainRepositoryFixture.Repository.MakeACommit();

                    var repositoryInfo = new RepositoryInfo
                    {
                        Url = mainRepositoryFixture.RepositoryPath
                    };

                    Commands.Checkout(mainRepositoryFixture.Repository, mainRepositoryFixture.Repository.CreateBranch("feature1"));

                    Should.NotThrow(() =>
                    {
                        using (DynamicRepositories.CreateOrOpen(repositoryInfo, tempPath, "feature1", commit.Sha))
                        {
                        }
                    });
                }
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Test]
        public void UsingDynamicRepositoryWithoutTargetBranchFails()
        {
            var tempPath = Path.GetTempPath();

            using (var mainRepositoryFixture = new EmptyRepositoryFixture())
            {
                mainRepositoryFixture.Repository.MakeACommit();

                var repositoryInfo = new RepositoryInfo
                {
                    Url = mainRepositoryFixture.RepositoryPath
                };

                Should.Throw<GitToolsException>(() =>
                {
                    using (DynamicRepositories.CreateOrOpen(repositoryInfo, tempPath, null, null))
                    {
                    }
                });
            }
        }

        [Test]
        public void UsingDynamicRepositoryWithoutTargetBranchCommitFails()
        {
            var tempPath = Path.GetTempPath();

            using (var mainRepositoryFixture = new EmptyRepositoryFixture())
            {
                mainRepositoryFixture.Repository.MakeACommit();

                var repositoryInfo = new RepositoryInfo
                {
                    Url = mainRepositoryFixture.RepositoryPath
                };

                Should.Throw<GitToolsException>(() =>
                {
                    using (DynamicRepositories.CreateOrOpen(repositoryInfo, tempPath, "master", null))
                    {
                    }
                });
            }
        }

        [Test]
        public void TestErrorThrownForInvalidRepository()
        {
            var repoName = Guid.NewGuid().ToString();
            var tempPath = Path.GetTempPath();
            var tempDir = Path.Combine(tempPath, repoName);
            Directory.CreateDirectory(tempDir);

            try
            {
                var repositoryInfo = new RepositoryInfo
                {
                    Url = "http://127.0.0.1/testrepo.git"
                };

                Should.Throw<Exception>(() =>
                {
                    using (DynamicRepositories.CreateOrOpen(repositoryInfo, tempPath, "master", "sha"))
                    {
                    }
                });
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}