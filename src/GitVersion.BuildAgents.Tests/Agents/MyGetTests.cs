using GitVersion.Core.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Agents.Tests;

[TestFixture]
public class MyGetTests : TestBase
{
    private MyGet buildServer;

    [SetUp]
    public void SetUp()
    {
        var sp = ConfigureServices(services => services.AddSingleton<MyGet>());
        this.buildServer = sp.GetRequiredService<MyGet>();
    }

    [Test]
    public void DevelopBranch()
    {
        var vars = new TestableVersionVariables(fullSemVer: "0.0.0-Unstable4");
        var message = this.buildServer.GenerateSetVersionMessage(vars);
        Assert.That(message, Is.EqualTo(null));
    }

    [Test]
    public void EscapeValues()
    {
        var message = this.buildServer.GenerateSetParameterMessage("Foo", "0.8.0-unstable568 Branch:'develop' Sha:'ee69bff1087ebc95c6b43aa2124bd58f5722e0cb'");
        Assert.That(message[0], Is.EqualTo("##myget[setParameter name='GitVersion.Foo' value='0.8.0-unstable568 Branch:|'develop|' Sha:|'ee69bff1087ebc95c6b43aa2124bd58f5722e0cb|'']"));
    }

    [Test]
    public void BuildNumber()
    {
        var message = this.buildServer.GenerateSetParameterMessage("SemVer", "0.8.0-unstable568");
        Assert.That(message[1], Is.EqualTo("##myget[buildNumber '0.8.0-unstable568']"));
    }
}
