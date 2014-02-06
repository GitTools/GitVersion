using GitFlowVersion;
using NUnit.Framework;

[TestFixture]
public class ReleaseInformationCalculatorTests
{

    [TestCase(null, Stability.Final, null)]
    [TestCase("beta", Stability.Beta, null)]
    [TestCase("beta3", Stability.Beta, 3)]
    [TestCase("alpha", Stability.Alpha, null)]
    [TestCase("alpha4", Stability.Alpha, 4)]
    [TestCase("rc", Stability.ReleaseCandidate, null)]
    [TestCase("rc3", Stability.ReleaseCandidate, 3)]
    [TestCase("rc03", Stability.ReleaseCandidate, 3)]
    [TestCase("beta3f", null, null)]
    [TestCase("notAStability1", null, 1)]
    public void ValidateVersionParsing(string tag, Stability? stability, int? releaseNumber)
    {
        var releaseInfo = ReleaseInformationCalculator.Calculate(null, tag);

        Assert.AreEqual(stability, releaseInfo.Stability);
        Assert.AreEqual(releaseNumber, releaseInfo.ReleaseNumber);
    }

}