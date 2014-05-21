namespace Tests
{
    using System;
    using GitVersion;
    using NUnit.Framework;
    using Shouldly;

    [TestFixture]
    public class VariableProviderTests
    {
        [Test]
        public void DevelopBranchFormatsSemVerForCiFeed()
        {
            var semVer = new SemanticVersion
            {
                Major = 1,
                Minor = 2,
                Patch = 3,
                PreReleaseTag = "unstable.4",
                BuildMetaData = "5.Branch.develop"
            };

            semVer.BuildMetaData.ReleaseDate = new ReleaseDate
            {
                OriginalCommitSha = "originalCommitSha",
                OriginalDate = DateTimeOffset.Parse("2014-03-01 00:00:01Z"),
                CommitSha = "commitSha",
                Date = DateTimeOffset.Parse("2014-03-06 23:59:59Z")
            };

            var vars = VariableProvider.GetVariablesFor(semVer);

            vars[VariableProvider.SemVer].ShouldBe("1.2.3.5-unstable");
        }

        [TestCase(2, 3, 4, "5.Branch.master", AssemblyVersioningScheme.None, true, "1.0.0.0", "2.3.4.5")]
        [TestCase(2, 3, 4, "5.Branch.master", AssemblyVersioningScheme.None, false, "1.0.0.0", "2.3.4.0")]
        [TestCase(2, 3, 4, "5.Branch.master", AssemblyVersioningScheme.Major, true, "2.0.0.0", "2.3.4.5")]
        [TestCase(2, 3, 4, "5.Branch.master", AssemblyVersioningScheme.Major, false, "2.0.0.0", "2.3.4.0")]
        [TestCase(2, 3, 4, "5.Branch.master", AssemblyVersioningScheme.MajorMinor, true, "2.3.0.0", "2.3.4.5")]
        [TestCase(2, 3, 4, "5.Branch.master", AssemblyVersioningScheme.MajorMinor, false, "2.3.0.0", "2.3.4.0")]
        [TestCase(2, 3, 4, "5.Branch.master", AssemblyVersioningScheme.MajorMinorPatch, true, "2.3.4.0", "2.3.4.5")]
        [TestCase(2, 3, 4, "5.Branch.master", AssemblyVersioningScheme.MajorMinorPatch, false, "2.3.4.0", "2.3.4.0")]
        [TestCase(2, 3, 4, "5.Branch.develop", AssemblyVersioningScheme.None, true, "1.0.0.0", "2.3.4.0")]
        [TestCase(2, 3, 4, "5.Branch.develop", AssemblyVersioningScheme.None, false, "1.0.0.0", "2.3.4.0")]
        [TestCase(2, 3, 4, "5.Branch.develop", AssemblyVersioningScheme.Major, true, "2.0.0.0", "2.3.4.0")]
        [TestCase(2, 3, 4, "5.Branch.develop", AssemblyVersioningScheme.Major, false, "2.0.0.0", "2.3.4.0")]
        [TestCase(2, 3, 4, "5.Branch.develop", AssemblyVersioningScheme.MajorMinor, true, "2.3.0.0", "2.3.4.0")]
        [TestCase(2, 3, 4, "5.Branch.develop", AssemblyVersioningScheme.MajorMinor, false, "2.3.0.0", "2.3.4.0")]
        [TestCase(2, 3, 4, "5.Branch.develop", AssemblyVersioningScheme.MajorMinorPatch, true, "2.3.4.0", "2.3.4.0")]
        [TestCase(2, 3, 4, "5.Branch.develop", AssemblyVersioningScheme.MajorMinorPatch, false, "2.3.4.0", "2.3.4.0")]
        public void AssemblyVersion(
            int major, int minor, int patch, string buildMetadata,
            AssemblyVersioningScheme versioningScheme, bool addNumberOfCommitsSinceTagOnMasterBranchToFileVersion,
            string version, string fileVersion)
        {
            var semVer = new SemanticVersion
            {
                Major = major,
                Minor = minor,
                Patch = patch,
                BuildMetaData = buildMetadata
            };

            semVer.BuildMetaData.ReleaseDate = new ReleaseDate
                            {
                                OriginalCommitSha = "originalCommitSha",
                                OriginalDate = DateTimeOffset.Parse("2014-03-01 00:00:01Z"),
                                CommitSha = "commitSha",
                                Date = DateTimeOffset.Parse("2014-03-06 23:59:59Z")
                            };

            var vars = VariableProvider.GetVariablesFor(semVer, versioningScheme, addNumberOfCommitsSinceTagOnMasterBranchToFileVersion);

            vars[VariableProvider.AssemblyVersion].ShouldBe(version);
            vars[VariableProvider.AssemblyFileVersion].ShouldBe(fileVersion);
        }
    }
}
