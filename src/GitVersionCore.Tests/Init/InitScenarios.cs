namespace GitVersionCore.Tests.Init
{
    using System.IO;
    using System.Runtime.InteropServices;
    using GitVersion;
    using NUnit.Framework;
    using Shouldly;

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
            var testFileSystem = new TestFileSystem();
            var testConsole = new TestConsole("3", "2.0.0", "0");
            var configFileLocator = new DefaultConfigFileLocator();
            var workingDirectory = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "c:\\proj" : "/proj";
            ConfigurationProvider.Init(workingDirectory, testFileSystem, testConsole, configFileLocator);

            testFileSystem.ReadAllText(Path.Combine(workingDirectory, "GitVersion.yml")).ShouldMatchApproved();
        }
    }
}
