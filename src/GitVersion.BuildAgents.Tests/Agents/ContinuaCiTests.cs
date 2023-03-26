using GitVersion.Core.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Agents.Tests;

[TestFixture]
public class ContinuaCiTests : TestBase
{
    private IServiceProvider sp;

    [SetUp]
    public void SetUp() => this.sp = ConfigureServices(services => services.AddSingleton<ContinuaCi>());

    [Test]
    public void GenerateBuildVersion()
    {
        var buildServer = this.sp.GetRequiredService<ContinuaCi>();
        var vars = new TestableGitVersionVariables(fullSemVer: "0.0.0-Beta4.7");
        var continuaCiVersion = buildServer.GenerateSetVersionMessage(vars);
        Assert.That(continuaCiVersion, Is.EqualTo("@@continua[setBuildVersion value='0.0.0-Beta4.7']"));
    }
}
