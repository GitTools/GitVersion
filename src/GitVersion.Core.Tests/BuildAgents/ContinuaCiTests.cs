using GitVersion.BuildAgents;
using GitVersion.Core.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace GitVersion.Core.Tests.BuildAgents;

[TestFixture]
public class ContinuaCiTests : TestBase
{
    private IServiceProvider sp;

    [SetUp]
    public void SetUp() => this.sp = ConfigureServices(services => services.AddSingleton<ContinuaCi>());

    [Test]
    public void GenerateBuildVersion()
    {
        var buildServer = this.sp.GetService<ContinuaCi>();
        var vars = new TestableVersionVariables(fullSemVer: "0.0.0-Beta4.7");
        var continuaCiVersion = buildServer.GenerateSetVersionMessage(vars);
        Assert.AreEqual("@@continua[setBuildVersion value='0.0.0-Beta4.7']", continuaCiVersion);
    }
}
