using System.Globalization;
using GitVersion.Formatting;

namespace GitVersion.Core.Tests.Formatting;

[TestFixture]
public class FormattableFormatterTests
{
    [Test]
    public void Priority_ShouldBe2() => new FormattableFormatter().Priority.ShouldBe(2);

    [Test]
    public void TryFormat_NullValue_ReturnsFalse()
    {
        var sut = new FormattableFormatter();
        var result = sut.TryFormat(null, "G", CultureInfo.InvariantCulture, out var formatted);
        result.ShouldBeFalse();
        formatted.ShouldBeEmpty();
    }

    [TestCase(123.456, "F2", "123.46")]
    [TestCase(1234.456, "F2", "1234.46")]
    [TestCase(123.456, "C", "¤123.46")]
    [TestCase(123.456, "P", "12,345.60 %")]
    [TestCase(1234567890, "N0", "1,234,567,890")]
    public void TryFormat_ValidFormats_ReturnsExpectedResult(object input, string format, string expected)
    {
        var sut = new FormattableFormatter();
        var result = sut.TryFormat(input, format, out var formatted);
        result.ShouldBeTrue();
        formatted.ShouldBe(expected);
    }

    [TestCase(1234567890, "Z", "Format 'Z' is not supported in FormattableFormatter")]
    public void TryFormat_UnsupportedFormat_ReturnsFalse(object input, string format, string expected)
    {
        var sut = new FormattableFormatter();
        var result = sut.TryFormat(input, format, out var formatted);
        result.ShouldBeFalse();
        formatted.ShouldBe(expected);
    }
}
