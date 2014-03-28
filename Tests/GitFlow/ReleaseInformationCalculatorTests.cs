using GitVersion;
using NUnit.Framework;

[TestFixture]
public class ReleaseInformationCalculatorTests
{

    [TestCase(null, Stability.Final, null)]
    [TestCase("1.0.0-beta", Stability.Beta, null)]
    [TestCase("1.0.0-beta3", Stability.Beta, 3)]
    [TestCase("1.0.0-alpha", Stability.Alpha, null)]
    [TestCase("1.0.0-alpha4", Stability.Alpha, 4)]
    [TestCase("1.0.0-rc", Stability.ReleaseCandidate, null)]
    [TestCase("1.0.0-rc3", Stability.ReleaseCandidate, 3)]
    [TestCase("1.0.0-rc03", Stability.ReleaseCandidate, 3)]
    [TestCase("1.0.0-beta3f", null, null)]
    [TestCase("1.0.0-notAStability1", null, 1)]
    public void ValidateVersionParsing(string tag, Stability? stability, int? releaseNumber)
    {
        var releaseInfo = ReleaseInformationCalculator.Calculate(null, tag);

        Assert.AreEqual(stability, releaseInfo.Stability);
        Assert.AreEqual(releaseNumber, releaseInfo.ReleaseNumber);
    }

}