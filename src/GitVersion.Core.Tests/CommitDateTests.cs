using GitVersion.Core.Tests.Helpers;

namespace GitVersion.Core.Tests;

[TestFixture]
public class CommitDateTests : TestBase
{
    [Test]
    [TestCase("yyyy-MM-dd", "2017-10-06")]
    [TestCase("dd.MM.yyyy", "06.10.2017")]
    [TestCase("yyyyMMdd", "20171006")]
    [TestCase("yyyy-MM", "2017-10")]
    public void CommitDateFormatTest(string format, string expectedOutcome)
    {
        var date = new DateTime(2017, 10, 6);
        var semanticVersionBuildMetaData = new SemanticVersionBuildMetaData(
            "950d2f830f5a2af12a6779a48d20dcbb02351f25",
            0,
            MainBranch,
            "3139d4eeb044f46057693473eacc2655b3b27e7d",
            "3139d4eeb",
            new DateTimeOffset(date, TimeSpan.Zero),
            0);
        var semanticVersion = new SemanticVersion
        {
            BuildMetaData = semanticVersionBuildMetaData // assume time zone is UTC
        };
        var configuration = new TestEffectiveConfiguration(commitDateFormat: format);
        var formatValues = new SemanticVersionFormatValues(semanticVersion, configuration);

        Assert.That(formatValues.CommitDate, Is.EqualTo(expectedOutcome));
    }
}
