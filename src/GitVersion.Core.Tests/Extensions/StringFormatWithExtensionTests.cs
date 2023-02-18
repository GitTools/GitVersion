using GitVersion.Core.Tests.Helpers;
using GitVersion.Helpers;

namespace GitVersion.Core.Tests;

[TestFixture]
public class StringFormatWithExtensionTests
{
    private IEnvironment environment;

    [SetUp]
    public void Setup() => this.environment = new TestEnvironment();

    [Test]
    public void FormatWithNoTokens()
    {
        var propertyObject = new { };
        const string expected = "Some String without tokens";
        var actual = expected.FormatWith(propertyObject, this.environment);
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void FormatWithSingleSimpleToken()
    {
        var propertyObject = new { SomeProperty = "SomeValue" };
        const string target = "{SomeProperty}";
        const string expected = "SomeValue";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void FormatWithMultipleTokensAndVerbatimText()
    {
        var propertyObject = new { SomeProperty = "SomeValue", AnotherProperty = "Other Value" };
        const string target = "{SomeProperty} some text {AnotherProperty}";
        const string expected = "SomeValue some text Other Value";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void FormatWithEnvVarToken()
    {
        this.environment.SetEnvironmentVariable("GIT_VERSION_TEST_VAR", "Env Var Value");
        var propertyObject = new { };
        const string target = "{env:GIT_VERSION_TEST_VAR}";
        const string expected = "Env Var Value";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void FormatWithEnvVarTokenWithFallback()
    {
        this.environment.SetEnvironmentVariable("GIT_VERSION_TEST_VAR", "Env Var Value");
        var propertyObject = new { };
        const string target = "{env:GIT_VERSION_TEST_VAR ?? fallback}";
        const string expected = "Env Var Value";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void FormatWithUnsetEnvVarToken_WithFallback()
    {
        this.environment.SetEnvironmentVariable("GIT_VERSION_UNSET_TEST_VAR", null);
        var propertyObject = new { };
        const string target = "{env:GIT_VERSION_UNSET_TEST_VAR ?? fallback}";
        const string expected = "fallback";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void FormatWithUnsetEnvVarToken_WithoutFallback()
    {
        this.environment.SetEnvironmentVariable("GIT_VERSION_UNSET_TEST_VAR", null);
        var propertyObject = new { };
        const string target = "{env:GIT_VERSION_UNSET_TEST_VAR}";
        Assert.Throws<ArgumentException>(() => target.FormatWith(propertyObject, this.environment));
    }

    [Test]
    public void FormatWithMultipleEnvVars()
    {
        this.environment.SetEnvironmentVariable("GIT_VERSION_TEST_VAR_1", "Val-1");
        this.environment.SetEnvironmentVariable("GIT_VERSION_TEST_VAR_2", "Val-2");
        var propertyObject = new { };
        const string target = "{env:GIT_VERSION_TEST_VAR_1} and {env:GIT_VERSION_TEST_VAR_2}";
        const string expected = "Val-1 and Val-2";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void FormatWithMultipleEnvChars()
    {
        var propertyObject = new { };
        //Test the greediness of the regex in matching env: char
        const string target = "{env:env:GIT_VERSION_TEST_VAR_1} and {env:DUMMY_VAR ?? fallback}";
        const string expected = "{env:env:GIT_VERSION_TEST_VAR_1} and fallback";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void FormatWithMultipleFallbackChars()
    {
        var propertyObject = new { };
        //Test the greediness of the regex in matching env: and ?? chars
        const string target = "{env:env:GIT_VERSION_TEST_VAR_1} and {env:DUMMY_VAR ??? fallback}";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.AreEqual(target, actual);
    }

    [Test]
    public void FormatWithSingleFallbackChar()
    {
        this.environment.SetEnvironmentVariable("DUMMY_ENV_VAR", "Dummy-Val");
        var propertyObject = new { };
        //Test the sanity of the regex when there is a grammar mismatch
        const string target = "{en:DUMMY_ENV_VAR} and {env:DUMMY_ENV_VAR??fallback}";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.AreEqual(target, actual);
    }

    [Test]
    public void FormatWIthNullPropagationWithMultipleSpaces()
    {
        var propertyObject = new { SomeProperty = "Some Value" };
        const string target = "{SomeProperty} and {env:DUMMY_ENV_VAR  ??  fallback}";
        const string expected = "Some Value and fallback";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void FormatEnvVar_WithFallback_QuotedAndEmpty()
    {
        this.environment.SetEnvironmentVariable("ENV_VAR", null);
        var propertyObject = new { };
        const string target = "{env:ENV_VAR ?? \"\"}";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.That(actual, Is.EqualTo(""));
    }

    [Test]
    public void FormatProperty_String()
    {
        var propertyObject = new { Property = "Value" };
        const string target = "{Property}";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.That(actual, Is.EqualTo("Value"));
    }

    [Test]
    public void FormatProperty_Integer()
    {
        var propertyObject = new { Property = 42 };
        const string target = "{Property}";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.That(actual, Is.EqualTo("42"));
    }

    [Test]
    public void FormatProperty_NullObject()
    {
        var propertyObject = new { Property = (object?)null };
        const string target = "{Property}";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.That(actual, Is.EqualTo(""));
    }

    [Test]
    public void FormatProperty_NullInteger()
    {
        var propertyObject = new { Property = (int?)null };
        const string target = "{Property}";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.That(actual, Is.EqualTo(""));
    }

    [Test]
    public void FormatProperty_String_WithFallback()
    {
        var propertyObject = new { Property = "Value" };
        const string target = "{Property ?? fallback}";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.That(actual, Is.EqualTo("Value"));
    }

    [Test]
    public void FormatProperty_Integer_WithFallback()
    {
        var propertyObject = new { Property = 42 };
        const string target = "{Property ?? fallback}";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.That(actual, Is.EqualTo("42"));
    }

    [Test]
    public void FormatProperty_NullObject_WithFallback()
    {
        var propertyObject = new { Property = (object?)null };
        const string target = "{Property ?? fallback}";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.That(actual, Is.EqualTo("fallback"));
    }

    [Test]
    public void FormatProperty_NullInteger_WithFallback()
    {
        var propertyObject = new { Property = (int?)null };
        const string target = "{Property ?? fallback}";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.That(actual, Is.EqualTo("fallback"));
    }

    [Test]
    public void FormatProperty_NullObject_WithFallback_Quoted()
    {
        var propertyObject = new { Property = (object?)null };
        const string target = "{Property ?? \"fallback\"}";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.That(actual, Is.EqualTo("fallback"));
    }

    [Test]
    public void FormatProperty_NullObject_WithFallback_QuotedAndPadded()
    {
        var propertyObject = new { Property = (object?)null };
        const string target = "{Property ?? \" fallback \"}";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.That(actual, Is.EqualTo(" fallback "));
    }

    [Test]
    public void FormatProperty_NullObject_WithFallback_QuotedAndEmpty()
    {
        var propertyObject = new { Property = (object?)null };
        const string target = "{Property ?? \"\"}";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.That(actual, Is.EqualTo(""));
    }
}
