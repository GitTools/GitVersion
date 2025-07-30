using GitVersion.Core.Tests.Extensions;
using GitVersion.Formatting;

namespace GitVersion.Tests.Helpers;

[TestFixture]
public partial class InputSanitizerTests
{
    [TestFixture]
    public class SanitizeFormatTests : InputSanitizerTests
    {
        [Test]
        public void SanitizeFormat_WithValidFormat_ReturnsInput()
        {
            var sut = new InputSanitizer();
            const string validFormat = "yyyy-MM-dd";
            sut.SanitizeFormat(validFormat).ShouldBe(validFormat);
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase("\t")]
        public void SanitizeFormat_WithEmptyOrWhitespace_ThrowsFormatException(string invalidFormat)
        {
            var sut = new InputSanitizer();
            Action act = () => sut.SanitizeFormat(invalidFormat);
            act.ShouldThrowWithMessage<FormatException>("Format string cannot be empty.");
        }

        [Test]
        public void SanitizeFormat_WithTooLongFormat_ThrowsFormatException()
        {
            var sut = new InputSanitizer();
            var longFormat = new string('x', 51);
            Action act = () => sut.SanitizeFormat(longFormat);
            act.ShouldThrowWithMessage<FormatException>("Format string too long: 'xxxxxxxxxxxxxxxxxxxx...'");
        }

        [Test]
        public void SanitizeFormat_WithMaxValidLength_ReturnsInput()
        {
            var sut = new InputSanitizer();
            var maxLengthFormat = new string('x', 50);
            sut.SanitizeFormat(maxLengthFormat).ShouldBe(maxLengthFormat);
        }

        [TestCase("\r", TestName = "SanitizeFormat_ControlChar_CR")]
        [TestCase("\n", TestName = "SanitizeFormat_ControlChar_LF")]
        [TestCase("\0", TestName = "SanitizeFormat_ControlChar_Null")]
        [TestCase("\x01", TestName = "SanitizeFormat_ControlChar_0x01")]
        [TestCase("\x1F", TestName = "SanitizeFormat_ControlChar_0x1F")]
        public void SanitizeFormat_WithControlCharacters_ThrowsFormatException(string controlChar)
        {
            var sut = new InputSanitizer();
            var formatWithControl = $"valid{controlChar}format";
            Action act = () => sut.SanitizeFormat(formatWithControl);
            act.ShouldThrowWithMessage<FormatException>("Format string contains invalid control characters");
        }

        [Test]
        public void SanitizeFormat_WithTabCharacter_ReturnsInput()
        {
            var sut = new InputSanitizer();
            const string formatWithTab = "format\twith\ttab";
            sut.SanitizeFormat(formatWithTab).ShouldBe(formatWithTab);
        }

        [TestCase("yyyy-MM-dd")]
        [TestCase("HH:mm:ss")]
        [TestCase("0.00")]
        [TestCase("C2")]
        [TestCase("X8")]
        [TestCase("format with spaces")]
        [TestCase("format-with-dashes")]
        [TestCase("format_with_underscores")]
        public void SanitizeFormat_WithValidFormats_ReturnsInput(string validFormat)
        {
            var sut = new InputSanitizer();
            sut.SanitizeFormat(validFormat).ShouldBe(validFormat);
        }
    }
}
