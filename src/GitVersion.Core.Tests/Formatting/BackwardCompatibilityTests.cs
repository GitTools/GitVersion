namespace GitVersion.Core.Tests.Formatting;

[TestFixture]
public class LegacyRegexPatternTests
{
    [Test]
    public void ExpandTokensRegex_ShouldParseLegacySemicolonSyntax()
    {
        const string input = "{CommitsSinceVersionSource:0000;;''}";

        var matches = RegexPatterns.Common.ExpandTokensRegex().Matches(input);

        matches.Count.ShouldBe(1);
        var match = matches[0];
        match.Groups["member"].Value.ShouldBe("CommitsSinceVersionSource");
        match.Groups["format"].Success.ShouldBeTrue();
    }

    [Test]
    public void ExpandTokensRegex_ShouldHandleMixedSyntax()
    {
        const string input = "{NewStyle:0000 ?? 'fallback'} {OldStyle:pos;neg;zero}";

        var matches = RegexPatterns.Common.ExpandTokensRegex().Matches(input);

        matches.Count.ShouldBe(2);

        var newMatch = matches[0];
        newMatch.Groups["member"].Value.ShouldBe("NewStyle");
        newMatch.Groups["fallback"].Value.ShouldBe("fallback");

        var oldMatch = matches[1];
        oldMatch.Groups["member"].Value.ShouldBe("OldStyle");
    }
}
