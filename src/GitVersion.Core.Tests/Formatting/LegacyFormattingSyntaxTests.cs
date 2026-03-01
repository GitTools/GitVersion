using System.Globalization;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Formatting;

namespace GitVersion.Core.Tests.Formatting;

[TestFixture]
public class LegacyFormattingSyntaxTests
{
    [Test]
    public void FormatWith_LegacyZeroFallbackSyntax_ShouldWork()
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
                VersionSourceSha = "versionSourceSha",
                Sha = "489a0c0ab425214def918e36399f3cc3c9a9c42d",
                ShortSha = "489a0c0",
                CommitsSinceVersionSource = 2,
                CommitDate = DateTimeOffset.Parse("2025-08-12", CultureInfo.InvariantCulture)
            }
        };

        const string template = "{MajorMinorPatch}{PreReleaseLabelWithDash}{CommitsSinceVersionSource:0000;;''}";
        const string expected = "6.13.54-gv60002";

        var actual = template.FormatWith(semanticVersion, new TestEnvironment());

        actual.ShouldBe(expected);
    }

    [Test]
    public void FormatWith_LegacyThreeSectionSyntax_ShouldWork()
    {
        var testObject = new { Value = -5 };
        const string template = "{Value:positive;negative;zero}";
        const string expected = "negative";

        var actual = template.FormatWith(testObject, new TestEnvironment());

        actual.ShouldBe(expected);
    }

    [Test]
    public void FormatWith_LegacyTwoSectionSyntax_ShouldWork()
    {
        var testObject = new { Value = -10 };
        const string template = "{Value:positive;negative}";
        const string expected = "negative";

        var actual = template.FormatWith(testObject, new TestEnvironment());

        actual.ShouldBe(expected);
    }

    [Test]
    public void FormatWith_LegacyZeroValue_ShouldUseThirdSection()
    {
        var testObject = new { Value = 0 };
        const string template = "{Value:pos;neg;ZERO}";
        const string expected = "ZERO";

        var actual = template.FormatWith(testObject, new TestEnvironment());

        actual.ShouldBe(expected);
    }

    [Test]
    public void FormatWith_MixedLegacyAndNewSyntax_ShouldWork()
    {
        var testObject = new
        {
            OldStyle = 0,
            NewStyle = 42,
            RegularProp = "test"
        };
        const string template = "{OldStyle:pos;neg;''}{NewStyle:0000 ?? 'fallback'}{RegularProp}";
        const string expected = "0042test";

        var actual = template.FormatWith(testObject, new TestEnvironment());

        actual.ShouldBe(expected);
    }

    [Test]
    public void FormatWith_LegacyWithStandardFormatSpecifiers_ShouldWork()
    {
        var testObject = new { Amount = 1234.56 };
        const string template = "{Amount:C2;(C2);'No Amount'}";
        const string expected = "¤1,234.56";

        var actual = template.FormatWith(testObject, new TestEnvironment());

        actual.ShouldBe(expected);
    }

    [Test]
    public void FormatWith_Issue4654ExactCase_ShouldWork()
    {
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

        var featureResult = template.FormatWith(semanticVersion, new TestEnvironment());
        featureResult.ShouldBe("6.13.54-gv60002");

        var mainResult = template.FormatWith(mainBranchVersion, new TestEnvironment());
        mainResult.ShouldBe("6.13.54");
    }
}
