using GitVersion.Formatting;

namespace GitVersion.Tests.Formatting;

[TestFixture]
public class DateFormatterTests
{
    [Test]
    public void Priority_ShouldBe2() => new DateFormatter().Priority.ShouldBe(2);

    [Test]
    public void TryFormat_NullValue_ReturnsFalse()
    {
        var sut = new DateFormatter();
        var result = sut.TryFormat(null, "yyyy-MM-dd", out var formatted);
        result.ShouldBeFalse();
        formatted.ShouldBeEmpty();
    }

    [TestCase("2021-01-01", "yyyy-MM-dd", "2021-01-01")]
    [TestCase("2021-01-01T12:00:00Z", "yyyy-MM-ddTHH:mm:ssZ", "2021-01-01T12:00:00Z")]
    public void TryFormat_ValidDateFormats_ReturnsExpectedResult(string input, string format, string expected)
    {
        var date = DateTime.Parse(input);
        var sut = new DateFormatter();
        var result = sut.TryFormat(date, format, out var formatted);
        result.ShouldBeTrue();
        formatted.ShouldBe(expected);
    }
}
