using GitVersion.Core.Tests.Helpers;
using GitVersion.Formatting;

namespace GitVersion.Core.Tests.Formatting;

[TestFixture]
public class LegacyFormatterProblemTests
{
    private TestEnvironment environment;

    [SetUp]
    public void Setup() => environment = new TestEnvironment();

    // ==========================================
    // PROBLEM 1: Non-existent properties
    // ==========================================

    [Test]
    [Category("Problem2")]
    public void Problem2_NullValue_ShouldUseZeroSection()
    {
        var testObject = new { Value = (int?)null };
        const string template = "{Value:positive;negative;zero}";
        const string expected = "zero";

        var actual = template.FormatWith(testObject, environment);
        actual.ShouldBe(expected, "Null values should use zero section without transformation");
    }

    [Test]
    [Category("Problem1")]
    public void Problem1_MissingProperty_ShouldFailGracefully()
    {
        // Test tries to use {MajorMinorPatch} on SemanticVersion but that property doesn't exist
        var semanticVersion = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3
        };

        const string template = "{MajorMinorPatch}"; // This property doesn't exist on SemanticVersion

        // Currently this will throw or behave unexpectedly
        // Should either throw meaningful error or handle gracefully
        Assert.Throws<ArgumentException>(() => template.FormatWith(semanticVersion, environment));
    }

    // ==========================================
    // PROBLEM 2: Double negative handling  
    // ==========================================

    [Test]
    [Category("Problem2")]
    public void Problem2_NegativeValue_ShouldNotDoubleNegative()
    {
        var testObject = new { Value = -5 };
        const string template = "{Value:positive;negative;zero}";

        // EXPECTED: "negative" (just the literal text from section 2)
        // ACTUAL: "-negative" (the negative sign from -5 plus the literal "negative")
        const string expected = "negative";

        var actual = template.FormatWith(testObject, environment);

        // This will currently fail - we get "-negative" instead of "negative"
        actual.ShouldBe(expected, "Negative values should use section text without the negative sign");
    }

    [Test]
    [Category("Problem2")]
    public void Problem2_PositiveValue_ShouldFormatCorrectly()
    {
        var testObject = new { Value = 5 };
        const string template = "{Value:positive;negative;zero}";
        const string expected = "positive";

        var actual = template.FormatWith(testObject, environment);
        actual.ShouldBe(expected);
    }

    [Test]
    [Category("Problem2")]
    public void Problem2_ZeroValue_ShouldUseZeroSection()
    {
        var testObject = new { Value = 0 };
        const string template = "{Value:positive;negative;zero}";
        const string expected = "zero";

        var actual = template.FormatWith(testObject, environment);
        actual.ShouldBe(expected);
    }

    // ==========================================
    // PROBLEM 3: Insufficient formatting logic
    // ==========================================

    [Test]
    [Category("Problem3")]
    public void Problem3_NumericFormatting_AllSectionsShouldFormat()
    {
        // Test that numeric formatting works in ALL sections, not just first
        var testObject = new { Value = -42 };
        const string template = "{Value:0000;0000;0000}"; // All sections should pad with zeros

        // EXPECTED: "0042" (absolute value 42, formatted with 0000 in negative section)
        // ACTUAL: "0000" (literal text instead of formatted value)
        const string expected = "0042";

        var actual = template.FormatWith(testObject, environment);
        actual.ShouldBe(expected, "Negative section should format the absolute value, not return literal");
    }

    [Test]
    [Category("Problem3")]
    public void Problem3_FirstSectionWorks_OthersDont()
    {
        // Demonstrate that first section works but others don't
        var positiveObject = new { Value = 42 };
        var negativeObject = new { Value = -42 };

        const string template = "{Value:0000;WRONG;WRONG}";

        // First section (positive) should work correctly
        var positiveResult = template.FormatWith(positiveObject, environment);
        positiveResult.ShouldBe("0042", "First section should format correctly");

        // Second section (negative) should return literal when invalid format provided
        var negativeResult = template.FormatWith(negativeObject, environment);
        // Invalid format "WRONG" should return literal to give user feedback about their error
        negativeResult.ShouldBe("WRONG", "Invalid format should return literal to indicate user error");
    }

    // ==========================================
    // VERIFY #4654 FIX STILL WORKS
    // ==========================================

    [Test]
    [Category("Issue4654")]
    public void Issue4654_LegacySyntax_ShouldStillWork()
    {
        // Verify the original #4654 fix still works
        var testObject = new { CommitsSinceVersionSource = 2 };
        const string template = "{CommitsSinceVersionSource:0000;;''}";
        const string expected = "0002";

        var actual = template.FormatWith(testObject, environment);
        actual.ShouldBe(expected, "Issue #4654 fix must be preserved");
    }

    [Test]
    [Category("Issue4654")]
    public void Issue4654_ZeroValue_ShouldUseEmptyString()
    {
        // Zero values should use the third section (empty string)
        var testObject = new { CommitsSinceVersionSource = 0 };
        const string template = "{CommitsSinceVersionSource:0000;;''}";
        const string expected = "";

        var actual = template.FormatWith(testObject, environment);
        actual.ShouldBe(expected, "Zero values should use third section (empty)");
    }
}
