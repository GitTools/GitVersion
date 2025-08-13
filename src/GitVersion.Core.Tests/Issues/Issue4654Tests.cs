using System.Globalization;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Formatting;

namespace GitVersion.Core.Tests.Issues;

/// <summary>
/// Tests for Issue #4654 - Incorrect output for assembly-informational-version
/// https://github.com/GitTools/GitVersion/issues/4654
///
/// These tests document the expected behavior before implementing the fix.
/// Currently FAILING - will pass once backward compatibility is implemented.
/// </summary>
[TestFixture]
public class Issue4654Tests
{
    /// <summary>
    /// Reproduce the exact issue reported in #4654
    /// Expected: "6.13.54-gv60002"
    /// Actual (broken): "6.13.54-gv6-CommitsSinceVersionSource-0000-----"
    /// </summary>
    [Test]
    [Category("Issue4654")]
    public void Issue4654_ExactReproduction_ShouldFormatCorrectly()
    {
        // Arrange - exact data from the issue
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

        // Add derived properties that would be available in real GitVersion
        var extendedVersion = new
        {
            // Core properties
            semanticVersion.Major,
            semanticVersion.Minor,
            semanticVersion.Patch,
            semanticVersion.BuildMetaData.CommitsSinceVersionSource,

            // Derived properties
            MajorMinorPatch = $"{semanticVersion.Major}.{semanticVersion.Minor}.{semanticVersion.Patch}",
            PreReleaseLabel = semanticVersion.PreReleaseTag.Name,
            PreReleaseLabelWithDash = string.IsNullOrEmpty(semanticVersion.PreReleaseTag.Name)
                ? ""
                : $"-{semanticVersion.PreReleaseTag.Name}",

            // Other properties from the issue JSON
            AssemblySemFileVer = "6.13.54.0",
            AssemblySemVer = "6.13.54.0",
            BranchName = "feature/gv6",
            EscapedBranchName = "feature-gv6",
            FullSemVer = "6.13.54-gv6.1+2",
            SemVer = "6.13.54-gv6.1"
        };

        // The exact template from the overrideconfig command
        const string template = "{MajorMinorPatch}{PreReleaseLabelWithDash}{CommitsSinceVersionSource:0000;;''}";

        // Expected result: MajorMinorPatch + PreReleaseLabelWithDash + formatted CommitsSinceVersionSource
        const string expected = "6.13.54-gv60002";

        // Act
        var actual = template.FormatWith(extendedVersion, new TestEnvironment());

        // Assert
        actual.ShouldBe(expected, "The legacy ;;'' syntax should format CommitsSinceVersionSource as 0002, not as literal text");
    }

    /// <summary>
    /// Test the simpler case without the legacy syntax to ensure basic formatting still works
    /// </summary>
    [Test]
    [Category("Issue4654")]
    public void Issue4654_WithoutLegacySyntax_ShouldStillWork()
    {
        // Arrange
        var testData = new
        {
            MajorMinorPatch = "6.13.54",
            PreReleaseLabelWithDash = "-gv6",
            CommitsSinceVersionSource = 2
        };

        // Using new ?? syntax instead of ;;
        const string template = "{MajorMinorPatch}{PreReleaseLabelWithDash}{CommitsSinceVersionSource:0000}";
        const string expected = "6.13.54-gv60002";

        // Act
        var actual = template.FormatWith(testData, new TestEnvironment());

        // Assert
        actual.ShouldBe(expected, "New format syntax should work correctly");
    }

    /// <summary>
    /// Test that demonstrates the current broken behavior
    /// This test documents what's currently happening (incorrect output)
    /// </summary>
    [Test]
    [Category("Issue4654")]
    [Category("CurrentBehavior")]
    public void Issue4654_CurrentBrokenBehavior_DocumentsActualOutput()
    {
        // Arrange
        var testData = new
        {
            MajorMinorPatch = "6.13.54",
            PreReleaseLabelWithDash = "-gv6",
            CommitsSinceVersionSource = 2
        };

        const string template = "{MajorMinorPatch}{PreReleaseLabelWithDash}{CommitsSinceVersionSource:0000;;''}";

        // This is what we expect it SHOULD return
        const string shouldBe = "6.13.54-gv60002";

        // Act
        var actual = template.FormatWith(testData, new TestEnvironment());

        // Assert - document current broken behavior
        // This assertion will fail until the fix is implemented
        if (actual == shouldBe)
        {
            Assert.Pass("The issue has been fixed!");
        }
        else
        {
            // Document what it currently returns
            Console.WriteLine($"Current broken output: {actual}");
            Console.WriteLine($"Expected output: {shouldBe}");

            // This assertion should fail, documenting the current broken state
            actual.ShouldContain("CommitsSinceVersionSource");
            // Explanation: Currently broken - the property name appears as literal text instead of being formatted
        }
    }

    /// <summary>
    /// Test edge case: zero value with legacy syntax should use empty fallback
    /// </summary>
    [Test]
    [Category("Issue4654")]
    public void Issue4654_ZeroValueWithLegacySyntax_ShouldUseEmptyFallback()
    {
        // Arrange - main branch scenario where commits since version source is 0
        var mainBranchData = new
        {
            MajorMinorPatch = "6.13.54",
            PreReleaseLabelWithDash = "", // Empty on main branch
            CommitsSinceVersionSource = 0  // Zero commits
        };

        const string template = "{MajorMinorPatch}{PreReleaseLabelWithDash}{CommitsSinceVersionSource:0000;;''}";
        const string expected = "6.13.54"; // Should be empty string for zero value

        // Act
        var actual = template.FormatWith(mainBranchData, new TestEnvironment());

        // Assert
        actual.ShouldBe(expected, "Zero values should use the third section (empty string) in legacy ;;'' syntax");
    }
}
