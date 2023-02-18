using System.Runtime.InteropServices;
using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Helpers;
using GitVersion.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GitVersion.Core.Tests.Init;

[TestFixture]
public class InitScenarios : TestBase
{
    [SetUp]
    public void Setup() => ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestAttribute>();

    [Test]
    public void CanSetNextVersion()
    {
        var workingDirectory = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "c:\\proj" : "/proj";
        var options = Options.Create(new GitVersionOptions { WorkingDirectory = workingDirectory });

        var sp = ConfigureServices(services =>
        {
            services.AddSingleton<IConsole>(new TestConsole("3", "2.0.0", "0"));
            services.AddSingleton(options);
        });

        var configurationProvider = sp.GetRequiredService<IConfigurationProvider>();
        var fileSystem = sp.GetRequiredService<IFileSystem>();
        configurationProvider.Init(workingDirectory);

        fileSystem.ReadAllText(PathHelper.Combine(workingDirectory, "GitVersion.yml")).ShouldMatchApproved();
    }
}
