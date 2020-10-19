using System.IO;
using System.Runtime.InteropServices;
using GitVersion;
using GitVersion.Configuration;
using GitVersion.Logging;
using GitVersionCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace GitVersionCore.Tests.Init
{
    [TestFixture]
    public class InitScenarios : TestBase
    {
        [SetUp]
        public void Setup()
        {
            ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestAttribute>();
        }

        [Test]
        [Category(NoMono)]
        [Description(NoMonoDescription)]
        public void CanSetNextVersion()
        {
            var workingDirectory = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "c:\\proj" : "/proj";
            var options = Options.Create(new GitVersionOptions { WorkingDirectory = workingDirectory });

            var sp = ConfigureServices(services =>
            {
                services.AddSingleton<IConsole>(new TestConsole("3", "2.0.0", "0"));
                services.AddSingleton(options);
            });

            var configurationProvider = sp.GetService<IConfigProvider>();
            var fileSystem = sp.GetService<IFileSystem>();
            configurationProvider.Init(workingDirectory);

            fileSystem.ReadAllText(Path.Combine(workingDirectory, "GitVersion.yml")).ShouldMatchApproved();
        }
    }
}
