using System;
using NUnit.Framework;
using GitVersion.Helpers;

namespace GitVersionCore.Tests
{
    [TestFixture]

    public class StringFormatWithExtensionTests
    {
        [Test]
        public void FormatWith_NoTokens()
        {
            var propertyObject = new { };
            var target = "Some String without tokens";
            var expected = target;
            var actual = target.FormatWith(propertyObject);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void FormatWith_SingleSimpleToken()
        {
            var propertyObject = new { SomeProperty = "SomeValue" };
            var target = "{SomeProperty}";
            var expected = "SomeValue";
            var actual = target.FormatWith(propertyObject);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void FormatWith_MultipleTokensAndVerbatimText()
        {
            var propertyObject = new { SomeProperty = "SomeValue", AnotherProperty = "Other Value" };
            var target = "{SomeProperty} some text {AnotherProperty}";
            var expected = "SomeValue some text Other Value";
            var actual = target.FormatWith(propertyObject);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void FormatWith_EnvVarToken()
        {
            Environment.SetEnvironmentVariable("GIT_VERSION_TEST_VAR", "Env Var Value");
            var propertyObject = new { };
            var target = "{env:GIT_VERSION_TEST_VAR}";
            var expected = "Env Var Value";
            var actual = target.FormatWith(propertyObject);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void FormatWith_EnvVarTokenWithFallback()
        {
            Environment.SetEnvironmentVariable("GIT_VERSION_TEST_VAR", "Env Var Value");
            var propertyObject = new { };
            var target = "{env:GIT_VERSION_TEST_VAR ?? fallback}";
            var expected = "Env Var Value";
            var actual = target.FormatWith(propertyObject);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void FormatWith_UnsetEnvVarTokenWithFallback()
        {
            Environment.SetEnvironmentVariable("GIT_VERSION_UNSET_TEST_VAR", null);
            var propertyObject = new { };
            var target = "{env:GIT_VERSION_UNSET_TEST_VAR ?? fallback}";
            var expected = "fallback";
            var actual = target.FormatWith(propertyObject);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void FormatWith_MultipleEnvVars()
        {
            Environment.SetEnvironmentVariable("GIT_VERSION_TEST_VAR_1", "Val-1");
            Environment.SetEnvironmentVariable("GIT_VERSION_TEST_VAR_2", "Val-2");
            var propertyObject = new { };
            var target = "{env:GIT_VERSION_TEST_VAR_1} and {env:GIT_VERSION_TEST_VAR_2}";
            var expected = "Val-1 and Val-2";
            var actual = target.FormatWith(propertyObject);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void FormatWith_MultipleEnvChars()
        {
            var propertyObject = new { };
            //Test the greediness of the regex in matching env: char
            var target = "{env:env:GIT_VERSION_TEST_VAR_1} and {env:DUMMY_VAR ?? fallback}";
            var expected = "{env:env:GIT_VERSION_TEST_VAR_1} and fallback";
            var actual = target.FormatWith(propertyObject);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void FormatWith_MultipleFallbackChars()
        {
            var propertyObject = new { };
            //Test the greediness of the regex in matching env: and ?? chars
            var target = "{env:env:GIT_VERSION_TEST_VAR_1} and {env:DUMMY_VAR ??? fallback}";
            var actual = target.FormatWith(propertyObject);
            Assert.AreEqual(target, actual);
        }

        [Test]
        public void FormatWith_SingleFallbackChar()
        {
            Environment.SetEnvironmentVariable("DUMMY_ENV_VAR", "Dummy-Val");
            var propertyObject = new { };
            //Test the sanity of the regex when there is a grammar mismatch
            var target = "{en:DUMMY_ENV_VAR} and {env:DUMMY_ENV_VAR??fallback}";
            var actual = target.FormatWith(propertyObject);
            Assert.AreEqual(target, actual);
        }

        [Test]
        public void FormatWIth_NullPropagationWithMultipleSpaces()
        {
            var propertyObject = new { SomeProperty = "Some Value" };
            var target = "{SomeProperty} and {env:DUMMY_ENV_VAR  ??  fallback}";
            var expected = "Some Value and fallback";
            var actual = target.FormatWith(propertyObject);
            Assert.AreEqual(expected, actual);
        }
    }
}
