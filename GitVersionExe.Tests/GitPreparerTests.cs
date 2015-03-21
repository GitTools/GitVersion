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

                // Copy contents into working directory
                File.Copy(Path.Combine(fixture.RepositoryPath, "TestFile.txt"), Path.Combine(tempDir, "TestFile.txt"));

                if (!string.IsNullOrWhiteSpace(branchName))
                {
                    arguments.TargetBranch = branchName;
                }

                var gitPreparer = new GitPreparer(arguments);
                gitPreparer.InitialiseDynamicRepositoryIfNeeded();
                dynamicRepositoryPath = gitPreparer.GetDotGitDirectory();

                gitPreparer.IsDynamicGitRepository.ShouldBe(true);
                gitPreparer.DynamicGitRepositoryPath.ShouldBe(expectedDynamicRepoLocation + "\\.git");

                using (var repository = new Repository(dynamicRepositoryPath))
                {
                    var currentBranch = repository.Head.CanonicalName;

                    currentBranch.EndsWith(expectedBranchName).ShouldBe(true);

                    if (checkConfig)
                    {
                        var expectedConfigPath = Path.Combine(dynamicRepositoryPath, "..\\GitVersionConfig.yaml");
                        File.Exists(expectedConfigPath).ShouldBe(true);
                    }
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

                var arguments = new Arguments
                {
                    TargetPath = tempDir,
                    TargetUrl = fixture.RepositoryPath
                };

                var gitPreparer = new GitPreparer(arguments);
                gitPreparer.InitialiseDynamicRepositoryIfNeeded();

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
        var tempDir = Path.GetTempPath();

        var arguments = new Arguments
        {
            TargetPath = tempDir
        };

        var gitPreparer = new GitPreparer(arguments);
        var dynamicRepositoryPath = gitPreparer.GetDotGitDirectory();

        dynamicRepositoryPath.ShouldBe(null);
        gitPreparer.IsDynamicGitRepository.ShouldBe(false);
    }
}