using System;
using System.IO;
using System.Text;
using GitTools.Testing;
using GitVersion;
using NUnit.Framework;
using Shouldly;
using GitVersion.BuildServers;
using GitVersion.Configuration;
using GitVersion.OutputVariables;
using GitVersion.Cache;
using GitVersion.Configuration.Init.Wizard;
using LibGit2Sharp;
using GitVersionCore.Tests.Helpers;
using GitVersion.Logging;
using GitVersion.VersionCalculation;
using GitVersionCore.Tests.VersionCalculation;
using Microsoft.Extensions.Options;
using Environment = System.Environment;

namespace GitVersionCore.Tests
{
    [TestFixture]
    [Parallelizable(ParallelScope.None)]
    public class GitVersionExecutorTests : TestBase
    {
        private IFileSystem fileSystem;
        private IEnvironment environment;
        private ILog log;
        private IConfigFileLocator configFileLocator;
        private IBuildServerResolver buildServerResolver;
        private IGitVersionCache gitVersionCache;
        private IMetaDataCalculator metaDataCalculator;
        private IGitVersionFinder gitVersionFinder;

        [SetUp]
        public void SetUp()
        {
            fileSystem = new FileSystem();
            environment = new TestEnvironment();
            log = new NullLog();
            configFileLocator = new DefaultConfigFileLocator(fileSystem, log);
            buildServerResolver = new BuildServerResolver(null, log);
            gitVersionCache = new GitVersionCache(fileSystem, log);
            metaDataCalculator = new MetaDataCalculator();
            var baseVersionCalculator = new TestBaseVersionStrategiesCalculator(log);
            var mainlineVersionCalculator = new MainlineVersionCalculator(log, metaDataCalculator);
            var nextVersionCalculator = new NextVersionCalculator(log, metaDataCalculator, baseVersionCalculator, mainlineVersionCalculator);

            gitVersionFinder = new GitVersionFinder(log, nextVersionCalculator);
        }

        [Test]
        public void CacheKeySameAfterReNormalizing()
        {
            RepositoryScope((fixture, vv) =>
            {
                var targetUrl = "https://github.com/GitTools/GitVersion.git";
                var targetBranch = "refs/head/master";

                var arguments = new Arguments
                {
                    TargetUrl = targetUrl,
                    TargetPath = fixture.RepositoryPath
                };
                var gitPreparer = new GitPreparer(log, environment, Options.Create(arguments));
                configFileLocator = new DefaultConfigFileLocator(fileSystem, log);

                gitPreparer.Prepare(true, targetBranch);
                var cacheKey1 = GitVersionCacheKeyFactory.Create(fileSystem, log, gitPreparer, configFileLocator, null);
                gitPreparer.Prepare(true, targetBranch);
                var cacheKey2 = GitVersionCacheKeyFactory.Create(fileSystem, log, gitPreparer, configFileLocator, null);

                cacheKey2.Value.ShouldBe(cacheKey1.Value);
            });
        }

        [Test]
        public void GitPreparerShouldNotFailWhenTargetPathNotInitialized()
        {
            RepositoryScope((fixture, vv) =>
            {
                var targetUrl = "https://github.com/GitTools/GitVersion.git";

                var arguments = new Arguments
                {
                    TargetUrl = targetUrl,
                    TargetPath = null
                };
                var options = Options.Create(arguments);
                Should.NotThrow(() => new GitPreparer(log, environment, options));
            });
        }

        [Test]
        [Category("NoMono")]
        [Description("LibGit2Sharp fails here when running under Mono")]
        public void CacheKeyForWorktree()
        {
            RepositoryScope((fixture, vv) =>
            {
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

                    var gitPreparer = new GitPreparer(log, environment, Options.Create(arguments));
                    configFileLocator = new DefaultConfigFileLocator(fileSystem, log);
                    var cacheKey = GitVersionCacheKeyFactory.Create(fileSystem, log, gitPreparer, configFileLocator, null);
                    cacheKey.Value.ShouldNotBeEmpty();
                }
                finally
                {
                    DirectoryHelper.DeleteDirectory(worktreePath);
                }
            });
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

            gitVersionCache = new GitVersionCache(fileSystem, log);

            RepositoryScope(log, (fixture, vv) =>
            {
                fileSystem.WriteAllText(vv.FileName, versionCacheFileContent);

                var arguments = new Arguments { TargetPath = fixture.RepositoryPath };
                var gitVersionCalculator = GetGitVersionCalculator(arguments);

                vv = gitVersionCalculator.CalculateVersionVariables();
                vv.AssemblySemVer.ShouldBe("4.10.3.0");
            });

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

            RepositoryScope((fixture, vv) =>
            {
                fileSystem.WriteAllText(vv.FileName, versionCacheFileContent);

                var arguments = new Arguments
                {
                    TargetPath = fixture.RepositoryPath
                };

                var gitPreparer = new GitPreparer(log, environment, Options.Create(arguments));

                gitVersionCache = new GitVersionCache(fileSystem, log);
                var cacheDirectory = gitVersionCache.GetCacheDirectory(gitPreparer);

                var cacheDirectoryTimestamp = fileSystem.GetLastDirectoryWrite(cacheDirectory);

                arguments = new Arguments { TargetPath = fixture.RepositoryPath, OverrideConfig = new Config { TagPrefix = "prefix" } };

                var gitVersionCalculator = GetGitVersionCalculator(arguments);
                vv = gitVersionCalculator.CalculateVersionVariables();

                vv.AssemblySemVer.ShouldBe("0.1.0.0");

                var cachedDirectoryTimestampAfter = fileSystem.GetLastDirectoryWrite(cacheDirectory);
                cachedDirectoryTimestampAfter.ShouldBe(cacheDirectoryTimestamp, () => "Cache was updated when override config was set");
            });
        }

        [Test]
        public void CacheFileIsMissing()
        {
            var stringBuilder = new StringBuilder();
            void Action(string s) => stringBuilder.AppendLine(s);

            var logAppender = new TestLogAppender(Action);
            log = new Log(logAppender);
            gitVersionCache = new GitVersionCache(fileSystem, log);

            RepositoryScope();
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

            RepositoryScope((fixture, vv) =>
            {
                fileSystem.WriteAllText(vv.FileName, versionCacheFileContent);
                var arguments = new Arguments { TargetPath = fixture.RepositoryPath };

                var gitVersionCalculator = GetGitVersionCalculator(arguments);

                vv = gitVersionCalculator.CalculateVersionVariables();
                vv.AssemblySemVer.ShouldBe("4.10.3.0");

                var configPath = Path.Combine(fixture.RepositoryPath, "GitVersionConfig.yaml");
                fileSystem.WriteAllText(configPath, "next-version: 5.0");

                vv = gitVersionCalculator.CalculateVersionVariables();
                vv.AssemblySemVer.ShouldBe("5.0.0.0");
            });
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

            RepositoryScope((fixture, vv) =>
            {
                var arguments = new Arguments { TargetPath = fixture.RepositoryPath };

                var gitVersionCalculator = GetGitVersionCalculator(arguments);

                fileSystem.WriteAllText(vv.FileName, versionCacheFileContent);
                vv = gitVersionCalculator.CalculateVersionVariables();
                vv.AssemblySemVer.ShouldBe("4.10.3.0");

                arguments.NoCache = true;
                vv = gitVersionCalculator.CalculateVersionVariables();
                vv.AssemblySemVer.ShouldBe("0.1.0.0");
            });
        }


        [Test]
        public void WorkingDirectoryWithoutGit()
        {
            RepositoryScope((fixture, vv) =>
            {
                var arguments = new Arguments { TargetPath = Environment.SystemDirectory };

                var gitVersionCalculator = GetGitVersionCalculator(arguments);

                var exception = Assert.Throws<DirectoryNotFoundException>(() => gitVersionCalculator.CalculateVersionVariables());
                exception.Message.ShouldContain("Can't find the .git directory in");
            });
        }

        [Test]
        [Category("NoMono")]
        [Description("LibGit2Sharp fails when running under Mono")]
        public void GetProjectRootDirectoryWorkingDirectoryWithWorktree()
        {
            RepositoryScope((fixture, vv) =>
            {
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

                    var gitPreparer = new GitPreparer(log, environment, Options.Create(arguments));

                    gitPreparer.GetProjectRootDirectoryInternal().TrimEnd('/', '\\').ShouldBe(worktreePath);
                }
                finally
                {
                    DirectoryHelper.DeleteDirectory(worktreePath);
                }
            });
        }

        [Test]
        public void GetProjectRootDirectoryNoWorktree()
        {
            RepositoryScope((fixture, vv) =>
            {
                var targetUrl = "https://github.com/GitTools/GitVersion.git";

                var arguments = new Arguments
                {
                    TargetUrl = targetUrl,
                    TargetPath = fixture.RepositoryPath
                };

                var gitPreparer = new GitPreparer(log, environment, Options.Create(arguments));
                var expectedPath = fixture.RepositoryPath.TrimEnd('/', '\\');
                gitPreparer.GetProjectRootDirectoryInternal().TrimEnd('/', '\\').ShouldBe(expectedPath);
            });
        }

        [Test]
        public void DynamicRepositoriesShouldNotErrorWithFailedToFindGitDirectory()
        {
            RepositoryScope((fixture, vv) =>
            {
                var arguments = new Arguments
                {
                    TargetPath = fixture.RepositoryPath,
                    TargetUrl = "https://github.com/GitTools/GitVersion.git",
                    TargetBranch = "refs/head/master"
                };

                var gitVersionCalculator = GetGitVersionCalculator(arguments);

                gitVersionCalculator.CalculateVersionVariables();
            });
        }

        [Test]
        public void GetDotGitDirectoryNoWorktree()
        {
            RepositoryScope((fixture, vv) =>
            {
                var targetUrl = "https://github.com/GitTools/GitVersion.git";

                var arguments = new Arguments
                {
                    TargetUrl = targetUrl,
                    TargetPath = fixture.RepositoryPath
                };

                var gitPreparer = new GitPreparer(log, environment, Options.Create(arguments));
                var expectedPath = Path.Combine(fixture.RepositoryPath, ".git");
                gitPreparer.GetDotGitDirectory().ShouldBe(expectedPath);
            });
        }

        [Test]
        [Category("NoMono")]
        [Description("LibGit2Sharp fails when running under Mono")]
        public void GetDotGitDirectoryWorktree()
        {
            RepositoryScope((fixture, vv) =>
            {
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

                    var gitPreparer = new GitPreparer(log, environment, Options.Create(arguments));
                    var expectedPath = Path.Combine(fixture.RepositoryPath, ".git");
                    gitPreparer.GetDotGitDirectory().ShouldBe(expectedPath);
                }
                finally
                {
                    DirectoryHelper.DeleteDirectory(worktreePath);
                }
            });
        }

        private void RepositoryScope(Action<EmptyRepositoryFixture, VersionVariables> fixtureAction = null)
        {
            // Make sure GitVersion doesn't trigger build server mode when we are running the tests
            environment.SetEnvironmentVariable(AppVeyor.EnvironmentVariableName, null);
            environment.SetEnvironmentVariable(TravisCi.EnvironmentVariableName, null);
            environment.SetEnvironmentVariable(AzurePipelines.EnvironmentVariableName, null);

            using var fixture = new EmptyRepositoryFixture();

            var arguments = new Arguments { TargetPath = fixture.RepositoryPath };

            var gitVersionCalculator = GetGitVersionCalculator(arguments);

            fixture.Repository.MakeACommit();
            var vv = gitVersionCalculator.CalculateVersionVariables();

            vv.AssemblySemVer.ShouldBe("0.1.0.0");
            vv.FileName.ShouldNotBeNullOrEmpty();

            fixtureAction?.Invoke(fixture, vv);
        }

        private void RepositoryScope(ILog log, Action<EmptyRepositoryFixture, VersionVariables> fixtureAction = null)
        {
            // Make sure GitVersion doesn't trigger build server mode when we are running the tests
            environment.SetEnvironmentVariable(AppVeyor.EnvironmentVariableName, null);
            environment.SetEnvironmentVariable(TravisCi.EnvironmentVariableName, null);
            environment.SetEnvironmentVariable(AzurePipelines.EnvironmentVariableName, null);

            using var fixture = new EmptyRepositoryFixture();

            var arguments = new Arguments { TargetPath = fixture.RepositoryPath };
            var options = Options.Create(arguments);

            var gitPreparer = new GitPreparer(log, environment, options);
            var stepFactory = new ConfigInitStepFactory();
            var configInitWizard = new ConfigInitWizard(new ConsoleAdapter(), stepFactory);
            var configurationProvider = new ConfigProvider(fileSystem, log, configFileLocator, gitPreparer, configInitWizard);
            var baseVersionCalculator = new BaseVersionCalculator(this.log, null);
            var mainlineVersionCalculator = new MainlineVersionCalculator(this.log, metaDataCalculator);
            var nextVersionCalculator = new NextVersionCalculator(this.log, metaDataCalculator, baseVersionCalculator, mainlineVersionCalculator);
            var variableProvider = new VariableProvider(nextVersionCalculator, new TestEnvironment());
            var gitVersionCalculator = new GitVersionCalculator(fileSystem, log, configFileLocator, configurationProvider, buildServerResolver, gitVersionCache, gitVersionFinder, gitPreparer, variableProvider, options);

            fixture.Repository.MakeACommit();
            var vv = gitVersionCalculator.CalculateVersionVariables();

            vv.AssemblySemVer.ShouldBe("0.1.0.0");
            vv.FileName.ShouldNotBeNullOrEmpty();

            fixtureAction?.Invoke(fixture, vv);
        }

        private GitVersionCalculator GetGitVersionCalculator(Arguments arguments)
        {
            var options = Options.Create(arguments);

            var gitPreparer = new GitPreparer(log, environment, options);
            var stepFactory = new ConfigInitStepFactory();
            var configInitWizard = new ConfigInitWizard(new ConsoleAdapter(), stepFactory);
            var configurationProvider = new ConfigProvider(fileSystem, log, configFileLocator, gitPreparer, configInitWizard);
            var baseVersionCalculator = new BaseVersionCalculator(log, null);
            var mainlineVersionCalculator = new MainlineVersionCalculator(log, metaDataCalculator);
            var nextVersionCalculator = new NextVersionCalculator(log, metaDataCalculator, baseVersionCalculator, mainlineVersionCalculator);
            var variableProvider = new VariableProvider(nextVersionCalculator, new TestEnvironment());
            var gitVersionCalculator = new GitVersionCalculator(fileSystem, log, configFileLocator, configurationProvider, buildServerResolver, gitVersionCache, gitVersionFinder, gitPreparer, variableProvider, options);
            return gitVersionCalculator;
        }
    }
}
