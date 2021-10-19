using GitVersion;
using NUnit.Framework;

namespace GitVersionExe.Tests;

[TestFixture]
public class QuotedStringHelpersTests
{
    [TestCaseSource(nameof(SplitUnquotedTestData))]
    public string[] SplitUnquotedTests(string input, char splitChar) => QuotedStringHelpers.SplitUnquoted(input, splitChar);

    private static IEnumerable<TestCaseData> SplitUnquotedTestData()
    {
        yield return new TestCaseData(null, ' ')
        {
            ExpectedResult = Array.Empty<string>()
        };
        yield return new TestCaseData("one two three", ' ')
        {
            ExpectedResult = new[] { "one", "two", "three" }
        };
        yield return new TestCaseData("one \"two three\"", ' ')
        {
            ExpectedResult = new[] { "one", "\"two three\"" }
        };
        yield return new TestCaseData("one \"two three", ' ')
        {
            ExpectedResult = new[] { "one", "\"two three" }
        };
        yield return new TestCaseData("/overrideconfig tag-prefix=Sample", ' ')
        {
            ExpectedResult = new[]
            {
                "/overrideconfig",
                "tag-prefix=Sample"
            }
        };
        yield return new TestCaseData("/overrideconfig tag-prefix=Sample 2", ' ')
        {
            ExpectedResult = new[]
            {
                "/overrideconfig",
                "tag-prefix=Sample",
                "2"
            }
        };
        yield return new TestCaseData("/overrideconfig tag-prefix=\"Sample 2\"", ' ')
        {
            ExpectedResult = new[]
            {
                "/overrideconfig",
                "tag-prefix=\"Sample 2\""
            }
        };
        yield return new TestCaseData("/overrideconfig tag-prefix=\"Sample \\\"quoted\\\"\"", ' ')
        {
            ExpectedResult = new[]
            {
                "/overrideconfig",
                "tag-prefix=\"Sample \\\"quoted\\\"\""
            }
        };
        yield return new TestCaseData("/overrideconfig tag-prefix=sample;assembly-versioning-format=\"{Major}.{Minor}.{Patch}.{env:CI_JOB_ID ?? 0}\"", ' ')
        {
            ExpectedResult = new[]
            {
                "/overrideconfig",
                "tag-prefix=sample;assembly-versioning-format=\"{Major}.{Minor}.{Patch}.{env:CI_JOB_ID ?? 0}\""
            }
        };
        yield return new TestCaseData("tag-prefix=sample;assembly-versioning-format=\"{Major}.{Minor}.{Patch}.{env:CI_JOB_ID ?? 0}\"", ';')
        {
            ExpectedResult = new[]
            {
                "tag-prefix=sample",
                "assembly-versioning-format=\"{Major}.{Minor}.{Patch}.{env:CI_JOB_ID ?? 0}\""
            }
        };
        yield return new TestCaseData("assembly-versioning-format=\"{Major}.{Minor}.{Patch}.{env:CI_JOB_ID ?? 0}\"", '=')
        {
            ExpectedResult = new[]
            {
                "assembly-versioning-format",
                "\"{Major}.{Minor}.{Patch}.{env:CI_JOB_ID ?? 0}\""
            }
        };
    }

    [TestCaseSource(nameof(RemoveEmptyEntriesTestData))]
    public string[] SplitUnquotedRemovesEmptyEntries(string input, char splitChar) => QuotedStringHelpers.SplitUnquoted(input, splitChar);

    private static IEnumerable<TestCaseData> RemoveEmptyEntriesTestData()
    {
        yield return new TestCaseData(" /switch1 value1  /switch2 ", ' ')
        {
            ExpectedResult = new[]
            {
                "/switch1",
                "value1",
                "/switch2"
            }
        };
    }

    [TestCaseSource(nameof(UnquoteTextTestData))]
    public string UnquoteTextTests(string input) => QuotedStringHelpers.UnquoteText(input);

    private static IEnumerable<TestCaseData> UnquoteTextTestData()
    {
        yield return new TestCaseData("\"sample\"")
        {
            ExpectedResult = "sample"
        };
        yield return new TestCaseData("\"escaped \\\"quote\"")
        {
            ExpectedResult = "escaped \"quote"
        };
    }
}
