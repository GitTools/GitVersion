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

    [TestCase("Prop1 ?? Prop2", "Prop1", "Prop2")]
    [TestCase("Prop1??Prop2", "Prop1", "Prop2")]
    [TestCase("Prop1??Prop2??42", "Prop1", "Prop2", "42")]
    [TestCase("Prop1??Prop2??\"42\"", "Prop1", "Prop2", "42")]
    [TestCase("Prop1 ?? Prop2 ?? \"fallback\"", "Prop1", "Prop2", "fallback")]
    [TestCase("Prop1 ??Prop2?? \"fallback\"", "Prop1", "Prop2", "fallback")]
    [TestCase("Prop1 ?? Prop2 ?? 42", "Prop1", "Prop2", "42")]
    [TestCase("Prop1:format ?? Prop2 ?? \"fallback\"", "Prop1", "Prop2", "fallback")]
    [TestCase("env:Env1 ?? Prop2 ?? \"fallback\"", "Env1", "Prop2", "fallback")]
    [TestCase("env:Env1:format ?? \"literal\" ?? \"fallback\"", "Env1", "literal", "fallback")]
    [TestCase("env:Env1 ?? 42", "Env1", "42")]
    public void ParseTokens_ValidIdentifiers_ReturnsValid(string input, params string[] expected) => AssertTokens(input, expected);

    [TestCase("Prop ??? literal")]
    [TestCase("Prop literal")]
    [TestCase("Prop ?? literal ?? ? fallback")]
    [TestCase("Prop ? fallback")]
    [TestCase("Prop ?? fall?back")]
    [TestCase("Prop ?? fallback ??")]
    [TestCase("Prop ?? fallback ??   ")]
    public void ParseTokens_MalformedIdentifiers_Throws(string input) => AssertThrows(input);

    private static void AssertTokens(string input, string[] expected)
    {
        var tokenizer = new LabelTokenizer(input);
        var tokens = tokenizer.ParseTokens()
            .Select(x => x.Name)
            .ToArray();

        tokens.ShouldBeEquivalentTo(expected);
    }

    private static void AssertThrows(string input)
    {
        var tokenizer = new LabelTokenizer(input);

        Assert.Throws<FormatException>(() => tokenizer.ParseTokens());
    }
}
