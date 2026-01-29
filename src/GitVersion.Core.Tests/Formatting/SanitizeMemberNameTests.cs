using System.Diagnostics.CodeAnalysis;
using GitVersion.Core.Tests.Extensions;
using GitVersion.Formatting;

namespace GitVersion.Core.Tests.Formatting;

[SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value")]
[SuppressMessage("Style", "IDE0060:Remove unused parameter")]
[TestFixture]
public class SanitizeMemberNameTests
{
    [Test]
    public void SanitizeMemberName_WithValidName_ReturnsInput()
    {
        var sut = new InputSanitizer();
        const string validName = "ValidMemberName";
        sut.SanitizeMemberName(validName).ShouldBe(validName);
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("\t")]
    public void SanitizeMemberName_WithEmptyOrWhitespace_ThrowsArgumentException(string invalidName)
    {
        var sut = new InputSanitizer();
        Action act = () => sut.SanitizeMemberName(invalidName);
        act.ShouldThrowWithMessage<ArgumentException>("Member name cannot be empty.");
    }

    [Test]
    public void SanitizeMemberName_WithTooLongName_ThrowsArgumentException()
    {
        var sut = new InputSanitizer();
        var longName = new string('A', 101);
        Action act = () => sut.SanitizeMemberName(longName);
        act.ShouldThrowWithMessage<ArgumentException>("Member name too long: 'AAAAAAAAAAAAAAAAAAAA...'");
    }

    [Test]
    public void SanitizeMemberName_WithMaxValidLength_ReturnsInput()
    {
        var sut = new InputSanitizer();
        var maxLengthName = new string('A', 100);
        sut.SanitizeMemberName(maxLengthName).ShouldBe(maxLengthName);
    }

    [Test]
    public void SanitizeMemberName_WithInvalidCharacters_ThrowsArgumentException()
    {
        var sut = new InputSanitizer();
        const string invalidName = "Invalid@Member";
        Action act = () => sut.SanitizeMemberName(invalidName);
        act.ShouldThrowWithMessage<ArgumentException>("Member name contains disallowed characters: 'Invalid@Member'");
    }

    [TestCase("PropertyName")]
    [TestCase("FieldName")]
    [TestCase("Member123")]
    [TestCase("_privateMember")]
    [TestCase("CamelCaseName")]
    [TestCase("PascalCaseName")]
    [TestCase("member_with_underscores")]
    public void SanitizeMemberName_WithValidNames_ReturnsInput(string validName)
    {
        var sut = new InputSanitizer();
        sut.SanitizeMemberName(validName).ShouldBe(validName);
    }

    [TestCase("member.nested")]
    [TestCase("Parent.Child.GrandChild")]
    public void SanitizeMemberName_WithDottedNames_HandledByRegex(string dottedName)
    {
        var sut = new InputSanitizer();
        Action act = () => sut.SanitizeMemberName(dottedName);

        act.ShouldNotThrow();
    }
}
