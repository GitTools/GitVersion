using GitVersion.VersionCalculation;

namespace GitVersion.Tests.VersionCalculation;

[TestFixture]
public class VersionBumpMessageParserTests
{
    [TestCase("=semver: NONE", VersionField.None)]
    [TestCase("=semver: patch", VersionField.Patch)]
    [TestCase("=semver: Minor", VersionField.Minor)]
    [TestCase("=SEMVER: major", VersionField.Major)]
    public void ParsesTheDefaultOverrideMessage(string message, VersionField expectedIncrement)
        => VersionBumpMessageParser.GetIncrementOverride(message, pattern: null).ShouldBe(expectedIncrement);

    [Test]
    public void ReturnsNullWhenTheOverrideMessageDoesNotMatch()
        => VersionBumpMessageParser.GetIncrementOverride("+semver: major", pattern: null).ShouldBeNull();

    [TestCase("micro")]
    [TestCase("1")]
    public void RejectsAnUnsupportedCapturedIncrement(string value)
    {
        const string pattern = @"=semver:\s?(?<increment>\w+)";

        var exception = Should.Throw<GitVersionException>(
            () => VersionBumpMessageParser.GetIncrementOverride($"=semver: {value}", pattern));

        exception.Message.ShouldContain("must capture one of");
        exception.Message.ShouldContain("named group called 'increment'");
    }
}
