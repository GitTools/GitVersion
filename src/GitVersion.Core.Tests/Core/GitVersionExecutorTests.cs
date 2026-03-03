using System.IO.Abstractions;
using GitVersion.Agents;
using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.Helpers;
using GitVersion.Testing.Extensions;
using GitVersion.VersionCalculation.Caching;
using LibGit2Sharp;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace GitVersion.Core.Tests;

[TestFixture]
[Parallelizable(ParallelScope.None)]
public class GitVersionExecutorTests : TestBase
{
    private IFileSystem fileSystem;
    private GitVersionCacheProvider gitVersionCacheProvider;
    private IServiceProvider sp;

    private const string versionCacheFileContent =
        """
        {
          "AssemblySemFileVer": "4.10.3.0",
          "AssemblySemVer": "4.10.3.0",
          "BranchName": "feature/test",
          "BuildMetaData": null,
          "CommitDate": "2015-11-10T00:00:00.000Z",
          "CommitsSinceVersionSource": 19,
          "EscapedBranchName": "feature-test",
          "FullBuildMetaData": "Branch.feature/test.Sha.dd2a29aff0c948e1bdf3dabbe13e1576e70d5f9f",
          "FullSemVer": "4.10.3-test.19",
          "InformationalVersion": "4.10.3-test.19+Branch.feature/test.Sha.dd2a29aff0c948e1bdf3dabbe13e1576e70d5f9f",
          "Major": 4,
          "MajorMinorPatch": "4.10.3",
          "Minor": 10,
          "Patch": 3,
          "PreReleaseLabel": "test",
          "PreReleaseLabelWithDash": "-test",
          "PreReleaseNumber": 19,
          "PreReleaseTag": "test.19",
          "PreReleaseTagWithDash": "-test.19",
          "SemVer": "4.10.3-test.19",
          "Sha": "dd2a29aff0c948e1bdf3dabbe13e1576e70d5f9f",
          "ShortSha": "dd2a29af",
          "UncommittedChanges": 0,
          "VersionSourceDistance": 19,
          "VersionSourceIncrement": "Patch",
          "VersionSourceSemVer": "4.10.2",
          "VersionSourceSha": "4.10.2",
          "WeightedPreReleaseNumber": 19
        }
        """;

    [Test]
    public void CacheKeySameAfterReNormalizing()
    {
        using var fixture = new EmptyRepositoryFixture();
        const string targetUrl = "https://github.com/GitTools/GitVersion.git";
        const string targetBranch = $"refs/head/{MainBranch}";

        var gitVersionOptions = new GitVersionOptions { RepositoryInfo = { TargetUrl = targetUrl, TargetBranch = targetBranch }, WorkingDirectory = fixture.RepositoryPath, Settings = { NoNormalize = false } };

        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable(AzurePipelines.EnvironmentVariableName, "true");

        this.sp = GetServiceProvider(gitVersionOptions, environment: environment);

        sp.DiscoverRepository();

        var preparer = this.sp.GetRequiredService<IGitPreparer>();
        var repositoryInfo = this.sp.GetRequiredService<IGitRepositoryInfo>();
        var dynamicRepositoryPath = repositoryInfo.DynamicGitRepositoryPath;

        try
        {
            preparer.Prepare();
            var cacheKeyFactory = this.sp.GetRequiredService<IGitVersionCacheKeyFactory>();
            var cacheKey1 = cacheKeyFactory.Create(null);
            preparer.Prepare();

            var cacheKey2 = cacheKeyFactory.Create(null);

            cacheKey2.Value.ShouldBe(cacheKey1.Value);
        }
        finally
        {
            CleanUpDynamicRepository(dynamicRepositoryPath);
        }
    }

    [Test]
    public void GitPreparerShouldNotFailWhenTargetPathNotInitialized()
    {
        const string targetUrl = "https://github.com/GitTools/GitVersion.git";

        var gitVersionOptions = new GitVersionOptions { RepositoryInfo = { TargetUrl = targetUrl }, WorkingDirectory = string.Empty };
        Should.NotThrow(() =>
        {
            this.sp = GetServiceProvider(gitVersionOptions);

            this.sp.GetRequiredService<IGitPreparer>();
        });
    }

    [Test]
    public void CacheKeyForWorktree()
    {
        this.fileSystem = new FileSystem();
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        var worktreePath = GetWorktreePath(fixture);
        string? dynamicRepositoryPath = null;
        try
        {
            // create a branch and a new worktree for it
            var repo = new Repository(fixture.RepositoryPath);
            repo.Worktrees.Add("worktree", worktreePath, false);

            const string targetUrl = "https://github.com/GitTools/GitVersion.git";

            var gitVersionOptions = new GitVersionOptions { RepositoryInfo = { TargetUrl = targetUrl, TargetBranch = MainBranch }, WorkingDirectory = worktreePath };

            this.sp = GetServiceProvider(gitVersionOptions);

            sp.DiscoverRepository();

            var preparer = this.sp.GetRequiredService<IGitPreparer>();
            var repositoryInfo = this.sp.GetRequiredService<IGitRepositoryInfo>();
            preparer.Prepare();

            dynamicRepositoryPath = repositoryInfo.DynamicGitRepositoryPath;

            var cacheKeyFactory = this.sp.GetRequiredService<IGitVersionCacheKeyFactory>();
            var cacheKey = cacheKeyFactory.Create(null);
            cacheKey.Value.ShouldNotBeEmpty();
        }
        finally
        {
            CleanUpDynamicRepository(dynamicRepositoryPath);
        }
    }

    [Test]
    public void CacheFileExistsOnDisk()
    {
        var stringBuilder = new StringBuilder();

        void Action(string s) => stringBuilder.AppendLine(s);
        var testLoggerFactory = new TestLoggerFactory(Action);

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();

        var gitVersionOptions = new GitVersionOptions { WorkingDirectory = fixture.RepositoryPath };

        var gitVersionCalculator = GetGitVersionCalculator(gitVersionOptions, testLoggerFactory);

        var versionVariables = gitVersionCalculator.CalculateVersionVariables();
        versionVariables.AssemblySemVer.ShouldBe("0.0.1.0");

        var cacheKeyFactory = this.sp.GetRequiredService<IGitVersionCacheKeyFactory>();
        var cacheKey = cacheKeyFactory.Create(null);
        var cacheFileName = this.gitVersionCacheProvider.GetCacheFileName(cacheKey);

        this.fileSystem.File.WriteAllText(cacheFileName, versionCacheFileContent);
        versionVariables = gitVersionCalculator.CalculateVersionVariables();
        versionVariables.AssemblySemVer.ShouldBe("4.10.3.0");

        var logsMessages = stringBuilder.ToString();

        logsMessages.ShouldContain("Loading version variables from disk cache file", Case.Insensitive, logsMessages);
    }

    [Test]
    public void CacheFileExistsOnDiskWhenOverrideConfigIsSpecifiedVersionShouldBeDynamicallyCalculatedWithoutSavingInCache()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();

        var gitVersionOptions = new GitVersionOptions { WorkingDirectory = fixture.RepositoryPath };
        var gitVersionCalculator = GetGitVersionCalculator(gitVersionOptions);

        var versionVariables = gitVersionCalculator.CalculateVersionVariables();
        versionVariables.AssemblySemVer.ShouldBe("0.0.1.0");

        var cacheKeyFactory = this.sp.GetRequiredService<IGitVersionCacheKeyFactory>();
        var cacheKey = cacheKeyFactory.Create(null);
        var cacheFileName = this.gitVersionCacheProvider.GetCacheFileName(cacheKey);
        this.fileSystem.File.WriteAllText(cacheFileName, versionCacheFileContent);

        var cacheDirectory = this.gitVersionCacheProvider.GetCacheDirectory();

        var cacheDirectoryTimestamp = this.fileSystem.GetLastDirectoryWrite(cacheDirectory);

        var configuration = GitFlowConfigurationBuilder.New.WithTagPrefixPattern("prefix").Build();
        var overrideConfiguration = new ConfigurationHelper(configuration).Dictionary;

        gitVersionOptions = new() { WorkingDirectory = fixture.RepositoryPath, ConfigurationInfo = { OverrideConfiguration = overrideConfiguration } };

        gitVersionCalculator = GetGitVersionCalculator(gitVersionOptions);
        versionVariables = gitVersionCalculator.CalculateVersionVariables();

        versionVariables.AssemblySemVer.ShouldBe("0.0.1.0");

        var cachedDirectoryTimestampAfter = this.fileSystem.GetLastDirectoryWrite(cacheDirectory);
        cachedDirectoryTimestampAfter.ShouldBe(cacheDirectoryTimestamp, "Cache was updated when override configuration was set");
    }

    [Test]
    public void CacheFileIsMissing()
    {
        var stringBuilder = new StringBuilder();

        void Action(string s) => stringBuilder.AppendLine(s);
        var testLoggerFactory = new TestLoggerFactory(Action);

        using var fixture = new EmptyRepositoryFixture();

        var gitVersionOptions = new GitVersionOptions { WorkingDirectory = fixture.RepositoryPath };

        fixture.Repository.MakeACommit();
        var gitVersionCalculator = GetGitVersionCalculator(gitVersionOptions, testLoggerFactory, fixture.Repository.ToGitRepository());

        gitVersionCalculator.CalculateVersionVariables();

        var logsMessages = stringBuilder.ToString();
        logsMessages.ShouldMatch("(?s).*Cache file.*(?-s) not found.*");
    }

    [TestCase(ConfigurationFileLocator.DefaultFileName)]
    [TestCase(ConfigurationFileLocator.DefaultAlternativeFileName)]
    [TestCase(ConfigurationFileLocator.DefaultFileNameDotted)]
    [TestCase(ConfigurationFileLocator.DefaultAlternativeFileNameDotted)]
    public void ConfigChangeInvalidatesCache(string configFileName)
    {
        using var fixture = new EmptyRepositoryFixture();

        var gitVersionOptions = new GitVersionOptions { WorkingDirectory = fixture.RepositoryPath };

        fixture.Repository.MakeACommit();

        var gitVersionCalculator = GetGitVersionCalculator(gitVersionOptions);
        var versionVariables = gitVersionCalculator.CalculateVersionVariables();

        versionVariables.AssemblySemVer.ShouldBe("0.0.1.0");

        var cacheKeyFactory = this.sp.GetRequiredService<IGitVersionCacheKeyFactory>();
        var cacheKey = cacheKeyFactory.Create(null);
        var cacheFileName = this.gitVersionCacheProvider.GetCacheFileName(cacheKey);

        this.fileSystem.File.WriteAllText(cacheFileName, versionCacheFileContent);

        versionVariables = gitVersionCalculator.CalculateVersionVariables();
        versionVariables.AssemblySemVer.ShouldBe("4.10.3.0");

        var configPath = FileSystemHelper.Path.Combine(fixture.RepositoryPath, configFileName);
        this.fileSystem.File.WriteAllText(configPath, "next-version: 5.0.0");

        gitVersionCalculator = GetGitVersionCalculator(gitVersionOptions, fs: this.fileSystem);

        versionVariables = gitVersionCalculator.CalculateVersionVariables();
        versionVariables.AssemblySemVer.ShouldBe("5.0.0.0");
    }

    [Test]
    public void NoCacheBypassesCache()
    {
        using var fixture = new EmptyRepositoryFixture();

        var gitVersionOptions = new GitVersionOptions { WorkingDirectory = fixture.RepositoryPath };

        fixture.Repository.MakeACommit();
        var gitVersionCalculator = GetGitVersionCalculator(gitVersionOptions);

        var versionVariables = gitVersionCalculator.CalculateVersionVariables();

        versionVariables.AssemblySemVer.ShouldBe("0.0.1.0");

        var cacheKeyFactory = this.sp.GetRequiredService<IGitVersionCacheKeyFactory>();
        var cacheKey = cacheKeyFactory.Create(null);
        var cacheFileName = this.gitVersionCacheProvider.GetCacheFileName(cacheKey);

        this.fileSystem.File.WriteAllText(cacheFileName, versionCacheFileContent);
        versionVariables = gitVersionCalculator.CalculateVersionVariables();
        versionVariables.AssemblySemVer.ShouldBe("4.10.3.0");

        gitVersionOptions.Settings.NoCache = true;
        versionVariables = gitVersionCalculator.CalculateVersionVariables();
        versionVariables.AssemblySemVer.ShouldBe("0.0.1.0");
    }

    [Test]
    public void WorkingDirectoryWithoutGit()
    {
        var gitVersionOptions = new GitVersionOptions { WorkingDirectory = SysEnv.SystemDirectory };

        var exception = Assert.Throws<DirectoryNotFoundException>(() =>
        {
            var gitVersionCalculator = GetGitVersionCalculator(gitVersionOptions);
            gitVersionCalculator.CalculateVersionVariables();
        });
        exception?.Message.ShouldContain("Cannot find the .git directory");
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
        exception?.Message.ShouldContain("No commits found on the current branch.");
    }

    [Test]
    public void GetProjectRootDirectoryWorkingDirectoryWithWorktree()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();

        var worktreePath = GetWorktreePath(fixture);
        try
        {
            // create a branch and a new worktree for it
            var repo = new Repository(fixture.RepositoryPath);
            repo.Worktrees.Add("worktree", worktreePath, false);

            const string targetUrl = "https://github.com/GitTools/GitVersion.git";

            var gitVersionOptions = new GitVersionOptions { RepositoryInfo = { TargetUrl = targetUrl }, WorkingDirectory = worktreePath };

            this.sp = GetServiceProvider(gitVersionOptions);
            var repositoryInfo = this.sp.GetRequiredService<IGitRepositoryInfo>();
            repositoryInfo.ProjectRootDirectory?.TrimEnd('/', '\\').ShouldBe(worktreePath);
        }
        finally
        {
            FileSystemHelper.Directory.DeleteDirectory(worktreePath);
        }
    }

    [Test]
    public void GetProjectRootDirectoryNoWorktree()
    {
        using var fixture = new EmptyRepositoryFixture();
        const string targetUrl = "https://github.com/GitTools/GitVersion.git";

        var gitVersionOptions = new GitVersionOptions { RepositoryInfo = { TargetUrl = targetUrl }, WorkingDirectory = fixture.RepositoryPath };

        this.sp = GetServiceProvider(gitVersionOptions);
        var repositoryInfo = this.sp.GetRequiredService<IGitRepositoryInfo>();

        var expectedPath = fixture.RepositoryPath.TrimEnd('/', '\\');
        repositoryInfo.ProjectRootDirectory?.TrimEnd('/', '\\').ShouldBe(expectedPath);
    }

    [Test]
    public void GetDotGitDirectoryNoWorktree()
    {
        using var fixture = new EmptyRepositoryFixture();

        var gitVersionOptions = new GitVersionOptions { WorkingDirectory = fixture.RepositoryPath };

        this.sp = GetServiceProvider(gitVersionOptions);
        var repositoryInfo = this.sp.GetRequiredService<IGitRepositoryInfo>();

        var expectedPath = FileSystemHelper.Path.Combine(fixture.RepositoryPath, ".git");
        repositoryInfo.DotGitDirectory.ShouldBe(expectedPath);
    }

    [Test]
    public void GetDotGitDirectoryWorktree()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();

        var worktreePath = GetWorktreePath(fixture);
        try
        {
            // create a branch and a new worktree for it
            var repo = new Repository(fixture.RepositoryPath);
            repo.Worktrees.Add("worktree", worktreePath, false);

            var gitVersionOptions = new GitVersionOptions { WorkingDirectory = worktreePath };

            this.sp = GetServiceProvider(gitVersionOptions);
            var repositoryInfo = this.sp.GetRequiredService<IGitRepositoryInfo>();

            var expectedPath = FileSystemHelper.Path.Combine(fixture.RepositoryPath, ".git");
            repositoryInfo.DotGitDirectory.ShouldBe(expectedPath);
        }
        finally
        {
            FileSystemHelper.Directory.DeleteDirectory(worktreePath);
        }
    }

    [Test]
    public void CalculateVersionFromWorktreeHead()
    {
        // Setup
        this.fileSystem = new FileSystem();
        using var fixture = new EmptyRepositoryFixture();
        var repoDir = fileSystem.DirectoryInfo.New(fixture.RepositoryPath);
        var worktreePath = FileSystemHelper.Path.Combine(repoDir.Parent?.FullName, $"{repoDir.Name}-v1");

        fixture.Repository.MakeATaggedCommit("v1.0.0");
        var branchV1 = fixture.Repository.CreateBranch("support/1.0");

        fixture.Repository.MakeATaggedCommit("v2.0.0");

        fixture.Repository.Worktrees.Add(branchV1.CanonicalName, "1.0", worktreePath, false);
        using var worktreeFixture = new LocalRepositoryFixture(new(worktreePath));

        var gitVersionOptions = new GitVersionOptions { WorkingDirectory = worktreeFixture.RepositoryPath };

        var sut = GetGitVersionCalculator(gitVersionOptions, fs: this.fileSystem);

        // Execute
        var version = sut.CalculateVersionVariables();

        // Verify
        version.SemVer.ShouldBe("1.0.0");
        version.VersionSourceSemVer.ShouldBe("1.0.0");
        var commits = worktreeFixture.Repository.Head.Commits;
        version.Sha.ShouldBe(commits.First().Sha);
    }

    [Test]
    public void CalculateVersionVariables_TwoBranchHasSameCommitHeadDetachedAndNotTagged_ThrowException()
    {
        // Setup
        using var fixture = new RemoteRepositoryFixture();
        fixture.LocalRepositoryFixture.Repository.MakeACommit("Init commit");
        fixture.LocalRepositoryFixture.Repository.CreateBranch("feature/1.0");
        fixture.LocalRepositoryFixture.Checkout("feature/1.0");
        var commit = fixture.LocalRepositoryFixture.Repository.MakeACommit("feat: a new commit");
        fixture.LocalRepositoryFixture.Repository.CreateBranch("support/1.0");
        fixture.LocalRepositoryFixture.Checkout(commit.Sha);

        using var worktreeFixture = new LocalRepositoryFixture(new(fixture.LocalRepositoryFixture.RepositoryPath));
        var gitVersionOptions = new GitVersionOptions { WorkingDirectory = worktreeFixture.RepositoryPath };

        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable(AzurePipelines.EnvironmentVariableName, "true");

        this.sp = GetServiceProvider(gitVersionOptions, environment: environment);

        sp.DiscoverRepository();

        var sut = sp.GetRequiredService<IGitVersionCalculateTool>();

        // Execute & Verify
        var exception = Assert.Throws<WarningException>(() => sut.CalculateVersionVariables());
        exception?.Message.ShouldBe("Failed to try and guess branch to use. Move one of the branches along a commit to remove warning");
    }

    [Test]
    public void CalculateVersionVariables_TwoBranchHasSameCommitHeadDetachedAndTagged_ReturnSemver()
    {
        // Setup
        using var fixture = new RemoteRepositoryFixture();
        fixture.LocalRepositoryFixture.Repository.MakeACommit("Init commit");
        fixture.LocalRepositoryFixture.Repository.CreateBranch("feature/1.0");
        fixture.LocalRepositoryFixture.Checkout("feature/1.0");
        var commit = fixture.LocalRepositoryFixture.Repository.MakeACommit("feat: a new commit");
        fixture.LocalRepositoryFixture.Repository.CreateBranch("support/1.0");
        fixture.LocalRepositoryFixture.ApplyTag("1.0.1");
        fixture.LocalRepositoryFixture.Checkout(commit.Sha);

        using var worktreeFixture = new LocalRepositoryFixture(new(fixture.LocalRepositoryFixture.RepositoryPath));
        var gitVersionOptions = new GitVersionOptions { WorkingDirectory = worktreeFixture.RepositoryPath };

        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable(AzurePipelines.EnvironmentVariableName, "true");

        this.sp = GetServiceProvider(gitVersionOptions, environment: environment);

        sp.DiscoverRepository();

        var sut = sp.GetRequiredService<IGitVersionCalculateTool>();

        // Execute
        var version = sut.CalculateVersionVariables();

        // Verify
        version.SemVer.ShouldBe("1.0.1");
        version.VersionSourceSemVer.ShouldBe("1.0.1");
        var commits = worktreeFixture.Repository.Head.Commits;
        version.Sha.ShouldBe(commits.First().Sha);
    }

    [Test]
    public void CalculateVersionVariables_ShallowFetch_ThrowException()
    {
        // Setup
        using var fixture = new RemoteRepositoryFixture();
        fixture.LocalRepositoryFixture.MakeShallow();

        using var worktreeFixture = new LocalRepositoryFixture(new(fixture.LocalRepositoryFixture.RepositoryPath));
        var gitVersionOptions = new GitVersionOptions { WorkingDirectory = worktreeFixture.RepositoryPath };

        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable(AzurePipelines.EnvironmentVariableName, "true");

        this.sp = GetServiceProvider(gitVersionOptions, environment: environment);

        sp.DiscoverRepository();

        var sut = sp.GetRequiredService<IGitVersionCalculateTool>();

        // Execute & Verify
        var exception = Assert.Throws<WarningException>(() => sut.CalculateVersionVariables());
        exception?.Message.ShouldBe("Repository is a shallow clone. Git repositories must contain the full history. See https://gitversion.net/docs/reference/requirements#unshallow for more info.");
    }

    [Test]
    public void CalculateVersionVariables_ShallowFetch_WithAllowShallow_ShouldNotThrowException()
    {
        // Setup
        using var fixture = new RemoteRepositoryFixture();
        fixture.LocalRepositoryFixture.MakeShallow();

        using var worktreeFixture = new LocalRepositoryFixture(new(fixture.LocalRepositoryFixture.RepositoryPath));
        var gitVersionOptions = new GitVersionOptions
        {
            WorkingDirectory = worktreeFixture.RepositoryPath,
            Settings = { AllowShallow = true }
        };

        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable(AzurePipelines.EnvironmentVariableName, "true");

        this.sp = GetServiceProvider(gitVersionOptions, environment: environment);

        sp.DiscoverRepository();

        var sut = sp.GetRequiredService<IGitVersionCalculateTool>();

        // Execute
        var version = sut.CalculateVersionVariables();

        // Verify
        version.ShouldNotBeNull();
        var commits = worktreeFixture.Repository.Head.Commits;
        version.Sha.ShouldBe(commits.First().Sha);
    }

    [Test]
    public void CalculateVersionVariables_WithLimitedCloneDepth_AndAllowShallowTrue_ShouldCalculateVersionCorrectly()
    {
        // Setup
        using var fixture = new RemoteRepositoryFixture();
        fixture.LocalRepositoryFixture.MakeShallow();

        fixture.LocalRepositoryFixture.Repository.MakeACommit("Initial commit");
        fixture.LocalRepositoryFixture.Repository.MakeATaggedCommit("1.0.0");
        var latestCommit = fixture.LocalRepositoryFixture.Repository.MakeACommit("+semver:major");

        using var worktreeFixture = new LocalRepositoryFixture(new(fixture.LocalRepositoryFixture.RepositoryPath));

        var gitVersionOptions = new GitVersionOptions
        {
            WorkingDirectory = worktreeFixture.RepositoryPath,
            Settings = { AllowShallow = true }
        };

        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable(AzurePipelines.EnvironmentVariableName, "true");

        this.sp = GetServiceProvider(gitVersionOptions, environment: environment);
        sp.DiscoverRepository();
        var sut = sp.GetRequiredService<IGitVersionCalculateTool>();

        // Execute
        var version = sut.CalculateVersionVariables();

        // Verify
        version.ShouldNotBeNull();

        // Verify that the correct commit is used
        version.Sha.ShouldBe(latestCommit.Sha);
        version.MajorMinorPatch.ShouldBe("2.0.0");
        version.VersionSourceSemVer.ShouldBe("1.0.0");

        // Verify repository is still recognized as shallow
        var repository = this.sp.GetRequiredService<IGitRepository>();
        repository.IsShallow.ShouldBeTrue("Repository should still be shallow after version calculation");
    }

    private string GetWorktreePath(EmptyRepositoryFixture fixture)
    {
        var worktreePath = FileSystemHelper.Path.Combine(this.fileSystem.Directory.GetParent(fixture.RepositoryPath)?.FullName, Guid.NewGuid().ToString());
        return worktreePath;
    }

    private static void CleanUpDynamicRepository(string? dynamicRepositoryPath)
    {
        if (!OperatingSystem.IsWindows() && dynamicRepositoryPath != null && FileSystemHelper.Directory.Exists(dynamicRepositoryPath))
        {
            FileSystemHelper.Directory.DeleteDirectory(dynamicRepositoryPath);
        }
    }

    private IGitVersionCalculateTool GetGitVersionCalculator(GitVersionOptions gitVersionOptions, ILoggerFactory? loggerFactory = null, IGitRepository? repository = null, IFileSystem? fs = null)
    {
        this.sp = GetServiceProvider(gitVersionOptions, loggerFactory, repository, fs);

        this.fileSystem = this.sp.GetRequiredService<IFileSystem>();
        this.gitVersionCacheProvider = (GitVersionCacheProvider)this.sp.GetRequiredService<IGitVersionCacheProvider>();

        sp.DiscoverRepository();

        return this.sp.GetRequiredService<IGitVersionCalculateTool>();
    }

    private static IServiceProvider GetServiceProvider(GitVersionOptions gitVersionOptions, ILoggerFactory? loggerFactory = null, IGitRepository? repository = null, IFileSystem? fileSystem = null, IEnvironment? environment = null) =>
        ConfigureServices(services =>
        {
            services.AddSingleton<IGitVersionContextFactory, GitVersionContextFactory>();
            services.AddSingleton(sp =>
            {
                var contextFactory = sp.GetRequiredService<IGitVersionContextFactory>();
                return new Lazy<GitVersionContext>(() => contextFactory.Create());
            });
            if (loggerFactory != null)
            {
                services.RemoveAll<ILoggerFactory>();
                services.RemoveAll(typeof(ILogger<>));
                services.AddSingleton(loggerFactory);
                // Register ILogger<T> to use the factory
                services.Add(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(LoggerFromFactory<>)));
            }
            if (fileSystem != null) services.AddSingleton(fileSystem);
            if (repository != null) services.AddSingleton(repository);
            if (environment != null) services.AddSingleton(environment);
            var options = Options.Create(gitVersionOptions);
            services.AddSingleton(options);
            services.AddSingleton<IGitRepositoryInfo>(sp =>
            {
                var fs = sp.GetRequiredService<IFileSystem>();
                return new GitRepositoryInfo(fs, options);
            });
        });

    /// <summary>
    /// A wrapper that creates ILogger{T} from ILoggerFactory.
    /// </summary>
    private sealed class LoggerFromFactory<T>(ILoggerFactory factory) : ILogger<T>
    {
        private readonly ILogger logger = factory.CreateLogger(typeof(T).FullName ?? typeof(T).Name);

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => this.logger.BeginScope(state);
        public bool IsEnabled(LogLevel logLevel) => this.logger.IsEnabled(logLevel);
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => this.logger.Log(logLevel, eventId, state, exception, formatter);
    }
}
