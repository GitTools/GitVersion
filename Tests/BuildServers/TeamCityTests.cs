using GitFlowVersion;
using NUnit.Framework;

[TestFixture]
public class TeamCityTests
{
    [Test]
    public void Develop_branch()
    {
        var versionBuilder = new TeamCity();
        var tcVersion = versionBuilder.GenerateSetVersionMessage("0.0.0-Unstable4");
        Assert.AreEqual("##teamcity[buildNumber '0.0.0-Unstable4']", tcVersion);
    }

}