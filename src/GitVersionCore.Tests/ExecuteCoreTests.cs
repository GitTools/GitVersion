using GitTools.Testing;
using GitVersion;
using GitVersion.Helpers;
using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using System.Text;

[TestFixture]
public class ExecuteCoreTests
{
    IFileSystem fileSystem;

    [SetUp]
    public void SetUp()
    {
        fileSystem = new FileSystem();
    }

    [Test]
    public void CacheKeySameAfterReNormalizing()
    {
        var versionAndBranchFinder = new ExecuteCore(fileSystem);

        RepositoryScope(versionAndBranchFinder, (fixture, vv) =>
        {
            var targetUrl = "https://github.com/GitTools/GitVersion.git";
            var targetBranch = "refs/head/master";
            var gitPreparer = new GitPreparer(targetUrl, null, new Authentication(), false, fixture.RepositoryPath);
            gitPreparer.Initialise(true, targetBranch);
            var cacheKey1 = GitVersionCacheKeyFactory.Create(fileSystem, gitPreparer, null);
            gitPreparer.Initialise(true, targetBranch);
            var cacheKey2 = GitVersionCacheKeyFactory.Create(fileSystem, gitPreparer, null);

            cacheKey2.Value.ShouldBe(cacheKey1.Value);
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
BuildMetaData: 
BuildMetaDataPadded: 
FullBuildMetaData: Branch.feature/test.Sha.dd2a29aff0c948e1bdf3dabbe13e1576e70d5f9f
MajorMinorPatch: 4.10.3
SemVer: 4.10.3-test.19
LegacySemVer: 4.10.3-test19
LegacySemVerPadded: 4.10.3-test0019
AssemblySemVer: 4.10.3.0
FullSemVer: 4.10.3-test.19
InformationalVersion: 4.10.3-test.19+Branch.feature/test.Sha.dd2a29aff0c948e1bdf3dabbe13e1576e70d5f9f
BranchName: feature/test
Sha: dd2a29aff0c948e1bdf3dabbe13e1576e70d5f9f
NuGetVersionV2: 4.10.3-test0019
NuGetVersion: 4.10.3-test0019
CommitsSinceVersionSource: 19
CommitsSinceVersionSourcePadded: 0019
CommitDate: 2015-11-10
";

        var versionAndBranchFinder = new ExecuteCore(fileSystem);

        var info = RepositoryScope(versionAndBranchFinder, (fixture, vv) =>
        {
            fileSystem.WriteAllText(vv.FileName, versionCacheFileContent);
            vv = versionAndBranchFinder.ExecuteGitVersion(null, null, null, null, false, fixture.RepositoryPath, null);
            vv.AssemblySemVer.ShouldBe("4.10.3.0");
        });

        info.ShouldContain("Deserializing version variables from cache file", () => info);
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
FullSemVer: 4.10.3-test.19
InformationalVersion: 4.10.3-test.19+Branch.feature/test.Sha.dd2a29aff0c948e1bdf3dabbe13e1576e70d5f9f
BranchName: feature/test
Sha: dd2a29aff0c948e1bdf3dabbe13e1576e70d5f9f
NuGetVersionV2: 4.10.3-test0019
NuGetVersion: 4.10.3-test0019
CommitsSinceVersionSource: 19
CommitsSinceVersionSourcePadded: 0019
CommitDate: 2015-11-10
";

        var versionAndBranchFinder = new ExecuteCore(fileSystem);

        RepositoryScope(versionAndBranchFinder, (fixture, vv) =>
        {
            fileSystem.WriteAllText(vv.FileName, versionCacheFileContent);

            var gitPreparer = new GitPreparer(null, null, null, false, fixture.RepositoryPath);
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
        var info = RepositoryScope();
        info.ShouldContain("yml not found", () => info);
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
BuildMetaData: 
BuildMetaDataPadded: 
FullBuildMetaData: Branch.feature/test.Sha.dd2a29aff0c948e1bdf3dabbe13e1576e70d5f9f
MajorMinorPatch: 4.10.3
SemVer: 4.10.3-test.19
LegacySemVer: 4.10.3-test19
LegacySemVerPadded: 4.10.3-test0019
AssemblySemVer: 4.10.3.0
FullSemVer: 4.10.3-test.19
InformationalVersion: 4.10.3-test.19+Branch.feature/test.Sha.dd2a29aff0c948e1bdf3dabbe13e1576e70d5f9f
BranchName: feature/test
Sha: dd2a29aff0c948e1bdf3dabbe13e1576e70d5f9f
NuGetVersionV2: 4.10.3-test0019
NuGetVersion: 4.10.3-test0019
CommitsSinceVersionSource: 19
CommitsSinceVersionSourcePadded: 0019
CommitDate: 2015-11-10
";

        var versionAndBranchFinder = new ExecuteCore(fileSystem);

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
    public void WorkingDirectoryWithoutGit()
    {
        var versionAndBranchFinder = new ExecuteCore(fileSystem);

        RepositoryScope(versionAndBranchFinder, (fixture, vv) =>
        {
            var exception = Assert.Throws<DirectoryNotFoundException>(() => versionAndBranchFinder.ExecuteGitVersion(null, null, null, null, false, Environment.SystemDirectory, null));
            exception.Message.ShouldContain("Can't find the .git directory in");
        });
    }

    [Test]
    public void DynamicRepositoriesShouldNotErrorWithFailedToFindGitDirectory()
    {
        var versionAndBranchFinder = new ExecuteCore(fileSystem);

        RepositoryScope(versionAndBranchFinder, (fixture, vv) =>
        {
            versionAndBranchFinder.ExecuteGitVersion("https://github.com/GitTools/GitVersion.git", null, new Authentication(), "refs/head/master", false, fixture.RepositoryPath, null);
        });
    }

    string RepositoryScope(ExecuteCore executeCore = null, Action<EmptyRepositoryFixture, VersionVariables> fixtureAction = null)
    {
        // Make sure GitVersion doesn't trigger build server mode when we are running the tests
        Environment.SetEnvironmentVariable(AppVeyor.EnvironmentVariableName, null);
        Environment.SetEnvironmentVariable(TravisCI.EnvironmentVariableName, null);
        var infoBuilder = new StringBuilder();
        Action<string> infoLogger = s =>
        {
            infoBuilder.AppendLine(s);
        };
        executeCore = executeCore ?? new ExecuteCore(fileSystem);

        using (Logger.AddLoggersTemporarily(infoLogger, s => {}, s => { }))
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeACommit();
            var vv = executeCore.ExecuteGitVersion(null, null, null, null, false, fixture.RepositoryPath, null);

            vv.AssemblySemVer.ShouldBe("0.1.0.0");
            vv.FileName.ShouldNotBeNullOrEmpty();

            if (fixtureAction != null)
            {
                fixtureAction(fixture, vv);
            }
        }

        return infoBuilder.ToString();
    }
}