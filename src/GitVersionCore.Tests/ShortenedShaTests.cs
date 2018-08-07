using System;
using System.Linq;
using GitVersion;
using GitVersion.VersionFilters;
using NUnit.Framework;

namespace GitVersionCore.Tests
{
    [TestFixture]
    public class ShortenedShaTests : TestBase
    {
        private SemanticVersionBuildMetaData _arrangedBuildMetaData;
        private SemanticVersion _arrangedSemVer;
        private string _arrangedSha;

        [OneTimeSetUp]
        public void SetUp()

        {
            _arrangedSha = "c7c03329ef0ae21496552219a38caa6d16dfb73f";
            _arrangedBuildMetaData = new SemanticVersionBuildMetaData(0, "master", _arrangedSha, DateTimeOffset.Now);
            _arrangedSemVer = new SemanticVersion { BuildMetaData = _arrangedBuildMetaData };
        }

        [Test]
        [TestCase(null, ExpectedResult = "c7c03329ef0ae21496552219a38caa6d16dfb73f")]
        [TestCase(-1, ExpectedResult = "c7c03329ef0ae21496552219a38caa6d16dfb73f")]
        [TestCase(int.MinValue, ExpectedResult = "c7c03329ef0ae21496552219a38caa6d16dfb73f")]
        [TestCase(int.MaxValue, ExpectedResult = "c7c03329ef0ae21496552219a38caa6d16dfb73f")]
        [TestCase(0, ExpectedResult = "c7c03329ef0ae21496552219a38caa6d16dfb73f")]
        [TestCase(1, ExpectedResult = "c7c03329ef0ae21496552219a38caa6d16dfb73f")]
        [TestCase(2, ExpectedResult = "c7c03329ef0ae21496552219a38caa6d16dfb73f")]
        [TestCase(3, ExpectedResult = "c7c03329ef0ae21496552219a38caa6d16dfb73f")]
        [TestCase(4, ExpectedResult = "c7c0")]
        [TestCase(5, ExpectedResult = "c7c03")]
        [TestCase(6, ExpectedResult = "c7c033")]
        [TestCase(7, ExpectedResult = "c7c0332")]
        [TestCase(8, ExpectedResult = "c7c03329")]
        public string ShortenedShaTest(int? shaLength)
        {
            var arrangedEffectiveConfiguration = new EffectiveConfiguration(assemblyVersioningScheme: AssemblyVersioningScheme.MajorMinorPatch,
                assemblyFileVersioningScheme: AssemblyFileVersioningScheme.MajorMinorPatch,
                assemblyInformationalFormat: string.Empty,
                assemblyVersioningFormat: string.Empty,
                assemblyFileVersioningFormat: string.Empty,
                versioningMode: VersioningMode.ContinuousDelivery, gitTagPrefix: string.Empty, tag: string.Empty, nextVersion: string.Empty, increment: IncrementStrategy.Inherit, branchPrefixToTrim: string.Empty, preventIncrementForMergedBranchVersion: true, tagNumberPattern: string.Empty, continuousDeploymentFallbackTag: string.Empty, trackMergeTarget: false, majorVersionBumpMessage: string.Empty, minorVersionBumpMessage: string.Empty, patchVersionBumpMessage: string.Empty, noBumpMessage: string.Empty, commitMessageIncrementing: CommitMessageIncrementMode.Enabled, legacySemVerPaddding: 4, buildMetaDataPadding: 4, commitsSinceVersionSourcePadding: 4, versionFilters: Enumerable.Empty<IVersionFilter>(), tracksReleaseBranches: false, isCurrentBranchRelease: true, commitDateFormat: "yy-MM-dd", commitShaShortlength: shaLength??0);
            var versionFormatValues = new SemanticVersionFormatValues(_arrangedSemVer, arrangedEffectiveConfiguration);

            return versionFormatValues.ShaShort;
        }
    }
}