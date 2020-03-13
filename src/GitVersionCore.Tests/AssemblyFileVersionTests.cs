using GitVersion;
using GitVersion.Extensions;
using GitVersionCore.Tests.Helpers;
using NUnit.Framework;
using Shouldly;

namespace GitVersionCore.Tests
{
    [TestFixture]
    public class AssemblyFileVersionTests : TestBase
    {
        [TestCase(AssemblyFileVersioningScheme.None, 1, 2, 3, 4, null)]
        [TestCase(AssemblyFileVersioningScheme.Major, 1, 2, 3, 4, "1.0.0.0")]
        [TestCase(AssemblyFileVersioningScheme.MajorMinor, 1, 2, 3, 4, "1.2.0.0")]
        [TestCase(AssemblyFileVersioningScheme.MajorMinorPatch, 1, 2, 3, 4, "1.2.3.0")]
        [TestCase(AssemblyFileVersioningScheme.MajorMinorPatchTag, 1, 2, 3, 4, "1.2.3.4")]
        public void ValidateAssemblyFileVersionBuilder(AssemblyFileVersioningScheme assemblyFileVersioningScheme, int major, int minor, int patch,
        int tag, string versionString)
        {
            var semVer = new SemanticVersion(major, minor, patch)
            {
                PreReleaseTag = new SemanticVersionPreReleaseTag("Test", tag)
            };

            var assemblyFileVersion = semVer.GetAssemblyFileVersion(assemblyFileVersioningScheme);

            assemblyFileVersion.ShouldBe(versionString);
        }
    }
}
