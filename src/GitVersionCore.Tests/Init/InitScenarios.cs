using System.IO;
using System.Runtime.InteropServices;
using GitVersion;
using GitVersion.Configuration;
using GitVersion.Configuration.Init;
using GitVersion.Configuration.Init.Wizard;
using GitVersion.Extensions;
using GitVersion.Logging;
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
        [Category("NoMono")]
        [Description("Won't run on Mono due to source information not being available for ShouldMatchApproved.")]
        public void CanSetNextVersion()
        {
            ILog log = new NullLog();
            IFileSystem fileSystem = new TestFileSystem();
            IConsole testConsole = new TestConsole("3", "2.0.0", "0");

            var serviceCollections = new ServiceCollection();
            serviceCollections.AddModule(new GitVersionInitModule());

            serviceCollections.AddSingleton(log);
            serviceCollections.AddSingleton(fileSystem);
            serviceCollections.AddSingleton(testConsole);

            var serviceProvider = serviceCollections.BuildServiceProvider();

            var stepFactory = new ConfigInitStepFactory(serviceProvider);
            var configInitWizard = new ConfigInitWizard(testConsole, stepFactory);
            var configFileLocator = new DefaultConfigFileLocator(fileSystem, log);
            var workingDirectory = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "c:\\proj" : "/proj";

            var gitPreparer = new GitPreparer(log, new TestEnvironment(), Options.Create(new Arguments { TargetPath = workingDirectory }));
            var configurationProvider = new ConfigProvider(fileSystem, log, configFileLocator, gitPreparer, configInitWizard);

            configurationProvider.Init(workingDirectory);

            fileSystem.ReadAllText(Path.Combine(workingDirectory, "GitVersion.yml")).ShouldMatchApproved();
        }
    }
}
