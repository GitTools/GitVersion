using GitVersion.Formatting;

namespace GitVersion.Core.Tests.Formatting;

[TestFixture]
public class StringFormatterTests
{
    [Test]
    public void Priority_ShouldBe2() => new StringFormatter().Priority.ShouldBe(2);

    [TestCase("u")]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("invalid")]
    public void TryFormat_NullValue_ReturnsFalse(string format)
    {
        var sut = new StringFormatter();
        var result = sut.TryFormat(null, format, out var formatted);
        result.ShouldBeFalse();
        formatted.ShouldBeEmpty();
    }

    [TestCase("hello", "u", "HELLO")]
    [TestCase("HELLO", "l", "hello")]
    [TestCase("hello world", "t", "Hello World")]
    [TestCase("hELLO", "s", "Hello")]
    [TestCase("hello world", "c", "HelloWorld")]
    public void TryFormat_ValidFormats_ReturnsExpectedResult(string input, string format, string expected)
    {
        var sut = new StringFormatter();
        var result = sut.TryFormat(input, format, out var formatted);
        result.ShouldBeTrue();
        formatted.ShouldBe(expected);
    }

    [TestCase("", "s")]
    [TestCase("", "u")]
    [TestCase("", "l")]
    [TestCase("", "t")]
    [TestCase("", "c")]
    public void TryFormat_EmptyStringWithValidFormat_ReturnsEmpty(string input, string format)
    {
        var sut = new StringFormatter();
        var result = sut.TryFormat(input, format, out var formatted);
        result.ShouldBeTrue();
        formatted.ShouldBeEmpty();
    }

    [TestCase("test", "")]
    [TestCase("test", "   ")]
    [TestCase("test", "invalid")]
    [TestCase("invalid", "")]
    [TestCase("invalid", "   ")]
    [TestCase("invalid", "invalid")]
    public void TryFormat_ValidStringWithInvalidFormat_ReturnsFalse(string input, string format)
    {
        var sut = new StringFormatter();
        var result = sut.TryFormat(input, format, out var formatted);
        result.ShouldBeFalse();
        formatted.ShouldBeEmpty();
    }

    [TestCase("", "")]
    [TestCase("", "   ")]
    [TestCase("", "invalid")]
    [TestCase("   ", "")]
    [TestCase("   ", "   ")]
    [TestCase("   ", "invalid")]
    public void TryFormat_EmptyOrWhitespaceStringWithInvalidFormat_ReturnsTrue(string input, string format)
    {
        var sut = new StringFormatter();
        var result = sut.TryFormat(input, format, out var formatted);
        result.ShouldBeTrue();
        formatted.ShouldBeEmpty();
    }
}
