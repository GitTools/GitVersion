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
using LibGit2Sharp;
using GitVersionCore.Tests.Helpers;
using GitVersion.Common;
using GitVersion.Logging;
using Environment = System.Environment;

namespace GitVersionCore.Tests
{
    [TestFixture]
    [Parallelizable(ParallelScope.None)]
    public class ExecuteCoreTests : TestBase
    {
        private IFileSystem fileSystem;
        private IEnvironment environment;
        private ILog log;

        [SetUp]
        public void SetUp()
        {
            fileSystem = new FileSystem();
            environment = new TestEnvironment();
            log = new NullLog();
        }

        [Test]
        public void CacheKeySameAfterReNormalizing()
        {
            var versionAndBranchFinder = new ExecuteCore(fileSystem, environment, log);

            RepositoryScope(versionAndBranchFinder, (fixture, vv) =>
            {
                var targetUrl = "https://github.com/GitTools/GitVersion.git";
                var targetBranch = "refs/head/master";
                var gitPreparer = new GitPreparer(log, targetUrl, null, new Authentication(), false, fixture.RepositoryPath);
                var configFileLocator = new DefaultConfigFileLocator(fileSystem, log);
                gitPreparer.Initialise(true, targetBranch);
                var cacheKey1 = GitVersionCacheKeyFactory.Create(fileSystem, log, gitPreparer, null, configFileLocator);
                gitPreparer.Initialise(true, targetBranch);
                var cacheKey2 = GitVersionCacheKeyFactory.Create(fileSystem, log, gitPreparer, null, configFileLocator);

                cacheKey2.Value.ShouldBe(cacheKey1.Value);
            });
        }

        [Test]
        [Category("NoMono")]
        [Description("LibGit2Sharp fails here when running under Mono")]
        public void CacheKeyForWorktree()
        {
            var versionAndBranchFinder = new ExecuteCore(fileSystem, environment, log);

            RepositoryScope(versionAndBranchFinder, (fixture, vv) =>
            {
                var worktreePath = Path.Combine(Directory.GetParent(fixture.RepositoryPath).FullName, Guid.NewGuid().ToString());
                try
                {
                    // create a branch and a new worktree for it
                    var repo = new Repository(fixture.RepositoryPath);
                    repo.Worktrees.Add("worktree", worktreePath, false);

                    var targetUrl = "https://github.com/GitTools/GitVersion.git";
                    var gitPreparer = new GitPreparer(log, targetUrl, null, new Authentication(), false, worktreePath);
                    var configFileLocator = new DefaultConfigFileLocator(fileSystem, log);
                    var cacheKey = GitVersionCacheKeyFactory.Create(fileSystem, log, gitPreparer, null, configFileLocator);
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

            var versionAndBranchFinder = new ExecuteCore(fileSystem, environment, log);

            RepositoryScope(versionAndBranchFinder, (fixture, vv) =>
            {
                fileSystem.WriteAllText(vv.FileName, versionCacheFileContent);
                vv = versionAndBranchFinder.ExecuteGitVersion(null, null, null, null, false, fixture.RepositoryPath, null);
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

            var versionAndBranchFinder = new ExecuteCore(fileSystem, environment, log);

            RepositoryScope(versionAndBranchFinder, (fixture, vv) =>
            {
                fileSystem.WriteAllText(vv.FileName, versionCacheFileContent);

                var gitPreparer = new GitPreparer(log, null, null, null, false, fixture.RepositoryPath);
                var cacheDirectory = GitVersionCache.GetCacheDirectory(gitPreparer);

                var cacheDirectoryTimestamp = fileSystem.GetLastDirectoryWrite(cacheDirectory);

                vv = versionAndBranchFinder.ExecuteGitVersion(null, null, null, null, false, fixture.RepositoryPath, null, new Config() { TagPrefix = "prefix" });

                vv.AssemblySemVer.ShouldBe("0.1.0.0");

                var cachedDirectoryTimestampAfter = fileSystem.GetLastDirectoryWrite(cacheDirectory);
                cachedDirectoryTimestampAfter.ShouldBe(cacheDirectoryTimestamp, () => "Cache was updated when override config was set");
            });

            // TODO info.ShouldContain("Override config from command line", () => info);
        }

        [Test]
        public void CacheFileIsMissing()
        {
            var stringBuilder = new StringBuilder();
            void Action(string s) => stringBuilder.AppendLine(s);

            var logAppender = new TestLogAppender(Action);
            log = new Log(logAppender);

            var executeCore = new ExecuteCore(fileSystem, environment, log);

            RepositoryScope(executeCore);
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

            var versionAndBranchFinder = new ExecuteCore(fileSystem, environment, log);

            RepositoryScope(versionAndBranchFinder, (fixture, vv) =>
            {
                fileSystem.WriteAllText(vv.FileName, versionCacheFileContent);
                vv = versionAndBranchFinder.ExecuteGitVersion(null, null, null, null, false, fixture.RepositoryPath, null);
                vv.AssemblySemVer.ShouldBe("4.10.3.0");

                var configPath = Path.Combine(fixture.RepositoryPath, "GitVersionConfig.yaml");
                fileSystem.WriteAllText(configPath, "next-version: 5.0");

                vv = versionAndBranchFinder.ExecuteGitVersion(null, null, null, null, false, fixture.RepositoryPath, null);
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

            var versionAndBranchFinder = new ExecuteCore(fileSystem, environment, log);

            RepositoryScope(versionAndBranchFinder, (fixture, vv) =>
            {
                fileSystem.WriteAllText(vv.FileName, versionCacheFileContent);
                vv = versionAndBranchFinder.ExecuteGitVersion(null, null, null, null, false, fixture.RepositoryPath, null);
                vv.AssemblySemVer.ShouldBe("4.10.3.0");

                vv = versionAndBranchFinder.ExecuteGitVersion(null, null, null, null, false, fixture.RepositoryPath, null, noCache: true);
                vv.AssemblySemVer.ShouldBe("0.1.0.0");
            });
        }


        [Test]
        public void WorkingDirectoryWithoutGit()
        {
            var versionAndBranchFinder = new ExecuteCore(fileSystem, environment, log);

            RepositoryScope(versionAndBranchFinder, (fixture, vv) =>
            {
                var exception = Assert.Throws<DirectoryNotFoundException>(() => versionAndBranchFinder.ExecuteGitVersion(null, null, null, null, false, Environment.SystemDirectory, null));
                exception.Message.ShouldContain("Can't find the .git directory in");
            });
        }

        [Test]
        [Category("NoMono")]
        [Description("LibGit2Sharp fails when running under Mono")]
        public void GetProjectRootDirectory_WorkingDirectoryWithWorktree()
        {
            var versionAndBranchFinder = new ExecuteCore(fileSystem, environment, log);

            RepositoryScope(versionAndBranchFinder, (fixture, vv) =>
            {
                var worktreePath = Path.Combine(Directory.GetParent(fixture.RepositoryPath).FullName, Guid.NewGuid().ToString());
                try
                {
                    // create a branch and a new worktree for it
                    var repo = new Repository(fixture.RepositoryPath);
                    repo.Worktrees.Add("worktree", worktreePath, false);

                    var targetUrl = "https://github.com/GitTools/GitVersion.git";
                    var gitPreparer = new GitPreparer(log, targetUrl, null, new Authentication(), false, worktreePath);
                    gitPreparer.GetProjectRootDirectory().TrimEnd('/', '\\').ShouldBe(worktreePath);
                }
                finally
                {
                    DirectoryHelper.DeleteDirectory(worktreePath);
                }
            });
        }

        [Test]
        public void GetProjectRootDirectory_NoWorktree()
        {
            var versionAndBranchFinder = new ExecuteCore(fileSystem, environment, log);

            RepositoryScope(versionAndBranchFinder, (fixture, vv) =>
            {
                var targetUrl = "https://github.com/GitTools/GitVersion.git";
                var gitPreparer = new GitPreparer(log, targetUrl, null, new Authentication(), false, fixture.RepositoryPath);
                var expectedPath = fixture.RepositoryPath.TrimEnd('/', '\\');
                gitPreparer.GetProjectRootDirectory().TrimEnd('/', '\\').ShouldBe(expectedPath);
            });
        }

        [Test]
        public void DynamicRepositoriesShouldNotErrorWithFailedToFindGitDirectory()
        {
            var versionAndBranchFinder = new ExecuteCore(fileSystem, environment, log);

            RepositoryScope(versionAndBranchFinder, (fixture, vv) =>
            {
                versionAndBranchFinder.ExecuteGitVersion("https://github.com/GitTools/GitVersion.git", null, new Authentication(), "refs/head/master", false, fixture.RepositoryPath, null);
            });
        }

        [Test]
        public void GetDotGitDirectory_NoWorktree()
        {
            var versionAndBranchFinder = new ExecuteCore(fileSystem, environment, log);

            RepositoryScope(versionAndBranchFinder, (fixture, vv) =>
            {
                var targetUrl = "https://github.com/GitTools/GitVersion.git";
                var gitPreparer = new GitPreparer(log, targetUrl, null, new Authentication(), false, fixture.RepositoryPath);
                var expectedPath = Path.Combine(fixture.RepositoryPath, ".git");
                gitPreparer.GetDotGitDirectory().ShouldBe(expectedPath);
            });
        }

        [Test]
        [Category("NoMono")]
        [Description("LibGit2Sharp fails when running under Mono")]
        public void GetDotGitDirectory_Worktree()
        {
            var versionAndBranchFinder = new ExecuteCore(fileSystem, environment, log);

            RepositoryScope(versionAndBranchFinder, (fixture, vv) =>
            {
                var worktreePath = Path.Combine(Directory.GetParent(fixture.RepositoryPath).FullName, Guid.NewGuid().ToString());
                try
                {

                    // create a branch and a new worktree for it
                    var repo = new Repository(fixture.RepositoryPath);
                    repo.Worktrees.Add("worktree", worktreePath, false);

                    var targetUrl = "https://github.com/GitTools/GitVersion.git";
                    var gitPreparer = new GitPreparer(log, targetUrl, null, new Authentication(), false, worktreePath);
                    var expectedPath = Path.Combine(fixture.RepositoryPath, ".git");
                    gitPreparer.GetDotGitDirectory().ShouldBe(expectedPath);
                }
                finally
                {
                    DirectoryHelper.DeleteDirectory(worktreePath);
                }
            });
        }

        private void RepositoryScope(ExecuteCore executeCore, Action<EmptyRepositoryFixture, VersionVariables> fixtureAction = null)
        {
            // Make sure GitVersion doesn't trigger build server mode when we are running the tests
            environment.SetEnvironmentVariable(AppVeyor.EnvironmentVariableName, null);
            environment.SetEnvironmentVariable(TravisCI.EnvironmentVariableName, null);
            environment.SetEnvironmentVariable(AzurePipelines.EnvironmentVariableName, null);

            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.Repository.MakeACommit();
                var vv = executeCore.ExecuteGitVersion(null, null, null, null, false, fixture.RepositoryPath, null);

                vv.AssemblySemVer.ShouldBe("0.1.0.0");
                vv.FileName.ShouldNotBeNullOrEmpty();

                fixtureAction?.Invoke(fixture, vv);
            }
        }
    }
}
