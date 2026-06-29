using System.Globalization;
using GitVersion.Formatting;

namespace GitVersion.Tests;

[TestFixture]
public class StringFormatWithExtensionTests
{
    private TestEnvironment environment = null!;

    [SetUp]
    public void Setup() => this.environment = new TestEnvironment();

    [Test]
    public void FormatWithNoTokens()
    {
        var propertyObject = new { };
        const string expected = "Some String without tokens";
        var actual = expected.FormatWith(propertyObject, this.environment);
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void FormatWithSingleSimpleToken()
    {
        var propertyObject = new { SomeProperty = "SomeValue" };
        const string target = "{SomeProperty}";
        const string expected = "SomeValue";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void FormatWithMultipleTokensAndVerbatimText()
    {
        var propertyObject = new { SomeProperty = "AValue", AnotherProperty = "Other Value" };
        const string target = "{SomeProperty} some text {AnotherProperty}";
        const string expected = "AValue some text Other Value";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void FormatWithEnvVarToken()
    {
        this.environment.SetEnvironmentVariable("GIT_VERSION_TEST_VAR", "Env Var Value");
        var propertyObject = new { };
        const string target = "{env:GIT_VERSION_TEST_VAR}";
        const string expected = "Env Var Value";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void FormatWithEnvVarTokenWithFallback()
    {
        this.environment.SetEnvironmentVariable("GIT_VERSION_TEST_VAR", "Env Var Value");
        var propertyObject = new { };
        const string target = "{env:GIT_VERSION_TEST_VAR ?? \"fallback\"}";
        const string expected = "Env Var Value";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void FormatWithUnsetEnvVarToken_WithFallback()
    {
        this.environment.SetEnvironmentVariable("GIT_VERSION_UNSET_TEST_VAR", null);
        var propertyObject = new { };
        const string target = "{env:GIT_VERSION_UNSET_TEST_VAR ?? \"fallback\"}";
        const string expected = "fallback";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.That(actual, Is.EqualTo(expected));
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
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void FormatWithMultipleEnvChars()
    {
        var propertyObject = new { };
        const string target = "{env:env:GIT_VERSION_TEST_VAR_1} and {env:DUMMY_VAR ?? \"fallback\"}";
        Assert.Throws<ArgumentException>(() => target.FormatWith(propertyObject, this.environment));
    }

    [Test]
    public void FormatWithMultipleFallbackChars()
    {
        var propertyObject = new { };
        const string target = " and {env:DUMMY_VAR ??? \"fallback\"}";
        Assert.Throws<FormatException>(() => target.FormatWith(propertyObject, this.environment));
    }

    [Test]
    public void FormatWithSingleFallbackChar()
    {
        this.environment.SetEnvironmentVariable("DUMMY_ENV_VAR", "DummyVal");
        var propertyObject = new { };
        const string target = "{en:DUMMY_ENV_VAR} and {env:DUMMY_ENV_VAR??\"fallback\"}";
        Assert.Throws<ArgumentException>(() => target.FormatWith(propertyObject, this.environment));
    }

    [Test]
    public void FormatWithNullPropagationWithMultipleSpaces()
    {
        var propertyObject = new { SomeProperty = "Some Value" };
        const string target = "{SomeProperty} and {env:DUMMY_ENV_VAR  ??  \"fallback\"}";
        const string expected = "Some Value and fallback";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void FormatWithMissingPropertyAndEnvFallback()
    {
        this.environment.SetEnvironmentVariable("DUMMY_ENV_VAR", "Dummy-Value");
        var propertyObject = new { };
        const string target = "{SomeProperty ?? env:DUMMY_ENV_VAR}";
        const string expected = "Dummy-Value";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void FormatWithMissingEnvAndPropertyFallback()
    {
        var propertyObject = new { SomeProperty = "Some Value" };
        const string target = "{env:DUMMY_ENV_VAR ?? SomeProperty}";
        const string expected = "Some Value";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void FormatWithMultiplePropertiesAndNoFallback()
    {
        var propertyObject = new { };
        const string target = "{SomeProperty ?? SomeOtherProperty ?? MissingProp}";
        Assert.Throws<ArgumentException>(() => target.FormatWith(propertyObject, this.environment));
    }

    [Test]
    public void FormatWithMultiplePropertiesAndQuotedFallback()
    {
        var propertyObject = new { };
        const string target = "{SomeProperty ?? SomeOtherProperty ?? \"fallback\"}";
        const string expected = "fallback";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void FormatWithMultipleAvailablePropertiesAndFallback()
    {
        var propertyObject = new { SomeOtherProperty = "Some-Value" };
        const string target = "{SomeProperty ?? SomeOtherProperty ?? \"fallback\"}";
        const string expected = "Some-Value";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void FormatWithMissingPropertiesAndIntegerFallback()
    {
        var propertyObject = new { };
        const string target = "{SomeProperty ?? SomeOtherProperty ?? 47}";
        const string expected = "47";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void FormatWithPropertyAndEnvAndFormatters()
    {
        this.environment.SetEnvironmentVariable("DUMMY_ENV_VAR", "DummyVal");
        var propertyObject = new { SomeProperty = "TheValue" };
        const string target = "{SomeProperty:l} and {env:DUMMY_ENV_VAR:l}";
        const string expected = "thevalue and dummyval";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCase("{env:VARIABLE ?? \"\"}", null, null, "")]
    [TestCase("{env:MISSING ?? env:VARIABLE}", "Var", null, "Var")]
    [TestCase("{Property ?? \"\"}", null, null, "")]
    [TestCase("{Property ?? 47}", null, null, "47")]
    [TestCase("{Property ?? env:VARIABLE}", null, "Branch", "Branch")]
    [TestCase("{Property ?? env:VARIABLE ?? \"\"}", null, null, "")]
    [TestCase("{Property ?? env:VARIABLE ?? 42}", null, null, "42")]
    public void FormatWith_EnvVarAndPropertyAndFallback_DoesNotThrow(string input, string? envVar, string? property, string expected)
    {
        if (envVar != null)
        {
            this.environment.SetEnvironmentVariable("VARIABLE", envVar);
        }

        object propertyObject = property != null
            ? new { Property = property }
            : new { };

        var actual = input.FormatWith(propertyObject, this.environment);
        Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCase("{env:VARIABLE}")]
    [TestCase("{Property}")]
    [TestCase("{Property ?? env:VARIABLE}")]
    [TestCase("{Property ?? Property}")]
    public void FormatWith_MissingEnvVarOrPropertyAndNoFallback_Throws(string input)
    {
        object propertyObject = new { };
        Assert.Throws<ArgumentException>(() => input.FormatWith(propertyObject, this.environment));
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
        const string target = "{Property ?? \"fallback\"}";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.That(actual, Is.EqualTo("Value"));
    }

    [Test]
    public void FormatProperty_Integer_WithFallback()
    {
        var propertyObject = new { Property = 42 };
        const string target = "{Property ?? \"fallback\"}";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.That(actual, Is.EqualTo("42"));
    }

    [Test]
    public void FormatProperty_NullObject_WithFallback()
    {
        var propertyObject = new { Property = (object?)null };
        const string target = "{Property ?? \"fallback\"}";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.That(actual, Is.EqualTo("fallback"));
    }

    [Test]
    public void FormatProperty_NullInteger_WithFallback()
    {
        var propertyObject = new { Property = (int?)null };
        const string target = "{Property ?? 43}";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.That(actual, Is.EqualTo("43"));
    }

    [Test]
    public void FormatProperty_NullObject_WithFallback_Quoted()
    {
        var propertyObject = new { Property = (object?)null };
        const string target = "{Property ?? \"literal\"}";
        var actual = target.FormatWith(propertyObject, this.environment);
        Assert.That(actual, Is.EqualTo("literal"));
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

    [Test]
    public void FormatAssemblyInformationalVersionWithSemanticVersionCustomFormattedVersionSourceDistance()
    {
        var semanticVersion = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            PreReleaseTag = new SemanticVersionPreReleaseTag(string.Empty, 9, true),
            BuildMetaData = new SemanticVersionBuildMetaData("Branch.main")
            {
                Branch = "main",
                VersionSourceSha = "versionSourceSha",
                Sha = "commitSha",
                ShortSha = "commitShortSha",
                VersionSourceDistance = 42,
                CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z", CultureInfo.InvariantCulture)
            }
        };
        const string expected = "1.2.3-0042";
        var target = "{Major}.{Minor}.{Patch}-{VersionSourceDistance:0000}";
        var actual = target.FormatWith(semanticVersion, this.environment);
        Assert.That(actual, Is.EqualTo(expected));

        target = "{Major}.{Minor}.{Patch}-{CommitsSinceVersionSource:0000}";
        actual = target.FormatWith(semanticVersion, this.environment);
        Assert.That(actual, Is.EqualTo(expected));
    }
}
