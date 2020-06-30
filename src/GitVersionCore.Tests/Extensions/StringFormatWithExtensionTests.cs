using System;
using GitVersion;
using GitVersion.Helpers;
using GitVersionCore.Tests.Helpers;
using NUnit.Framework;

namespace GitVersionCore.Tests
{
    [TestFixture]
    public class StringFormatWithExtensionTests
    {
        private IEnvironment environment;

        [SetUp]
        public void Setup()
        {
            environment = new TestEnvironment();
        }

        [Test]
        public void FormatWithNoTokens()
        {
            var propertyObject = new { };
            var target = "Some String without tokens";
            var expected = target;
            var actual = target.FormatWith(propertyObject, environment);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void FormatWithSingleSimpleToken()
        {
            var propertyObject = new { SomeProperty = "SomeValue" };
            var target = "{SomeProperty}";
            var expected = "SomeValue";
            var actual = target.FormatWith(propertyObject, environment);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void FormatWithMultipleTokensAndVerbatimText()
        {
            var propertyObject = new { SomeProperty = "SomeValue", AnotherProperty = "Other Value" };
            var target = "{SomeProperty} some text {AnotherProperty}";
            var expected = "SomeValue some text Other Value";
            var actual = target.FormatWith(propertyObject, environment);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void FormatWithEnvVarToken()
        {
            environment.SetEnvironmentVariable("GIT_VERSION_TEST_VAR", "Env Var Value");
            var propertyObject = new { };
            var target = "{env:GIT_VERSION_TEST_VAR}";
            var expected = "Env Var Value";
            var actual = target.FormatWith(propertyObject, environment);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void FormatWithEnvVarTokenWithFallback()
        {
            environment.SetEnvironmentVariable("GIT_VERSION_TEST_VAR", "Env Var Value");
            var propertyObject = new { };
            var target = "{env:GIT_VERSION_TEST_VAR ?? fallback}";
            var expected = "Env Var Value";
            var actual = target.FormatWith(propertyObject, environment);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void FormatWithUnsetEnvVarToken_WithFallback()
        {
            environment.SetEnvironmentVariable("GIT_VERSION_UNSET_TEST_VAR", null);
            var propertyObject = new { };
            var target = "{env:GIT_VERSION_UNSET_TEST_VAR ?? fallback}";
            var expected = "fallback";
            var actual = target.FormatWith(propertyObject, environment);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void FormatWithUnsetEnvVarToken_WithoutFallback()
        {
            environment.SetEnvironmentVariable("GIT_VERSION_UNSET_TEST_VAR", null);
            var propertyObject = new { };
            var target = "{env:GIT_VERSION_UNSET_TEST_VAR}";
            Assert.Throws<ArgumentException>(() => target.FormatWith(propertyObject, environment));
        }

        [Test]
        public void FormatWithMultipleEnvVars()
        {
            environment.SetEnvironmentVariable("GIT_VERSION_TEST_VAR_1", "Val-1");
            environment.SetEnvironmentVariable("GIT_VERSION_TEST_VAR_2", "Val-2");
            var propertyObject = new { };
            var target = "{env:GIT_VERSION_TEST_VAR_1} and {env:GIT_VERSION_TEST_VAR_2}";
            var expected = "Val-1 and Val-2";
            var actual = target.FormatWith(propertyObject, environment);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void FormatWithMultipleEnvChars()
        {
            var propertyObject = new { };
            //Test the greediness of the regex in matching env: char
            var target = "{env:env:GIT_VERSION_TEST_VAR_1} and {env:DUMMY_VAR ?? fallback}";
            var expected = "{env:env:GIT_VERSION_TEST_VAR_1} and fallback";
            var actual = target.FormatWith(propertyObject, environment);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void FormatWithMultipleFallbackChars()
        {
            var propertyObject = new { };
            //Test the greediness of the regex in matching env: and ?? chars
            var target = "{env:env:GIT_VERSION_TEST_VAR_1} and {env:DUMMY_VAR ??? fallback}";
            var actual = target.FormatWith(propertyObject, environment);
            Assert.AreEqual(target, actual);
        }

        [Test]
        public void FormatWithSingleFallbackChar()
        {
            environment.SetEnvironmentVariable("DUMMY_ENV_VAR", "Dummy-Val");
            var propertyObject = new { };
            //Test the sanity of the regex when there is a grammar mismatch
            var target = "{en:DUMMY_ENV_VAR} and {env:DUMMY_ENV_VAR??fallback}";
            var actual = target.FormatWith(propertyObject, environment);
            Assert.AreEqual(target, actual);
        }

        [Test]
        public void FormatWIthNullPropagationWithMultipleSpaces()
        {
            var propertyObject = new { SomeProperty = "Some Value" };
            var target = "{SomeProperty} and {env:DUMMY_ENV_VAR  ??  fallback}";
            var expected = "Some Value and fallback";
            var actual = target.FormatWith(propertyObject, environment);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void FormatEnvVar_WithFallback_QuotedAndEmpty()
        {
            environment.SetEnvironmentVariable("ENV_VAR", null);
            var propertyObject = new { };
            var target = "{env:ENV_VAR ?? \"\"}";
            var actual = target.FormatWith(propertyObject, environment);
            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void FormatProperty_String()
        {
            var propertyObject = new { Property = "Value" };
            var target = "{Property}";
            var actual = target.FormatWith(propertyObject, environment);
            Assert.That(actual, Is.EqualTo("Value"));
        }

        [Test]
        public void FormatProperty_Integer()
        {
            var propertyObject = new { Property = 42 };
            var target = "{Property}";
            var actual = target.FormatWith(propertyObject, environment);
            Assert.That(actual, Is.EqualTo("42"));
        }

        [Test]
        public void FormatProperty_NullObject()
        {
            var propertyObject = new { Property = (object)null };
            var target = "{Property}";
            var actual = target.FormatWith(propertyObject, environment);
            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void FormatProperty_NullInteger()
        {
            var propertyObject = new { Property = (int?)null };
            var target = "{Property}";
            var actual = target.FormatWith(propertyObject, environment);
            Assert.That(actual, Is.EqualTo(""));
        }

        [Test]
        public void FormatProperty_String_WithFallback()
        {
            var propertyObject = new { Property = "Value" };
            var target = "{Property ?? fallback}";
            var actual = target.FormatWith(propertyObject, environment);
            Assert.That(actual, Is.EqualTo("Value"));
        }

        [Test]
        public void FormatProperty_Integer_WithFallback()
        {
            var propertyObject = new { Property = 42 };
            var target = "{Property ?? fallback}";
            var actual = target.FormatWith(propertyObject, environment);
            Assert.That(actual, Is.EqualTo("42"));
        }

        [Test]
        public void FormatProperty_NullObject_WithFallback()
        {
            var propertyObject = new { Property = (object)null };
            var target = "{Property ?? fallback}";
            var actual = target.FormatWith(propertyObject, environment);
            Assert.That(actual, Is.EqualTo("fallback"));
        }

        [Test]
        public void FormatProperty_NullInteger_WithFallback()
        {
            var propertyObject = new { Property = (int?)null };
            var target = "{Property ?? fallback}";
            var actual = target.FormatWith(propertyObject, environment);
            Assert.That(actual, Is.EqualTo("fallback"));
        }

        [Test]
        public void FormatProperty_NullObject_WithFallback_Quoted()
        {
            var propertyObject = new { Property = (object)null };
            var target = "{Property ?? \"fallback\"}";
            var actual = target.FormatWith(propertyObject, environment);
            Assert.That(actual, Is.EqualTo("fallback"));
        }

        [Test]
        public void FormatProperty_NullObject_WithFallback_QuotedAndPadded()
        {
            var propertyObject = new { Property = (object)null };
            var target = "{Property ?? \" fallback \"}";
            var actual = target.FormatWith(propertyObject, environment);
            Assert.That(actual, Is.EqualTo(" fallback "));
        }

        [Test]
        public void FormatProperty_NullObject_WithFallback_QuotedAndEmpty()
        {
            var propertyObject = new { Property = (object)null };
            var target = "{Property ?? \"\"}";
            var actual = target.FormatWith(propertyObject, environment);
            Assert.That(actual, Is.EqualTo(""));
        }
    }
}
