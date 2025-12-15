using GitVersion.Formatting;

namespace GitVersion.Tests.Formatting;

[TestFixture]
public class NumericFormatterTests
{
    [Test]
    public void Priority_ShouldBe1() => new NumericFormatter().Priority.ShouldBe(1);

    [Test]
    public void TryFormat_NullValue_ReturnsFalse()
    {
        var sut = new NumericFormatter();
        var result = sut.TryFormat(null, "n", out var formatted);
        result.ShouldBeFalse();
        formatted.ShouldBeEmpty();
    }

    [TestCase("1234.5678", "n", "1,234.57")]
    [TestCase("1234.5678", "f2", "1234.57")]
    [TestCase("1234.5678", "f0", "1235")]
    [TestCase("1234.5678", "g", "1234.5678")]
    [TestCase("1234", "d8", "00001234")]
    [TestCase("1234", "x8", "000004d2")]
    [TestCase("12", "b8", "00001100")]
    public void TryFormat_ValidFormats_ReturnsExpectedResult(string input, string format, string expected)
    {
        var sut = new NumericFormatter();
        var result = sut.TryFormat(input, format, out var formatted);
        result.ShouldBeTrue();
        formatted.ShouldBe(expected);
    }
    [Test]
    public void TryFormat_UnsupportedFormat_ReturnsFalse()
    {
        var sut = new NumericFormatter();
        var result = sut.TryFormat(1234.5678, "z", out var formatted);
        result.ShouldBeFalse();
        formatted.ShouldBeEmpty();
    }
}
