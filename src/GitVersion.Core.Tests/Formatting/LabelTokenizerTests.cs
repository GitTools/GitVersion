using GitVersion.Formatting;

namespace GitVersion.Tests.Formatting;

[TestFixture]
public class LabelTokenizerTests
{
    [TestCase("Pattern", "Pattern")]
    [TestCase("Pattern  ", "Pattern")]
    [TestCase("  Pattern", "Pattern")]
    [TestCase("  Pattern  ", "Pattern")]
    [TestCase("Pat\\\"tern", "Pat\"tern")]
    [TestCase("\"Pattern\"", "Pattern")]
    [TestCase("\"Pat?tern\"", "Pat?tern")]
    [TestCase("\"Pat tern\"", "Pat tern")]
    [TestCase("\" Pattern\"", " Pattern")]
    [TestCase("\"Pattern \"", "Pattern ")]
    [TestCase("\"Pat\\\"tern\"", "Pat\"tern")]
    public void ParseTokens_ValidLiterals_ReturnsValid(string input, params string[] expected) => AssertTokens(input, expected);

    [TestCase("Pat?tern")]
    [TestCase("\"Pattern")]
    [TestCase("Pattern\"")]
    [TestCase("Pat\"tern")]
    public void ParseTokens_InvalidLiterals_Throws(string input) => AssertThrows(input);

    [TestCase("Prop ?? literal", "Prop", "literal")]
    [TestCase("Prop??literal", "Prop", "literal")]
    [TestCase("Prop ?? literal ?? fallback", "Prop", "literal", "fallback")]
    [TestCase("Prop ??literal?? fallback", "Prop", "literal", "fallback")]
    [TestCase("Prop ?? \"literal\" ?? fallback", "Prop", "literal", "fallback")]
    [TestCase("Prop:format ?? \"literal\" ?? fallback", "Prop", "literal", "fallback")]
    [TestCase("env:Prop ?? \"literal\" ?? fallback", "Prop", "literal", "fallback")]
    [TestCase("env:Prop:format ?? \"literal\" ?? fallback", "Prop", "literal", "fallback")]
    public void ParseTokens_ValidIdentifiers_ReturnsValid(string input, params string[] expected) => AssertTokens(input, expected);

    [TestCase("Prop ??? literal")]
    [TestCase("Prop literal")]
    [TestCase("Prop ?? literal ?? ? fallback")]
    [TestCase("Prop ? fallback")]
    [TestCase("Prop ?? fall?back")]
    public void ParseTokens_MalformedIdentifiers_Throws(string input) => AssertThrows(input);

    private void AssertTokens(string input, string[] expected)
    {
        var tokenizer = new LabelTokenizer(input);
        var tokens = tokenizer.ParseTokens()
            .Select(x => x.Name)
            .ToArray();

        tokens.ShouldBeEquivalentTo(expected);
    }

    private void AssertThrows(string input)
    {
        var tokenizer = new LabelTokenizer(input);

        Assert.Throws<FormatException>(() => tokenizer.ParseTokens());
    }
}
