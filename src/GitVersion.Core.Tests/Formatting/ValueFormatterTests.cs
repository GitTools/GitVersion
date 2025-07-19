using System.Globalization;
using GitVersion.Formatting;

namespace GitVersion.Core.Tests.Formatting;

[TestFixture]
public class ValueFormatterTests
{
    [Test]
    public void TryFormat_NullValue_ReturnsFalse()
    {
        var result = ValueFormatter.Default.TryFormat(null, "any", out var formatted);
        result.ShouldBeFalse();
        formatted.ShouldBeEmpty();
    }

    [Test]
    public void TryFormat_String_UsesStringFormatter()
    {
        var result = ValueFormatter.Default.TryFormat("hello", "u", out var formatted);
        result.ShouldBeTrue();
        formatted.ShouldBe("HELLO");
    }

    [Test]
    public void TryFormat_Number_UsesNumericFormatter()
    {
        var result = ValueFormatter.Default.TryFormat(1234.5678, "n", out var formatted);
        result.ShouldBeTrue();
        formatted.ShouldBe("1,234.57");
    }

    [Test]
    public void TryFormat_Date_UsesDateFormatter()
    {
        var date = new DateTime(2023, 12, 25);
        var result = ValueFormatter.Default.TryFormat(date, "yyyy-MM-dd", out var formatted);
        result.ShouldBeTrue();
        formatted.ShouldBe("2023-12-25");
    }

    [Test]
    public void TryFormat_FormattableObject_UsesFormattableFormatter()
    {
        var value = 123.456m;
        var result = ValueFormatter.Default.TryFormat(value, "C", out var formatted);
        result.ShouldBeTrue();
        formatted.ShouldBe("¤123.46");
    }

    [Test]
    public void TryFormat_InvalidFormat_ReturnsFalse()
    {
        var result = ValueFormatter.Default.TryFormat("test", "invalidformat", out var formatted);
        result.ShouldBeFalse();
        formatted.ShouldBeEmpty();
    }

    [Test]
    public void RegisterFormatter_AddsNewFormatter()
    {
        var customFormatter = new TestFormatter { Priority = 0 };
        IValueFormatterCombiner sut = new ValueFormatter();
        sut.RegisterFormatter(customFormatter);
        var result = sut.TryFormat("test", "custom", out var formatted);
        result.ShouldBeTrue();
        formatted.ShouldBe("CUSTOM:test");
    }

    [Test]
    public void RemoveFormatter_RemovesExistingFormatter()
    {
        IValueFormatterCombiner sut = new ValueFormatter();
        // First verify numeric formatting works
        sut.TryFormat(123.45, "n1", out var before);
        before.ShouldBe("123.5");

        sut.RemoveFormatter<NumericFormatter>();

        // Now numeric formatting will still happen, but via the FormattableFormatter
        var result = sut.TryFormat(123.45, "n1", out var afterFormatted);
        result.ShouldBeTrue();
        afterFormatted.ShouldBe("123.5");

        sut.RemoveFormatter<FormattableFormatter>();

        // Now numeric formatting will now not be handled by any formatter that remains
        result = sut.TryFormat(123.45, "n1", out var afterNotFormatted);
        result.ShouldBeFalse();
        afterNotFormatted.ShouldBeEmpty();
    }

    [Test]
    public void Formatters_ExecuteInPriorityOrder()
    {
        IValueFormatterCombiner sut = new ValueFormatter();
        var highPriorityFormatter = new TestFormatter { Priority = 0 };
        var lowPriorityFormatter = new TestFormatter { Priority = 99 };

        sut.RegisterFormatter(lowPriorityFormatter);
        sut.RegisterFormatter(highPriorityFormatter);
        var result = sut.TryFormat("test", "custom", out var formatted);
        result.ShouldBeTrue();

        // Should use the high priority formatter first
        formatted.ShouldBe("CUSTOM:test");
    }

    private class TestFormatter : IValueFormatter
    {
        public int Priority { get; init; }

        public bool TryFormat(object? value, string format, out string result)
        {
            if (format == "custom" && value is string str)
            {
                result = $"CUSTOM:{str}";
                return true;
            }

            result = string.Empty;
            return false;
        }

        public bool TryFormat(object? value, string format, CultureInfo cultureInfo, out string result)
            => TryFormat(value, format, out result);
    }
}
