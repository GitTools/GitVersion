using System;
using System.Collections.Concurrent;
using System.Text;

using GitVersion;
using GitVersion.Helpers;

using NUnit.Framework;

using Shouldly;

[TestFixture]
public class VersionAndBranchFinderTests
{
    [Test]
    public void CacheFileExistsOnDisk()
    {
        const string versionCacheFileContent = @"
Major: 4
Minor: 10
Patch: 3
PreReleaseTag: test.19
PreReleaseTagWithDash: -test.19
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

        var info = RepositoryScope((fixture, vv, fs) =>
        {
            fs.WriteAllText(vv.FileName, versionCacheFileContent);

            // I would rather see that VersionAndBranchFinder was non-static and could be reinstantiated to
            // clear the in-memory cache, but that's not the case, so I have to perform this ugly hack. @asbjornu
            VersionAndBranchFinder.VersionCacheVersions = new ConcurrentDictionary<string, VersionVariables>();

            vv = VersionAndBranchFinder.GetVersion(fixture.RepositoryPath, null, false, fs);

            vv.AssemblySemVer.ShouldBe("4.10.3.0");
        });

        info.ShouldContain("Deserializing version variables from cache file", () => info);
    }


    [Test]
    public void CacheFileExistsInMemory()
    {
        var info = RepositoryScope((fixture, vv, fs) =>
        {
            vv = VersionAndBranchFinder.GetVersion(fixture.RepositoryPath, null, false, fs);
            vv.AssemblySemVer.ShouldBe("0.1.0.0");
        });

        info.ShouldContain("yml not found", () => info);
        info.ShouldNotContain("Deserializing version variables from cache file", () => info);
    }


    [Test]
    public void CacheFileIsMissing()
    {
        var info = RepositoryScope();
        info.ShouldContain("yml not found", () => info);
    }


    static string RepositoryScope(Action<EmptyRepositoryFixture, VersionVariables, IFileSystem> fixtureAction = null)
    {
        var infoBuilder = new StringBuilder();
        Action<string> infoLogger = s => { infoBuilder.AppendLine(s); };

        Logger.SetLoggers(infoLogger, null, null);

        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeACommit();
            var fileSystem = new FileSystem();
            var vv = VersionAndBranchFinder.GetVersion(fixture.RepositoryPath, null, false, fileSystem);

            vv.AssemblySemVer.ShouldBe("0.1.0.0");
            vv.FileName.ShouldNotBeNullOrEmpty();

            if (fixtureAction != null)
            {
                fixtureAction(fixture, vv, fileSystem);
            }
        }

        return infoBuilder.ToString();
    }
}