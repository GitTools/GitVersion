using System.IO;
using System.Runtime.InteropServices;
using GitVersion.Configuration;
using GitVersion.Logging;
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
        [Category("NoMono")]
        [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
        public void CanSetNextVersion()
        {
            var log = new NullLog();
            var fileSystem = new TestFileSystem();
            var testConsole = new TestConsole("3", "2.0.0", "0");
            var configFileLocator = new DefaultConfigFileLocator(fileSystem, log);
            var workingDirectory = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "c:\\proj" : "/proj";
            ConfigurationProvider.Init(workingDirectory, fileSystem, testConsole, log, configFileLocator);

            fileSystem.ReadAllText(Path.Combine(workingDirectory, "GitVersion.yml")).ShouldMatchApproved();
        }
    }
}
