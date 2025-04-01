using GitVersion.Core.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Agents.Tests;

[TestFixture]
public class TeamCityTests : TestBase
{
    private TeamCity buildServer;

    [SetUp]
    public void SetUp()
    {
        var sp = ConfigureServices(services => services.AddSingleton<TeamCity>());
        this.buildServer = sp.GetRequiredService<TeamCity>();
    }

    [Test]
    public void ShouldSetBuildNumber()
    {
        var vars = new TestableGitVersionVariables { FullSemVer = "0.0.0-Unstable4" };
        var tcVersion = this.buildServer.SetBuildNumber(vars);
        Assert.That(tcVersion, Is.EqualTo("##teamcity[buildNumber '0.0.0-Unstable4']"));
    }

    [Test]
    public void ShouldSetOutputVariables()
    {
        var tcVersion = this.buildServer.SetOutputVariables("Foo", "0.8.0-unstable568 Branch:'develop' Sha:'ee69bff1087ebc95c6b43aa2124bd58f5722e0cb'");
        Assert.Multiple(() =>
        {
            Assert.That(tcVersion[0], Is.EqualTo("##teamcity[setParameter name='GitVersion.Foo' value='0.8.0-unstable568 Branch:|'develop|' Sha:|'ee69bff1087ebc95c6b43aa2124bd58f5722e0cb|'']"));
            Assert.That(tcVersion[1], Is.EqualTo("##teamcity[setParameter name='system.GitVersion.Foo' value='0.8.0-unstable568 Branch:|'develop|' Sha:|'ee69bff1087ebc95c6b43aa2124bd58f5722e0cb|'']"));
        });
    }
}
