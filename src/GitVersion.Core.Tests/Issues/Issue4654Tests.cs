using System.Globalization;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Formatting;

namespace GitVersion.Core.Tests.Issues;

[TestFixture]
public class Issue4654Tests
{
    [Test]
    [Category("Issue4654")]
    public void Issue4654_ExactReproduction_ShouldFormatCorrectly()
    {
        var semanticVersion = new SemanticVersion
        {
            Major = 6,
            Minor = 13,
            Patch = 54,
            PreReleaseTag = new SemanticVersionPreReleaseTag("gv6", 1, true),
            BuildMetaData = new SemanticVersionBuildMetaData()
            {
                Branch = "feature/gv6",
                VersionSourceSha = "21d7e26e6ff58374abd3daf2177be4b7a9c49040",
                Sha = "489a0c0ab425214def918e36399f3cc3c9a9c42d",
                ShortSha = "489a0c0",
                CommitsSinceVersionSource = 2,
                CommitDate = DateTimeOffset.Parse("2025-08-12", CultureInfo.InvariantCulture),
                UncommittedChanges = 0
            }
        };

        var extendedVersion = new
        {
            semanticVersion.Major,
            semanticVersion.Minor,
            semanticVersion.Patch,
            semanticVersion.BuildMetaData.CommitsSinceVersionSource,
            MajorMinorPatch = $"{semanticVersion.Major}.{semanticVersion.Minor}.{semanticVersion.Patch}",
            PreReleaseLabel = semanticVersion.PreReleaseTag.Name,
            PreReleaseLabelWithDash = string.IsNullOrEmpty(semanticVersion.PreReleaseTag.Name)
                ? ""
                : $"-{semanticVersion.PreReleaseTag.Name}",
            AssemblySemFileVer = "6.13.54.0",
            AssemblySemVer = "6.13.54.0",
            BranchName = "feature/gv6",
            EscapedBranchName = "feature-gv6",
            FullSemVer = "6.13.54-gv6.1+2",
            SemVer = "6.13.54-gv6.1"
        };

        const string template = "{MajorMinorPatch}{PreReleaseLabelWithDash}{CommitsSinceVersionSource:0000;;''}";
        const string expected = "6.13.54-gv60002";

        var actual = template.FormatWith(extendedVersion, new TestEnvironment());

        actual.ShouldBe(expected, "The legacy ;;'' syntax should format CommitsSinceVersionSource as 0002, not as literal text");
    }

    [Test]
    [Category("Issue4654")]
    public void Issue4654_WithoutLegacySyntax_ShouldStillWork()
    {
        var testData = new
        {
            MajorMinorPatch = "6.13.54",
            PreReleaseLabelWithDash = "-gv6",
            CommitsSinceVersionSource = 2
        };

        const string template = "{MajorMinorPatch}{PreReleaseLabelWithDash}{CommitsSinceVersionSource:0000}";
        const string expected = "6.13.54-gv60002";

        var actual = template.FormatWith(testData, new TestEnvironment());

        actual.ShouldBe(expected, "New format syntax should work correctly");
    }

    [Test]
    [Category("Issue4654")]
    [Category("CurrentBehavior")]
    public void Issue4654_CurrentBrokenBehavior_DocumentsActualOutput()
    {
        var testData = new
        {
            MajorMinorPatch = "6.13.54",
            PreReleaseLabelWithDash = "-gv6",
            CommitsSinceVersionSource = 2
        };

        const string template = "{MajorMinorPatch}{PreReleaseLabelWithDash}{CommitsSinceVersionSource:0000;;''}";
        const string shouldBe = "6.13.54-gv60002";

        var actual = template.FormatWith(testData, new TestEnvironment());

        if (actual == shouldBe)
        {
            Assert.Pass("The issue has been fixed!");
        }
        else
        {
            Console.WriteLine($"Current broken output: {actual}");
            Console.WriteLine($"Expected output: {shouldBe}");
            actual.ShouldContain("CommitsSinceVersionSource");
        }
    }

    [Test]
    [Category("Issue4654")]
    public void Issue4654_ZeroValueWithLegacySyntax_ShouldUseEmptyFallback()
    {
        var mainBranchData = new
        {
            MajorMinorPatch = "6.13.54",
            PreReleaseLabelWithDash = "",
            CommitsSinceVersionSource = 0
        };

        const string template = "{MajorMinorPatch}{PreReleaseLabelWithDash}{CommitsSinceVersionSource:0000;;''}";
        const string expected = "6.13.54";

        var actual = template.FormatWith(mainBranchData, new TestEnvironment());

        actual.ShouldBe(expected, "Zero values should use the third section (empty string) in legacy ;;'' syntax");
    }
}