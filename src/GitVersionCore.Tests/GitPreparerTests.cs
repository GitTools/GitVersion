using System;
using System.IO;
using System.Linq;
using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class GitPreparerTests
{
    const string DefaultBranchName = "master";
    const string SpecificBranchName = "feature/foo";

    [Test]
    [TestCase(DefaultBranchName, DefaultBranchName)]
    [TestCase(SpecificBranchName, SpecificBranchName)]
    public void WorksCorrectlyWithRemoteRepository(string branchName, string expectedBranchName)
    {
        var repoName = Guid.NewGuid().ToString();
        var tempPath = Path.GetTempPath();
        var tempDir = Path.Combine(tempPath, repoName);
        Directory.CreateDirectory(tempDir);
        string dynamicRepositoryPath = null;

        try
        {
            using (var fixture = new EmptyRepositoryFixture(new Config()))
            {
                var expectedDynamicRepoLocation = Path.Combine(tempPath, fixture.RepositoryPath.Split('\\').Last());

                fixture.Repository.MakeCommits(5);
                fixture.Repository.CreateFileAndCommit("TestFile.txt");

                fixture.Repository.CreateBranch(SpecificBranchName);

                // Copy contents into working directory
                File.Copy(Path.Combine(fixture.RepositoryPath, "TestFile.txt"), Path.Combine(tempDir, "TestFile.txt"));

                var gitPreparer = new GitPreparer(fixture.RepositoryPath, null, new Authentication(), false, tempDir);
                gitPreparer.Initialise(false, branchName);
                dynamicRepositoryPath = gitPreparer.GetDotGitDirectory();

                gitPreparer.IsDynamicGitRepository.ShouldBe(true);
                gitPreparer.DynamicGitRepositoryPath.ShouldBe(expectedDynamicRepoLocation + "\\.git");

                using (var repository = new Repository(dynamicRepositoryPath))
                {
                    var currentBranch = repository.Head.CanonicalName;

                    currentBranch.ShouldEndWith(expectedBranchName);
                }
            }
        }
        finally
        {
            Directory.Delete(tempDir, true);
            if (dynamicRepositoryPath != null)
                DeleteHelper.DeleteGitRepository(dynamicRepositoryPath);
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
            using (var mainRepositoryFixture = new EmptyRepositoryFixture(new Config()))
            {
                mainRepositoryFixture.Repository.MakeCommits(1);

                var gitPreparer = new GitPreparer(mainRepositoryFixture.RepositoryPath, null, new Authentication(), false, tempDir);
                gitPreparer.Initialise(false, "master");
                dynamicRepositoryPath = gitPreparer.GetDotGitDirectory();

                var newCommit = mainRepositoryFixture.Repository.MakeACommit();
                gitPreparer.Initialise(false, "master");

                using (var repository = new Repository(dynamicRepositoryPath))
                {
                    mainRepositoryFixture.Repository.DumpGraph();
                    repository.DumpGraph();
                    repository.Commits.ShouldContain(c => c.Sha == newCommit.Sha);
                }
            }
        }
        finally
        {
            Directory.Delete(tempDir, true);
            if (dynamicRepositoryPath != null)
                DeleteHelper.DeleteGitRepository(dynamicRepositoryPath);
        }
    }

    [Test]
    public void PicksAnotherDirectoryNameWhenDynamicRepoFolderTaken()
    {
        var repoName = Guid.NewGuid().ToString();
        var tempPath = Path.GetTempPath();
        var tempDir = Path.Combine(tempPath, repoName);
        Directory.CreateDirectory(tempDir);
        string expectedDynamicRepoLocation = null;

        try
        {
            using (var fixture = new EmptyRepositoryFixture(new Config()))
            {
                fixture.Repository.CreateFileAndCommit("TestFile.txt");
                File.Copy(Path.Combine(fixture.RepositoryPath, "TestFile.txt"), Path.Combine(tempDir, "TestFile.txt"));
                expectedDynamicRepoLocation = Path.Combine(tempPath, fixture.RepositoryPath.Split('\\').Last());
                Directory.CreateDirectory(expectedDynamicRepoLocation);

                var gitPreparer = new GitPreparer(fixture.RepositoryPath, null, new Authentication(), false, tempDir);
                gitPreparer.Initialise(false, "master");

                gitPreparer.IsDynamicGitRepository.ShouldBe(true);
                gitPreparer.DynamicGitRepositoryPath.ShouldBe(expectedDynamicRepoLocation + "_1\\.git");
            }
        }
        finally
        {
            Directory.Delete(tempDir, true);
            if (expectedDynamicRepoLocation != null)
                Directory.Delete(expectedDynamicRepoLocation, true);
            if (expectedDynamicRepoLocation != null)
                DeleteHelper.DeleteGitRepository(expectedDynamicRepoLocation + "_1");
        }
    }

    [Test]
    public void WorksCorrectlyWithLocalRepository()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            var targetPath = Path.Combine(fixture.RepositoryPath, "tools\\gitversion\\");
            Directory.CreateDirectory(targetPath);
            var gitPreparer = new GitPreparer(null, null, null, false, targetPath);
            var dotGitDirectory = gitPreparer.GetDotGitDirectory();
            var projectRoot = gitPreparer.GetProjectRootDirectory();

            dotGitDirectory.ShouldBe(Path.Combine(fixture.RepositoryPath, ".git"));
            projectRoot.ShouldBe(fixture.RepositoryPath);
        }
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
            using (var mainRepositoryFixture = new EmptyRepositoryFixture(new Config()))
            {
                mainRepositoryFixture.Repository.MakeACommit();

                var gitPreparer = new GitPreparer(mainRepositoryFixture.RepositoryPath, null, new Authentication(), false, tempDir);
                gitPreparer.Initialise(true, "feature1");

                mainRepositoryFixture.Repository.Checkout(mainRepositoryFixture.Repository.CreateBranch("feature1"));

                Should.NotThrow(() => gitPreparer.Initialise(true, "feature1"));
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
        var repoName = Guid.NewGuid().ToString();
        var tempPath = Path.GetTempPath();
        var tempDir = Path.Combine(tempPath, repoName);
        Directory.CreateDirectory(tempDir);

        try
        {
            using (var mainRepositoryFixture = new EmptyRepositoryFixture(new Config()))
            {
                mainRepositoryFixture.Repository.MakeACommit();

                var gitPreparer = new GitPreparer(mainRepositoryFixture.RepositoryPath, null, new Authentication(), false, tempDir);

                Should.Throw<Exception>(() => gitPreparer.Initialise(true, null));
            }
        }
        finally
        {
            Directory.Delete(tempDir, true);
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
            var gitPreparer = new GitPreparer("http://127.0.0.1/testrepo.git", null, new Authentication(), false, tempDir);

            Should.Throw<Exception>(() => gitPreparer.Initialise(true, "master"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
