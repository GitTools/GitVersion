using System;
using System.IO;
using System.Text;
using GitTools.Testing;
using GitVersion;
using GitVersion.Configuration;
using GitVersion.Logging;
using GitVersion.VersionCalculation.Cache;
using GitVersionCore.Tests.Helpers;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;
using Environment = System.Environment;

namespace GitVersionCore.Tests
{
    [TestFixture]
    [Parallelizable(ParallelScope.None)]
    public class GitVersionExecutorTests : TestBase
    {
        private IFileSystem fileSystem;
        private ILog log;
        private IGitVersionCache gitVersionCache;
        private IGitPreparer gitPreparer;
        private IServiceProvider sp;

        [Test]
        public void CacheKeySameAfterReNormalizing()
        {
            using var fixture = new EmptyRepositoryFixture();
            var targetUrl = "https://github.com/GitTools/GitVersion.git";
            var targetBranch = "refs/head/master";

            var arguments = new Arguments
            {
                TargetUrl = targetUrl,
                TargetPath = fixture.RepositoryPath
            };

            sp = GetServiceProvider(arguments);

            var preparer = sp.GetService<IGitPreparer>() as GitPreparer;

            preparer?.PrepareInternal(true, targetBranch);
            var cacheKeyFactory = sp.GetService<IGitVersionCacheKeyFactory>();
            var cacheKey1 = cacheKeyFactory.Create(null);
            preparer?.PrepareInternal(true, targetBranch);
            var cacheKey2 = cacheKeyFactory.Create(null);

            cacheKey2.Value.ShouldBe(cacheKey1.Value);
        }

        [Test]
        public void GitPreparerShouldNotFailWhenTargetPathNotInitialized()
        {
            var targetUrl = "https://github.com/GitTools/GitVersion.git";

            var arguments = new Arguments
            {
                TargetUrl = targetUrl,
                TargetPath = null
            };
            Should.NotThrow(() =>
            {
                sp = GetServiceProvider(arguments);

                sp.GetService<IGitPreparer>();
            });
        }

        [Test]
        [Category("NoMono")]
        [Description("LibGit2Sharp fails here when running under Mono")]
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

                var arguments = new Arguments
                {
                    TargetUrl = targetUrl,
                    TargetPath = worktreePath
                };

                sp = GetServiceProvider(arguments);
                var cacheKey = sp.GetService<IGitVersionCacheKeyFactory>().Create(null);
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
        ";

            var stringBuilder = new StringBuilder();
            void Action(string s) => stringBuilder.AppendLine(s);

            var logAppender = new TestLogAppender(Action);
            log = new Log(logAppender);

            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeACommit();

            var arguments = new Arguments { TargetPath = fixture.RepositoryPath };

            var gitVersionCalculator = GetGitVersionCalculator(arguments, log);

            var versionVariables = gitVersionCalculator.CalculateVersionVariables();
            versionVariables.AssemblySemVer.ShouldBe("0.1.0.0");

            fileSystem.WriteAllText(versionVariables.FileName, versionCacheFileContent);
            versionVariables = gitVersionCalculator.CalculateVersionVariables();
            versionVariables.AssemblySemVer.ShouldBe("4.10.3.0");

            var logsMessages = stringBuilder.ToString();

            logsMessages.ShouldContain("Deserializing version variables from cache file", () => logsMessages);
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
        ";

            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeACommit();

            var arguments = new Arguments { TargetPath = fixture.RepositoryPath };
            var gitVersionCalculator = GetGitVersionCalculator(arguments, log);

            var versionVariables = gitVersionCalculator.CalculateVersionVariables();
            versionVariables.AssemblySemVer.ShouldBe("0.1.0.0");

            fileSystem.WriteAllText(versionVariables.FileName, versionCacheFileContent);

            var cacheDirectory = gitVersionCache.GetCacheDirectory();

            var cacheDirectoryTimestamp = fileSystem.GetLastDirectoryWrite(cacheDirectory);

            var config = new Config { TagPrefix = "prefix" };
            config.Reset();
            arguments = new Arguments { TargetPath = fixture.RepositoryPath, OverrideConfig = config };

            gitVersionCalculator = GetGitVersionCalculator(arguments);
            versionVariables = gitVersionCalculator.CalculateVersionVariables();

            versionVariables.AssemblySemVer.ShouldBe("0.1.0.0");

            var cachedDirectoryTimestampAfter = fileSystem.GetLastDirectoryWrite(cacheDirectory);
            cachedDirectoryTimestampAfter.ShouldBe(cacheDirectoryTimestamp, () => "Cache was updated when override config was set");
        }

        [Test]
        public void CacheFileIsMissing()
        {
            var stringBuilder = new StringBuilder();
            void Action(string s) => stringBuilder.AppendLine(s);

            var logAppender = new TestLogAppender(Action);
            log = new Log(logAppender);

            using var fixture = new EmptyRepositoryFixture();

            var arguments = new Arguments { TargetPath = fixture.RepositoryPath };

            fixture.Repository.MakeACommit();
            var gitVersionCalculator = GetGitVersionCalculator(arguments, log, fixture.Repository);

            gitVersionCalculator.CalculateVersionVariables();

            var logsMessages = stringBuilder.ToString();
            logsMessages.ShouldContain("yml not found", () => logsMessages);
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
        ";

            using var fixture = new EmptyRepositoryFixture();

            var arguments = new Arguments { TargetPath = fixture.RepositoryPath };

            fixture.Repository.MakeACommit();

            var gitVersionCalculator = GetGitVersionCalculator(arguments);
            var versionVariables = gitVersionCalculator.CalculateVersionVariables();

            versionVariables.AssemblySemVer.ShouldBe("0.1.0.0");
            versionVariables.FileName.ShouldNotBeNullOrEmpty();

            fileSystem.WriteAllText(versionVariables.FileName, versionCacheFileContent);

            versionVariables = gitVersionCalculator.CalculateVersionVariables();
            versionVariables.AssemblySemVer.ShouldBe("4.10.3.0");

            var configPath = Path.Combine(fixture.RepositoryPath, DefaultConfigFileLocator.DefaultFileName);
            fileSystem.WriteAllText(configPath, "next-version: 5.0");

            gitVersionCalculator = GetGitVersionCalculator(arguments, fs: fileSystem);

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
        ";

            using var fixture = new EmptyRepositoryFixture();

            var arguments = new Arguments { TargetPath = fixture.RepositoryPath };

            fixture.Repository.MakeACommit();
            var gitVersionCalculator = GetGitVersionCalculator(arguments);

            var versionVariables = gitVersionCalculator.CalculateVersionVariables();

            versionVariables.AssemblySemVer.ShouldBe("0.1.0.0");
            versionVariables.FileName.ShouldNotBeNullOrEmpty();

            fileSystem.WriteAllText(versionVariables.FileName, versionCacheFileContent);
            versionVariables = gitVersionCalculator.CalculateVersionVariables();
            versionVariables.AssemblySemVer.ShouldBe("4.10.3.0");

            arguments.NoCache = true;
            versionVariables = gitVersionCalculator.CalculateVersionVariables();
            versionVariables.AssemblySemVer.ShouldBe("0.1.0.0");
        }

        [Test]
        public void WorkingDirectoryWithoutGit()
        {
            var arguments = new Arguments { TargetPath = Environment.SystemDirectory };


            var exception = Assert.Throws<DirectoryNotFoundException>(() =>
            {
                var gitVersionCalculator = GetGitVersionCalculator(arguments);
                gitVersionCalculator.CalculateVersionVariables();
            });
            exception.Message.ShouldContain("Can't find the .git directory in");
        }

        [Test]
        [Category("NoMono")]
        [Description("LibGit2Sharp fails when running under Mono")]
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

                var arguments = new Arguments
                {
                    TargetUrl = targetUrl,
                    TargetPath = worktreePath
                };

                sp = GetServiceProvider(arguments);

                arguments.ProjectRootDirectory.TrimEnd('/', '\\').ShouldBe(worktreePath);
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

            var arguments = new Arguments
            {
                TargetUrl = targetUrl,
                TargetPath = fixture.RepositoryPath
            };

            sp = GetServiceProvider(arguments);

            var expectedPath = fixture.RepositoryPath.TrimEnd('/', '\\');
            arguments.ProjectRootDirectory.TrimEnd('/', '\\').ShouldBe(expectedPath);
        }

        [Test]
        public void DynamicRepositoriesShouldNotErrorWithFailedToFindGitDirectory()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeACommit();

            var arguments = new Arguments
            {
                TargetPath = fixture.RepositoryPath,
                TargetUrl = "https://github.com/GitTools/GitVersion.git",
                TargetBranch = "refs/head/master"
            };

            var gitVersionCalculator = GetGitVersionCalculator(arguments, repository: fixture.Repository);
            gitPreparer.Prepare();
            gitVersionCalculator.CalculateVersionVariables();
        }

        [Test]
        public void GetDotGitDirectoryNoWorktree()
        {
            using var fixture = new EmptyRepositoryFixture();
            var targetUrl = "https://github.com/GitTools/GitVersion.git";

            var arguments = new Arguments
            {
                TargetUrl = targetUrl,
                TargetPath = fixture.RepositoryPath
            };

            sp = GetServiceProvider(arguments);

            var expectedPath = Path.Combine(fixture.RepositoryPath, ".git");
            arguments.DotGitDirectory.ShouldBe(expectedPath);
        }

        [Test]
        [Category("NoMono")]
        [Description("LibGit2Sharp fails when running under Mono")]
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

                var targetUrl = "https://github.com/GitTools/GitVersion.git";

                var arguments = new Arguments
                {
                    TargetUrl = targetUrl,
                    TargetPath = worktreePath
                };

                sp = GetServiceProvider(arguments);

                var expectedPath = Path.Combine(fixture.RepositoryPath, ".git");
                arguments.DotGitDirectory.ShouldBe(expectedPath);
            }
            finally
            {
                DirectoryHelper.DeleteDirectory(worktreePath);
            }
        }

        private IGitVersionCalculator GetGitVersionCalculator(Arguments arguments, ILog logger = null, IRepository repository = null, IFileSystem fs = null)
        {
            sp = GetServiceProvider(arguments, logger, repository, fs);

            fileSystem = sp.GetService<IFileSystem>();
            log = sp.GetService<ILog>();
            gitVersionCache = sp.GetService<IGitVersionCache>();
            gitPreparer = sp.GetService<IGitPreparer>();

            return sp.GetService<IGitVersionCalculator>();
        }

        private static IServiceProvider GetServiceProvider(Arguments arguments, ILog log = null, IRepository repository = null, IFileSystem fileSystem = null)
        {
            return ConfigureServices(services =>
            {
                if (log != null) services.AddSingleton(log);
                if (fileSystem != null) services.AddSingleton(fileSystem);
                if (repository != null) services.AddSingleton(repository);
                services.AddSingleton(Options.Create(arguments));
            });
        }
    }
}
