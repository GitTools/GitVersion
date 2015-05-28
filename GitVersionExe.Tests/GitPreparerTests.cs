using System;
using System.IO;
using System.Linq;
using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;
using Shouldly;
using GitVersion.Helpers;

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
    [TestCase(null, DefaultBranchName)]
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
    public void UsesGitVersionConfigWhenCreatingDynamicRepository()
    {
        string localRepoPath = PathHelper.GetTempPath();
        string repoBasePath = Path.GetDirectoryName(PathHelper.GetTempPath());

        try
        {
            using (var remote = new EmptyRepositoryFixture(new Config()))
            {
                var configFile = Path.Combine(remote.Repository.Info.WorkingDirectory, "GitVersionConfig.yaml");
                File.WriteAllText(configFile, "next-version: 1.0.0");
                remote.Repository.Stage(configFile);
                remote.Repository.Commit("Add GitVersionConfig");

                Repository.Clone(remote.RepositoryPath, localRepoPath);

                var arguments = string.Format(" /url {0} /dynamicRepoLocation {1}", remote.RepositoryPath, repoBasePath);
                var results = GitVersionHelper.ExecuteIn(localRepoPath, arguments, false);
                results.OutputVariables.SemVer.ShouldBe("1.0.0");
            }
        }
        finally
        {
            DeleteHelper.DeleteGitRepository(repoBasePath);
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

                var arguments = new Arguments
                {
                    TargetPath = tempDir,
                    TargetUrl = mainRepositoryFixture.RepositoryPath,
                    TargetBranch = "master"
                };

                var gitPreparer = new GitPreparer(arguments);
                gitPreparer.InitialiseDynamicRepositoryIfNeeded();
                dynamicRepositoryPath = gitPreparer.GetDotGitDirectory();

                var newCommit = mainRepositoryFixture.Repository.MakeACommit();
                gitPreparer.InitialiseDynamicRepositoryIfNeeded();

                using (var repository = new Repository(dynamicRepositoryPath))
                {
                    mainRepositoryFixture.DumpGraph();
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