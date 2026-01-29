using System.Diagnostics.CodeAnalysis;
using GitVersion.Core.Tests.Extensions;
using GitVersion.Formatting;

namespace GitVersion.Core.Tests.Formatting;

[SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value")]
[SuppressMessage("Style", "IDE0060:Remove unused parameter")]
[TestFixture]
public class SanitizeEnvVarNameTests
{
    [Test]
    public void SanitizeEnvVarName_WithValidName_ReturnsInput()
    {
        var sut = new InputSanitizer();
        const string validName = "VALID_ENV_VAR";
        sut.SanitizeEnvVarName(validName).ShouldBe(validName);
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("\t")]
    public void SanitizeEnvVarName_WithEmptyOrWhitespace_ThrowsArgumentException(string invalidName)
    {
        var sut = new InputSanitizer();
        Action act = () => sut.SanitizeEnvVarName(invalidName);
        act.ShouldThrowWithMessage<ArgumentException>("Environment variable name cannot be null or empty.");
    }

    [Test]
    public void SanitizeEnvVarName_WithTooLongName_ThrowsArgumentException()
    {
        var sut = new InputSanitizer();
        var longName = new string('A', 201);
        Action act = () => sut.SanitizeEnvVarName(longName);
        act.ShouldThrowWithMessage<ArgumentException>("Environment variable name too long: 'AAAAAAAAAAAAAAAAAAAA...'");
    }

    [Test]
    public void SanitizeEnvVarName_WithMaxValidLength_ReturnsInput()
    {
        var sut = new InputSanitizer();
        var maxLengthName = new string('A', 200);
        sut.SanitizeEnvVarName(maxLengthName).ShouldBe(maxLengthName);
    }

    [Test]
    public void SanitizeEnvVarName_WithInvalidCharacters_ThrowsArgumentException()
    {
        var sut = new InputSanitizer();
        const string invalidName = "INVALID@NAME";
        Action act = () => sut.SanitizeEnvVarName(invalidName);
        act.ShouldThrowWithMessage<ArgumentException>("Environment variable name contains disallowed characters: 'INVALID@NAME'");
    }

    [TestCase("PATH")]
    [TestCase("HOME")]
    [TestCase("USER_NAME")]
    [TestCase("MY_VAR_123")]
    [TestCase("_PRIVATE_VAR")]
    [TestCase("VAR123")]
    public void SanitizeEnvVarName_WithValidNames_ReturnsInput(string validName)
    {
        var sut = new InputSanitizer();
        sut.SanitizeEnvVarName(validName).ShouldBe(validName);
    }
}
