#region License

// --------------------------------------------------
// Copyright © OKB. All Rights Reserved.
// 
// This software is proprietary information of OKB.
// USE IS SUBJECT TO LICENSE TERMS.
// --------------------------------------------------

#endregion

using NUnit.Framework;

using Shouldly;

[TestFixture]
public class VersionAndBranchFinderTests
{
    [Test]
    public void ExistingCacheFile()
    {
        var fileSystem = new TestFileSystem();
        fileSystem.WriteAllText("existing\\gitversion_cache\\C7B23F8A47ECE0E14CBE2E22C04269CC5A88E275.yml", @"
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
");

        var vv = VersionAndBranchFinder.GetVersion("existing", null, false, fileSystem);

        vv.AssemblySemVer.ShouldBe("4.10.3.0");
    }


    [Test]
    public void MissingCacheFile()
    {
        var fileSystem = new TestFileSystem();

        var vv = VersionAndBranchFinder.GetVersion("missing", null, false, fileSystem);

        vv.AssemblySemVer.ShouldBe("0.1.0.0");
    }
}