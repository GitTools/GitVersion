using GitTools.Testing;
using GitVersion.BuildAgents;
using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Logging;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation.Cache;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.Core.Tests;

[TestFixture]
[Parallelizable(ParallelScope.None)]
public class GitVersionExecutorTests : TestBase
{
    private IFileSystem fileSystem;
    private ILog log;
    private IGitVersionCache gitVersionCache;
    private IServiceProvider sp;

    [Test]
    public void CacheKeySameAfterReNormalizing()
    {
        using var fixture = new EmptyRepositoryFixture();
        var targetUrl = "https://github.com/GitTools/GitVersion.git";
        var targetBranch = $"refs/head/{MainBranch}";

        var gitVersionOptions = new GitVersionOptions
        {
            RepositoryInfo = { TargetUrl = targetUrl, TargetBranch = targetBranch },
            WorkingDirectory = fixture.RepositoryPath,
            Settings = { NoNormalize = false }
        };

        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable(AzurePipelines.EnvironmentVariableName, "true");

        this.sp = GetServiceProvider(gitVersionOptions, environment: environment);

        var preparer = this.sp.GetService<IGitPreparer>();

        preparer.Prepare();
        var cacheKeyFactory = this.sp.GetService<IGitVersionCacheKeyFactory>();
        var cacheKey1 = cacheKeyFactory.Create(null);
        preparer.Prepare();

        var cacheKey2 = cacheKeyFactory.Create(null);

        cacheKey2.Value.ShouldBe(cacheKey1.Value);
    }

    [Test]
    public void GitPreparerShouldNotFailWhenTargetPathNotInitialized()
    {
        var targetUrl = "https://github.com/GitTools/GitVersion.git";

        var gitVersionOptions = new GitVersionOptions
        {
            RepositoryInfo = { TargetUrl = targetUrl },
            WorkingDirectory = null
        };
        Should.NotThrow(() =>
        {
            this.sp = GetServiceProvider(gitVersionOptions);

            this.sp.GetService<IGitPreparer>();
        });
    }

    [Test]
    [Category(NoMono)]
    [Description(NoMonoDescription)]
    public void CacheKeyForWorktree()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        var worktreePath = Path.Combine(Directory.GetParent(fixture.RepositoryPath).FullName, Guid.NewGuid().ToString());
        try
        {
            // create a branch and a new worktree for it
            var repo = new Repository(fixture.RepositoryPath);
            repo.Worktrees.Add("worktree", worktreePath, false);

            var targetUrl = "https://github.com/GitTools/GitVersion.git";

            var gitVersionOptions = new GitVersionOptions
            {
                RepositoryInfo = { TargetUrl = targetUrl, TargetBranch = MainBranch },
                WorkingDirectory = worktreePath
            };

            this.sp = GetServiceProvider(gitVersionOptions);

            var preparer = this.sp.GetService<IGitPreparer>();
            preparer.Prepare();
            var cacheKey = this.sp.GetService<IGitVersionCacheKeyFactory>().Create(null);
            cacheKey.Value.ShouldNotBeEmpty();
        }
        finally
        {
            DirectoryHelper.DeleteDirectory(worktreePath);
        }
    }

    [Test]
    public void CacheFileExistsOnDisk()
    {
        const string versionCacheFileContent = @"
        Major: 4
        Minor: 10
        Patch: 3
        PreReleaseTag: test.19
        PreReleaseTagWithDash: -test.19
        PreReleaseLabel: test
        PreReleaseLabelWithDash: -test
        PreReleaseNumber: 19
        WeightedPreReleaseNumber: 19
        BuildMetaData:
        BuildMetaDataPadded:
        FullBuildMetaData: Branch.feature/test.Sha.dd2a29aff0c948e1bdf3dabbe13e1576e70d5f9f
        MajorMinorPatch: 4.10.3
        SemVer: 4.10.3-test.19
        LegacySemVer: 4.10.3-test19
        LegacySemVerPadded: 4.10.3-test0019
        AssemblySemVer: 4.10.3.0
        AssemblySemFileVer: 4.10.3.0
        FullSemVer: 4.10.3-test.19
        InformationalVersion: 4.10.3-test.19+Branch.feature/test.Sha.dd2a29aff0c948e1bdf3dabbe13e1576e70d5f9f
        BranchName: feature/test
        EscapedBranchName: feature-test
        Sha: dd2a29aff0c948e1bdf3dabbe13e1576e70d5f9f
        ShortSha: dd2a29af
        NuGetVersionV2: 4.10.3-test0019
        NuGetVersion: 4.10.3-test0019
        NuGetPreReleaseTagV2: test0019
        NuGetPreReleaseTag: test0019
        VersionSourceSha: 4.10.2
        CommitsSinceVersionSource: 19
        CommitsSinceVersionSourcePadded: 0019
        CommitDate: 2015-11-10
        UncommittedChanges: 0
        ";

        var stringBuilder = new StringBuilder();
        void Action(string s) => stringBuilder.AppendLine(s);

        var logAppender = new TestLogAppender(Action);
        this.log = new Log(logAppender);

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();

        var gitVersionOptions = new GitVersionOptions { WorkingDirectory = fixture.RepositoryPath };

        var gitVersionCalculator = GetGitVersionCalculator(gitVersionOptions, this.log);

        var versionVariables = gitVersionCalculator.CalculateVersionVariables();
        versionVariables.AssemblySemVer.ShouldBe("0.1.0.0");

        this.fileSystem.WriteAllText(versionVariables.FileName, versionCacheFileContent);
        versionVariables = gitVersionCalculator.CalculateVersionVariables();
        versionVariables.AssemblySemVer.ShouldBe("4.10.3.0");

        var logsMessages = stringBuilder.ToString();

        logsMessages.ShouldContain("Deserializing version variables from cache file", Case.Insensitive, logsMessages);
    }

    [Test]
    public void CacheFileExistsOnDiskWhenOverrideConfigIsSpecifiedVersionShouldBeDynamicallyCalculatedWithoutSavingInCache()
    {
        const string versionCacheFileContent = @"
        Major: 4
        Minor: 10
        Patch: 3
        PreReleaseTag: test.19
        PreReleaseTagWithDash: -test.19
        PreReleaseLabel: test
        PreReleaseLabelWithDash: -test
        PreReleaseNumber: 19
        BuildMetaData:
        BuildMetaDataPadded:
        FullBuildMetaData: Branch.feature/test.Sha.dd2a29aff0c948e1bdf3dabbe13e1576e70d5f9f
        MajorMinorPatch: 4.10.3
        SemVer: 4.10.3-test.19
        LegacySemVer: 4.10.3-test19
        LegacySemVerPadded: 4.10.3-test0019
        AssemblySemVer: 4.10.3.0
        AssemblySemFileVer: 4.10.3.0
        FullSemVer: 4.10.3-test.19
        InformationalVersion: 4.10.3-test.19+Branch.feature/test.Sha.dd2a29aff0c948e1bdf3dabbe13e1576e70d5f9f
        BranchName: feature/test
        EscapedBranchName: feature-test
        Sha: dd2a29aff0c948e1bdf3dabbe13e1576e70d5f9f
        ShortSha: dd2a29af
        NuGetVersionV2: 4.10.3-test0019
        NuGetVersion: 4.10.3-test0019
        NuGetPreReleaseTagV2: test0019
        NuGetPreReleaseTag: test0019
        CommitsSinceVersionSource: 19
        CommitsSinceVersionSourcePadded: 0019
        CommitDate: 2015-11-10
        UncommittedChanges: 0
        ";

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();

        var gitVersionOptions = new GitVersionOptions { WorkingDirectory = fixture.RepositoryPath };
        var gitVersionCalculator = GetGitVersionCalculator(gitVersionOptions, this.log);

        var versionVariables = gitVersionCalculator.CalculateVersionVariables();
        versionVariables.AssemblySemVer.ShouldBe("0.1.0.0");

        this.fileSystem.WriteAllText(versionVariables.FileName, versionCacheFileContent);

        var cacheDirectory = this.gitVersionCache.GetCacheDirectory();

        var cacheDirectoryTimestamp = this.fileSystem.GetLastDirectoryWrite(cacheDirectory);

        var config = new ConfigurationBuilder().Add(new Config { TagPrefix = "prefix" }).Build();
        gitVersionOptions = new GitVersionOptions { WorkingDirectory = fixture.RepositoryPath, ConfigInfo = { OverrideConfig = config } };

        gitVersionCalculator = GetGitVersionCalculator(gitVersionOptions);
        versionVariables = gitVersionCalculator.CalculateVersionVariables();

        versionVariables.AssemblySemVer.ShouldBe("0.1.0.0");

        var cachedDirectoryTimestampAfter = this.fileSystem.GetLastDirectoryWrite(cacheDirectory);
        cachedDirectoryTimestampAfter.ShouldBe(cacheDirectoryTimestamp, "Cache was updated when override config was set");
    }

    [Test]
    public void CacheFileIsMissing()
    {
        var stringBuilder = new StringBuilder();
        void Action(string s) => stringBuilder.AppendLine(s);

        var logAppender = new TestLogAppender(Action);
        this.log = new Log(logAppender);

        using var fixture = new EmptyRepositoryFixture();

        var gitVersionOptions = new GitVersionOptions { WorkingDirectory = fixture.RepositoryPath };

        fixture.Repository.MakeACommit();
        var gitVersionCalculator = GetGitVersionCalculator(gitVersionOptions, this.log, fixture.Repository.ToGitRepository());

        gitVersionCalculator.CalculateVersionVariables();

        var logsMessages = stringBuilder.ToString();
        logsMessages.ShouldContain("yml not found", Case.Insensitive, logsMessages);
    }

    [Test]
    public void ConfigChangeInvalidatesCache()
    {
        const string versionCacheFileContent = @"
        Major: 4
        Minor: 10
        Patch: 3
        PreReleaseTag: test.19
        PreReleaseTagWithDash: -test.19
        PreReleaseLabel: test
        PreReleaseLabelWithDash: -test
        PreReleaseNumber: 19
        WeightedPreReleaseNumber: 19
        BuildMetaData:
        BuildMetaDataPadded:
        FullBuildMetaData: Branch.feature/test.Sha.dd2a29aff0c948e1bdf3dabbe13e1576e70d5f9f
        MajorMinorPatch: 4.10.3
        SemVer: 4.10.3-test.19
        LegacySemVer: 4.10.3-test19
        LegacySemVerPadded: 4.10.3-test0019
        AssemblySemVer: 4.10.3.0
        AssemblySemFileVer: 4.10.3.0
        FullSemVer: 4.10.3-test.19
        InformationalVersion: 4.10.3-test.19+Branch.feature/test.Sha.dd2a29aff0c948e1bdf3dabbe13e1576e70d5f9f
        BranchName: feature/test
        EscapedBranchName: feature-test
        Sha: dd2a29aff0c948e1bdf3dabbe13e1576e70d5f9f
        ShortSha: dd2a29af
        NuGetVersionV2: 4.10.3-test0019
        NuGetVersion: 4.10.3-test0019
        NuGetPreReleaseTagV2: test0019
        NuGetPreReleaseTag: test0019
        VersionSourceSha: 4.10.2
        CommitsSinceVersionSource: 19
        CommitsSinceVersionSourcePadded: 0019
        CommitDate: 2015-11-10
        UncommittedChanges: 0
        ";

        using var fixture = new EmptyRepositoryFixture();

        var gitVersionOptions = new GitVersionOptions { WorkingDirectory = fixture.RepositoryPath };

        fixture.Repository.MakeACommit();

        var gitVersionCalculator = GetGitVersionCalculator(gitVersionOptions);
        var versionVariables = gitVersionCalculator.CalculateVersionVariables();

        versionVariables.AssemblySemVer.ShouldBe("0.1.0.0");
        versionVariables.FileName.ShouldNotBeNullOrEmpty();

        this.fileSystem.WriteAllText(versionVariables.FileName, versionCacheFileContent);

        versionVariables = gitVersionCalculator.CalculateVersionVariables();
        versionVariables.AssemblySemVer.ShouldBe("4.10.3.0");

        var configPath = Path.Combine(fixture.RepositoryPath, ConfigFileLocator.DefaultFileName);
        this.fileSystem.WriteAllText(configPath, "next-version: 5.0");

        gitVersionCalculator = GetGitVersionCalculator(gitVersionOptions, fs: this.fileSystem);

        versionVariables = gitVersionCalculator.CalculateVersionVariables();
        versionVariables.AssemblySemVer.ShouldBe("5.0.0.0");
    }

    [Test]
    public void NoCacheBypassesCache()
    {
        const string versionCacheFileContent = @"
        Major: 4
        Minor: 10
        Patch: 3
        PreReleaseTag: test.19
        PreReleaseTagWithDash: -test.19
        PreReleaseLabel: test
        PreReleaseLabelWithDash: -test
        PreReleaseNumber: 19
        WeightedPreReleaseNumber: 19
        BuildMetaData:
        BuildMetaDataPadded:
        FullBuildMetaData: Branch.feature/test.Sha.dd2a29aff0c948e1bdf3dabbe13e1576e70d5f9f
        MajorMinorPatch: 4.10.3
        SemVer: 4.10.3-test.19
        LegacySemVer: 4.10.3-test19
        LegacySemVerPadded: 4.10.3-test0019
        AssemblySemVer: 4.10.3.0
        AssemblySemFileVer: 4.10.3.0
        FullSemVer: 4.10.3-test.19
        InformationalVersion: 4.10.3-test.19+Branch.feature/test.Sha.dd2a29aff0c948e1bdf3dabbe13e1576e70d5f9f
        BranchName: feature/test
        EscapedBranchName: feature-test
        Sha: dd2a29aff0c948e1bdf3dabbe13e1576e70d5f9f
        ShortSha: dd2a29af
        NuGetVersionV2: 4.10.3-test0019
        NuGetVersion: 4.10.3-test0019
        NuGetPreReleaseTagV2: test0019
        NuGetPreReleaseTag: test0019
        VersionSourceSha: 4.10.2
        CommitsSinceVersionSource: 19
        CommitsSinceVersionSourcePadded: 0019
        CommitDate: 2015-11-10
        UncommittedChanges: 0
        ";

        using var fixture = new EmptyRepositoryFixture();

        var gitVersionOptions = new GitVersionOptions { WorkingDirectory = fixture.RepositoryPath };

        fixture.Repository.MakeACommit();
        var gitVersionCalculator = GetGitVersionCalculator(gitVersionOptions);

        var versionVariables = gitVersionCalculator.CalculateVersionVariables();

        versionVariables.AssemblySemVer.ShouldBe("0.1.0.0");
        versionVariables.FileName.ShouldNotBeNullOrEmpty();

        this.fileSystem.WriteAllText(versionVariables.FileName, versionCacheFileContent);
        versionVariables = gitVersionCalculator.CalculateVersionVariables();
        versionVariables.AssemblySemVer.ShouldBe("4.10.3.0");

        gitVersionOptions.Settings.NoCache = true;
        versionVariables = gitVersionCalculator.CalculateVersionVariables();
        versionVariables.AssemblySemVer.ShouldBe("0.1.0.0");
    }

    [Test]
    public void WorkingDirectoryWithoutGit()
    {
        var gitVersionOptions = new GitVersionOptions { WorkingDirectory = System.Environment.SystemDirectory };

        var exception = Assert.Throws<DirectoryNotFoundException>(() =>
        {
            var gitVersionCalculator = GetGitVersionCalculator(gitVersionOptions);
            gitVersionCalculator.CalculateVersionVariables();
        });
        exception.Message.ShouldContain("Cannot find the .git directory");
    }

    [Test]
    public void WorkingDirectoryWithoutCommits()
    {
        using var fixture = new EmptyRepositoryFixture();

        var gitVersionOptions = new GitVersionOptions { WorkingDirectory = fixture.RepositoryPath };

        var exception = Assert.Throws<GitVersionException>(() =>
        {
            var gitVersionCalculator = GetGitVersionCalculator(gitVersionOptions);
            gitVersionCalculator.CalculateVersionVariables();
        });
        exception.Message.ShouldContain("No commits found on the current branch.");
    }

    [Test]
    [Category(NoMono)]
    [Description(NoMonoDescription)]
    public void GetProjectRootDirectoryWorkingDirectoryWithWorktree()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();

        var worktreePath = Path.Combine(Directory.GetParent(fixture.RepositoryPath).FullName, Guid.NewGuid().ToString());
        try
        {
            // create a branch and a new worktree for it
            var repo = new Repository(fixture.RepositoryPath);
            repo.Worktrees.Add("worktree", worktreePath, false);

            var targetUrl = "https://github.com/GitTools/GitVersion.git";

            var gitVersionOptions = new GitVersionOptions
            {
                RepositoryInfo = { TargetUrl = targetUrl },
                WorkingDirectory = worktreePath
            };

            this.sp = GetServiceProvider(gitVersionOptions);
            var repositoryInfo = this.sp.GetService<IGitRepositoryInfo>();
            repositoryInfo?.ProjectRootDirectory.TrimEnd('/', '\\').ShouldBe(worktreePath);
        }
        finally
        {
            DirectoryHelper.DeleteDirectory(worktreePath);
        }
    }

    [Test]
    public void GetProjectRootDirectoryNoWorktree()
    {
        using var fixture = new EmptyRepositoryFixture();
        var targetUrl = "https://github.com/GitTools/GitVersion.git";

        var gitVersionOptions = new GitVersionOptions
        {
            RepositoryInfo = { TargetUrl = targetUrl },
            WorkingDirectory = fixture.RepositoryPath
        };

        this.sp = GetServiceProvider(gitVersionOptions);
        var repositoryInfo = this.sp.GetService<IGitRepositoryInfo>();

        var expectedPath = fixture.RepositoryPath.TrimEnd('/', '\\');
        repositoryInfo?.ProjectRootDirectory.TrimEnd('/', '\\').ShouldBe(expectedPath);
    }

    [Test]
    public void GetDotGitDirectoryNoWorktree()
    {
        using var fixture = new EmptyRepositoryFixture();

        var gitVersionOptions = new GitVersionOptions
        {
            WorkingDirectory = fixture.RepositoryPath
        };

        this.sp = GetServiceProvider(gitVersionOptions);
        var repositoryInfo = this.sp.GetService<IGitRepositoryInfo>();

        var expectedPath = Path.Combine(fixture.RepositoryPath, ".git");
        repositoryInfo?.DotGitDirectory.ShouldBe(expectedPath);
    }

    [Test]
    [Category(NoMono)]
    [Description(NoMonoDescription)]
    public void GetDotGitDirectoryWorktree()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();

        var worktreePath = Path.Combine(Directory.GetParent(fixture.RepositoryPath).FullName, Guid.NewGuid().ToString());
        try
        {
            // create a branch and a new worktree for it
            var repo = new Repository(fixture.RepositoryPath);
            repo.Worktrees.Add("worktree", worktreePath, false);

            var gitVersionOptions = new GitVersionOptions
            {
                WorkingDirectory = worktreePath
            };

            this.sp = GetServiceProvider(gitVersionOptions);
            var repositoryInfo = this.sp.GetService<IGitRepositoryInfo>();

            var expectedPath = Path.Combine(fixture.RepositoryPath, ".git");
            repositoryInfo?.DotGitDirectory.ShouldBe(expectedPath);
        }
        finally
        {
            DirectoryHelper.DeleteDirectory(worktreePath);
        }
    }

    [Test]
    [Category(NoMono)]
    [Description(NoMonoDescription)]
    public void CalculateVersionFromWorktreeHead()
    {
        // Setup
        using var fixture = new EmptyRepositoryFixture();
        var repoDir = new DirectoryInfo(fixture.RepositoryPath);
        var worktreePath = Path.Combine(repoDir.Parent.FullName, $"{repoDir.Name}-v1");

        fixture.Repository.MakeATaggedCommit("v1.0.0");
        var branchV1 = fixture.Repository.CreateBranch("support/1.0");

        fixture.Repository.MakeATaggedCommit("v2.0.0");

        fixture.Repository.Worktrees.Add(branchV1.CanonicalName, "1.0", worktreePath, false);
        using var worktreeFixture = new LocalRepositoryFixture(new Repository(worktreePath));

        var gitVersionOptions = new GitVersionOptions { WorkingDirectory = worktreeFixture.RepositoryPath };

        var sut = GetGitVersionCalculator(gitVersionOptions);

        // Execute
        var version = sut.CalculateVersionVariables();

        // Verify
        version.SemVer.ShouldBe("1.0.0");
        var commits = worktreeFixture.Repository.Head.Commits;
        version.Sha.ShouldBe(commits.First().Sha);
    }

    private IGitVersionCalculateTool GetGitVersionCalculator(GitVersionOptions gitVersionOptions, ILog logger = null, IGitRepository repository = null, IFileSystem fs = null)
    {
        this.sp = GetServiceProvider(gitVersionOptions, logger, repository, fs);

        this.fileSystem = this.sp.GetService<IFileSystem>();
        this.log = this.sp.GetService<ILog>();
        this.gitVersionCache = this.sp.GetService<IGitVersionCache>();

        return this.sp.GetService<IGitVersionCalculateTool>();
    }

    private static IServiceProvider GetServiceProvider(GitVersionOptions gitVersionOptions, ILog log = null, IGitRepository repository = null, IFileSystem fileSystem = null, IEnvironment environment = null) =>
        ConfigureServices(services =>
        {
            if (log != null) services.AddSingleton(log);
            if (fileSystem != null) services.AddSingleton(fileSystem);
            if (repository != null) services.AddSingleton(repository);
            if (environment != null) services.AddSingleton(environment);
            var options = Options.Create(gitVersionOptions);
            services.AddSingleton(options);
            services.AddSingleton(RepositoryExtensions.ToGitRepositoryInfo(options));
        });
}
