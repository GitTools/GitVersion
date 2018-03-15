using System;

using GitVersion;
using NUnit.Framework;

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
            var target = "{$GIT_VERSION_TEST_VAR}";
            var expected = "Env Var Value";
            var actual = target.FormatWith(propertyObject);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void FormatWith_EnvVarTokenWithFallback()
        {
            Environment.SetEnvironmentVariable("GIT_VERSION_TEST_VAR", "Env Var Value");
            var propertyObject = new { };
            var target = "{$GIT_VERSION_TEST_VAR??fallback}";
            var expected = "Env Var Value";
            var actual = target.FormatWith(propertyObject);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void FormatWith_UnsetEnvVarTokenWithFallback()
        {
            Environment.SetEnvironmentVariable("GIT_VERSION_UNSET_TEST_VAR", null);
            var propertyObject = new { };
            var target = "{$GIT_VERSION_UNSET_TEST_VAR??fallback}";
            var expected = "fallback";
            var actual = target.FormatWith(propertyObject);
            Assert.AreEqual(expected, actual);
        }
    }
}
