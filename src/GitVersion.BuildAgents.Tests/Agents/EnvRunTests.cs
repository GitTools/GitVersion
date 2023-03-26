using GitVersion.Core.Tests.Helpers;
using GitVersion.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Agents.Tests;

[TestFixture]
public class EnvRunTests : TestBase
{
    private const string EnvVarName = "ENVRUN_DATABASE";
    private string mFilePath;
    private IEnvironment environment;
    private EnvRun buildServer;

    [SetUp]
    public void SetEnvironmentVariableForTest()
    {
        var sp = ConfigureServices(services => services.AddSingleton<EnvRun>());
        this.environment = sp.GetRequiredService<IEnvironment>();
        this.buildServer = sp.GetRequiredService<EnvRun>();

        // set environment variable and create an empty envrun file to indicate that EnvRun is running...
        this.mFilePath = PathHelper.Combine(Path.GetTempPath(), "envrun.db");
        this.environment.SetEnvironmentVariable(EnvVarName, this.mFilePath);
        File.OpenWrite(this.mFilePath).Dispose();
    }

    [TearDown]
    public void ClearEnvironmentVariableForTest()
    {
        this.environment.SetEnvironmentVariable(EnvVarName, null);
        File.Delete(this.mFilePath);
    }

    [Test]
    public void CanApplyToCurrentContext()
    {
        var applys = this.buildServer.CanApplyToCurrentContext();
        applys.ShouldBeTrue();
    }

    [Test]
    public void CanApplyToCurrentContextEnvironmentVariableNotSet()
    {
        this.environment.SetEnvironmentVariable(EnvVarName, null);
        var applys = this.buildServer.CanApplyToCurrentContext();
        applys.ShouldBeFalse();
    }

    [TestCase("1.2.3")]
    [TestCase("1.2.3-rc4")]
    public void GenerateSetVersionMessage(string fullSemVer)
    {
        var vars = new TestableGitVersionVariables(fullSemVer: fullSemVer);
        var version = this.buildServer.GenerateSetVersionMessage(vars);
        version.ShouldBe(fullSemVer);
    }

    [TestCase("Version", "1.2.3", "@@envrun[set name='GitVersion_Version' value='1.2.3']")]
    [TestCase("Version", "1.2.3-rc4", "@@envrun[set name='GitVersion_Version' value='1.2.3-rc4']")]
    public void GenerateSetParameterMessage(string name, string value, string expected)
    {
        var output = this.buildServer.GenerateSetParameterMessage(name, value);
        output.ShouldHaveSingleItem();
        output[0].ShouldBe(expected);
    }
}
