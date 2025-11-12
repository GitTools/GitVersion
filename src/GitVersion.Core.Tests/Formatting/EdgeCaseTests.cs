using GitVersion.Core.Tests.Extensions;
using GitVersion.Formatting;

namespace GitVersion.Tests.Formatting;

[TestFixture]
public class EdgeCaseTests
{
    [TestCase(49)]
    [TestCase(50)]
    public void SanitizeFormat_WithBoundaryLengths_ReturnsInput(int length)
    {
        var input = new string('x', length);
        new InputSanitizer().SanitizeFormat(input).ShouldBe(input);
    }

    [TestCase(199)]
    [TestCase(200)]
    public void SanitizeEnvVarName_WithBoundaryLengths_ReturnsInput(int length)
    {
        var input = new string('A', length);
        new InputSanitizer().SanitizeEnvVarName(input).ShouldBe(input);
    }

    [TestCase(99)]
    [TestCase(100)]
    public void SanitizeMemberName_WithBoundaryLengths_ReturnsInput(int length)
    {
        var input = new string('A', length);
        new InputSanitizer().SanitizeMemberName(input).ShouldBe(input);
    }

    [Test]
    public void SanitizeFormat_WithUnicode_ReturnsInput()
    {
        const string unicodeFormat = "测试format";
        new InputSanitizer().SanitizeFormat(unicodeFormat).ShouldBe(unicodeFormat);
    }

    [Test]
    public void SanitizeEnvVarName_WithUnicode_ThrowsArgumentException()
    {
        const string unicodeEnvVar = "测试_VAR";
        Action act = () => new InputSanitizer().SanitizeEnvVarName(unicodeEnvVar);
        act.ShouldThrowWithMessage<ArgumentException>(
            $"Environment variable name contains disallowed characters: '{unicodeEnvVar}'");
    }

    [Test]
    public void SanitizeMemberName_WithUnicode_ThrowsArgumentException()
    {
        const string unicodeMember = "测试Member";
        Action act = () => new InputSanitizer().SanitizeMemberName(unicodeMember);
        act.ShouldThrowWithMessage<ArgumentException>(
            $"Member name contains disallowed characters: '{unicodeMember}'");
    }
}
