using System.Globalization;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Formatting;

namespace GitVersion.Core.Tests.Formatting;

/// <summary>
/// Tests for backward compatibility with legacy .NET composite format syntax (;;)
/// These tests document the expected behavior before implementing the fix.
/// </summary>
[TestFixture]
public class LegacyFormattingSyntaxTests
{
    /// <summary>
    /// Test that the old ;;'' syntax for zero-value fallbacks still works
    /// This is the exact case from issue #4654
    /// </summary>
    [Test]
    public void FormatWith_LegacyZeroFallbackSyntax_ShouldWork()
    {
        // Arrange
        var semanticVersion = new SemanticVersion
        {
            Major = 6,
            Minor = 13,
            Patch = 54,
            PreReleaseTag = new SemanticVersionPreReleaseTag("gv6", 1, true),
            BuildMetaData = new SemanticVersionBuildMetaData()
            {
                Branch = "feature/gv6",
                VersionSourceSha = "versionSourceSha",
                Sha = "489a0c0ab425214def918e36399f3cc3c9a9c42d",
                ShortSha = "489a0c0",
                CommitsSinceVersionSource = 2,
                CommitDate = DateTimeOffset.Parse("2025-08-12", CultureInfo.InvariantCulture)
            }
        };

        // The exact template from the issue
        const string template = "{MajorMinorPatch}{PreReleaseLabelWithDash}{CommitsSinceVersionSource:0000;;''}";
        const string expected = "6.13.54-gv60002"; // Should format CommitsSinceVersionSource as 0002, not show literal text

        // Act
        var actual = template.FormatWith(semanticVersion, new TestEnvironment());

        // Assert
        actual.ShouldBe(expected);
    }

    /// <summary>
    /// Test that legacy positive/negative/zero section syntax works
    /// </summary>
    [Test]
    public void FormatWith_LegacyThreeSectionSyntax_ShouldWork()
    {
        // Arrange
        var testObject = new { Value = -5 };
        const string template = "{Value:positive;negative;zero}";
        const string expected = "negative";

        // Act
        var actual = template.FormatWith(testObject, new TestEnvironment());

        // Assert
        actual.ShouldBe(expected);
    }

    /// <summary>
    /// Test that legacy two-section syntax works (positive;negative)
    /// </summary>
    [Test]
    public void FormatWith_LegacyTwoSectionSyntax_ShouldWork()
    {
        // Arrange
        var testObject = new { Value = -10 };
        const string template = "{Value:positive;negative}";
        const string expected = "negative";

        // Act
        var actual = template.FormatWith(testObject, new TestEnvironment());

        // Assert
        actual.ShouldBe(expected);
    }

    /// <summary>
    /// Test that zero values use the third section in legacy syntax
    /// </summary>
    [Test]
    public void FormatWith_LegacyZeroValue_ShouldUseThirdSection()
    {
        // Arrange
        var testObject = new { Value = 0 };
        const string template = "{Value:pos;neg;ZERO}";
        const string expected = "ZERO";

        // Act
        var actual = template.FormatWith(testObject, new TestEnvironment());

        // Assert
        actual.ShouldBe(expected);
    }

    /// <summary>
    /// Test mixed usage: some properties with legacy syntax, others with new syntax
    /// </summary>
    [Test]
    public void FormatWith_MixedLegacyAndNewSyntax_ShouldWork()
    {
        // Arrange
        var testObject = new
        {
            OldStyle = 0,
            NewStyle = 42,
            RegularProp = "test"
        };
        const string template = "{OldStyle:pos;neg;''}{NewStyle:0000 ?? 'fallback'}{RegularProp}";
        const string expected = "0042test"; // Empty string for zero, 0042 for 42, test as-is

        // Act
        var actual = template.FormatWith(testObject, new TestEnvironment());

        // Assert
        actual.ShouldBe(expected);
    }

    /// <summary>
    /// Test that complex legacy format with actual .NET format specifiers works
    /// </summary>
    [Test]
    public void FormatWith_LegacyWithStandardFormatSpecifiers_ShouldWork()
    {
        // Arrange
        var testObject = new { Amount = 1234.56 };
        const string template = "{Amount:C2;(C2);'No Amount'}";
        const string expected = "¤1,234.56"; // Should format as currency

        // Act
        var actual = template.FormatWith(testObject, new TestEnvironment());

        // Assert
        actual.ShouldBe(expected);
    }

    /// <summary>
    /// Test that the original failing case from issue #4654 works exactly as expected
    /// </summary>
    [Test]
    public void FormatWith_Issue4654ExactCase_ShouldWork()
    {
        // Arrange - recreate the exact scenario from the issue
        var semanticVersion = new SemanticVersion
        {
            Major = 6,
            Minor = 13,
            Patch = 54,
            PreReleaseTag = new SemanticVersionPreReleaseTag("gv6", 1, true),
            BuildMetaData = new SemanticVersionBuildMetaData("Branch.feature-gv6")
            {
                CommitsSinceVersionSource = 2
            }
        };

        // This should work on main branch where PreReleaseLabelWithDash would be empty
        var mainBranchVersion = new SemanticVersion
        {
            Major = 6,
            Minor = 13,
            Patch = 54,
            PreReleaseTag = new SemanticVersionPreReleaseTag(string.Empty, 0, true),
            BuildMetaData = new SemanticVersionBuildMetaData()
            {
                CommitsSinceVersionSource = 0
            }
        };

        const string template = "{MajorMinorPatch}{PreReleaseLabelWithDash}{CommitsSinceVersionSource:0000;;''}";

        // Act & Assert for feature branch
        var featureResult = template.FormatWith(semanticVersion, new TestEnvironment());
        featureResult.ShouldBe("6.13.54-gv60002");

        // Act & Assert for main branch (zero commits should show empty string)
        var mainResult = template.FormatWith(mainBranchVersion, new TestEnvironment());
        mainResult.ShouldBe("6.13.54"); // Empty PreReleaseLabelWithDash and empty string for zero commits
    }
}

/// <summary>
/// Tests specifically for the regex pattern changes to ensure backward compatibility
/// </summary>
[TestFixture]
public class LegacyRegexPatternTests
{
    /// <summary>
    /// Test that the ExpandTokensRegex can parse legacy semicolon syntax
    /// </summary>
    [Test]
    public void ExpandTokensRegex_ShouldParseLegacySemicolonSyntax()
    {
        // Arrange
        const string input = "{CommitsSinceVersionSource:0000;;''}";

        // Act
        var matches = RegexPatterns.Common.ExpandTokensRegex().Matches(input);

        // Assert
        matches.Count.ShouldBe(1);
        var match = matches[0];
        match.Groups["member"].Value.ShouldBe("CommitsSinceVersionSource");

        // The format group should capture the entire format including semicolons
        // This test documents what should happen - the format might need to be "0000;;''"
        // or the regex might need to separate format and fallback parts
        match.Groups["format"].Success.ShouldBeTrue();
        // The exact capture will depend on implementation - this test will guide the regex design
    }

    /// <summary>
    /// Test that both new and old syntax can coexist in the same template
    /// </summary>
    [Test]
    public void ExpandTokensRegex_ShouldHandleMixedSyntax()
    {
        // Arrange
        const string input = "{NewStyle:0000 ?? 'fallback'} {OldStyle:pos;neg;zero}";

        // Act
        var matches = RegexPatterns.Common.ExpandTokensRegex().Matches(input);

        // Assert
        matches.Count.ShouldBe(2);

        // First match: new syntax
        var newMatch = matches[0];
        newMatch.Groups["member"].Value.ShouldBe("NewStyle");
        newMatch.Groups["fallback"].Value.ShouldBe("fallback");

        // Second match: old syntax  
        var oldMatch = matches[1];
        oldMatch.Groups["member"].Value.ShouldBe("OldStyle");
        // Format handling for legacy syntax TBD based on implementation approach
    }
}
