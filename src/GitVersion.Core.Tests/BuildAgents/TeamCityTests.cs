using GitVersion.BuildAgents;
using GitVersion.Core.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace GitVersion.Core.Tests.BuildAgents;

[TestFixture]
public class TeamCityTests : TestBase
{
    private TeamCity buildServer;

    [SetUp]
    public void SetUp()
    {
        var sp = ConfigureServices(services => services.AddSingleton<TeamCity>());
        this.buildServer = sp.GetService<TeamCity>();
    }

    [Test]
    public void DevelopBranch()
    {
        var vars = new TestableVersionVariables(fullSemVer: "0.0.0-Unstable4");
        var tcVersion = this.buildServer.GenerateSetVersionMessage(vars);
        Assert.AreEqual("##teamcity[buildNumber '0.0.0-Unstable4']", tcVersion);
    }

    [Test]
    public void EscapeValues()
    {
        var tcVersion = this.buildServer.GenerateSetParameterMessage("Foo", "0.8.0-unstable568 Branch:'develop' Sha:'ee69bff1087ebc95c6b43aa2124bd58f5722e0cb'");
        Assert.AreEqual("##teamcity[setParameter name='GitVersion.Foo' value='0.8.0-unstable568 Branch:|'develop|' Sha:|'ee69bff1087ebc95c6b43aa2124bd58f5722e0cb|'']", tcVersion[0]);
        Assert.AreEqual("##teamcity[setParameter name='system.GitVersion.Foo' value='0.8.0-unstable568 Branch:|'develop|' Sha:|'ee69bff1087ebc95c6b43aa2124bd58f5722e0cb|'']", tcVersion[1]);
    }
}
