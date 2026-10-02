namespace GitVersion.App.Tests;

[TestFixture]
public class QuotedStringHelpersTests
{
    [TestCaseSource(nameof(SplitUnquotedTestData))]
    public string[] SplitUnquotedTests(string? input, char splitChar) => QuotedStringHelpers.SplitUnquoted(input, splitChar);

    private static IEnumerable<TestCaseDataWithReturn<string?, char, string[]>> SplitUnquotedTestData()
    {
        yield return TestCaseData.Create<string?, char>(null, ' ').Returns(Array.Empty<string>());
        yield return TestCaseData.Create<string?, char>("one two three", ' ').Returns(new[] { "one", "two", "three" });
        yield return TestCaseData.Create<string?, char>("one \"two three\"", ' ').Returns(new[] { "one", "\"two three\"" });
        yield return TestCaseData.Create<string?, char>("one \"two three", ' ').Returns(new[] { "one", "\"two three" });
        yield return TestCaseData.Create<string?, char>("/overrideconfig tag-prefix=Sample", ' ').Returns(new[]
            {
                "/overrideconfig",
                "tag-prefix=Sample"
            });
        yield return TestCaseData.Create<string?, char>("/overrideconfig tag-prefix=Sample 2", ' ').Returns(new[]
            {
                "/overrideconfig",
                "tag-prefix=Sample",
                "2"
            });
        yield return TestCaseData.Create<string?, char>("/overrideconfig tag-prefix=\"Sample 2\"", ' ').Returns(new[]
            {
                "/overrideconfig",
                "tag-prefix=\"Sample 2\""
            });
        yield return TestCaseData.Create<string?, char>("/overrideconfig tag-prefix=\"Sample \\\"quoted\\\"\"", ' ').Returns(new[]
            {
                "/overrideconfig",
                "tag-prefix=\"Sample \\\"quoted\\\"\""
            });
        yield return TestCaseData.Create<string?, char>("/overrideconfig tag-prefix=sample;assembly-versioning-format=\"{Major}.{Minor}.{Patch}.{env:CI_JOB_ID ?? 0}\"", ' ').Returns(new[]
            {
                "/overrideconfig",
                "tag-prefix=sample;assembly-versioning-format=\"{Major}.{Minor}.{Patch}.{env:CI_JOB_ID ?? 0}\""
            });
        yield return TestCaseData.Create<string?, char>("tag-prefix=sample;assembly-versioning-format=\"{Major}.{Minor}.{Patch}.{env:CI_JOB_ID ?? 0}\"", ';').Returns(new[]
            {
                "tag-prefix=sample",
                "assembly-versioning-format=\"{Major}.{Minor}.{Patch}.{env:CI_JOB_ID ?? 0}\""
            });
        yield return TestCaseData.Create<string?, char>("assembly-versioning-format=\"{Major}.{Minor}.{Patch}.{env:CI_JOB_ID ?? 0}\"", '=').Returns(new[]
            {
                "assembly-versioning-format",
                "\"{Major}.{Minor}.{Patch}.{env:CI_JOB_ID ?? 0}\""
            });
    }

    [TestCaseSource(nameof(RemoveEmptyEntriesTestData))]
    public string[] SplitUnquotedRemovesEmptyEntries(string input, char splitChar) => QuotedStringHelpers.SplitUnquoted(input, splitChar);

    private static IEnumerable<TestCaseDataWithReturn<string, char, string[]>> RemoveEmptyEntriesTestData()
    {
        yield return TestCaseData.Create(" /switch1 value1  /switch2 ", ' ').Returns(new[]
            {
                "/switch1",
                "value1",
                "/switch2"
            });
    }

    [TestCaseSource(nameof(UnquoteTextTestData))]
    public string UnquoteTextTests(string input) => QuotedStringHelpers.UnquoteText(input);

    private static IEnumerable<TestCaseDataWithReturn<string, string>> UnquoteTextTestData()
    {
        yield return TestCaseData.Create("\"sample\"").Returns("sample");
        yield return TestCaseData.Create("\"escaped \\\"quote\"").Returns("escaped \"quote");
    }
}
