using System.IO;
using GitVersion;
using GitVersion.Cache;
using GitVersion.Configuration;
using GitVersion.Configuration.Init.Wizard;
using NUnit.Framework;
using GitVersion.Logging;
using GitVersion.OutputVariables;
using GitVersion.VersionCalculation;
using GitVersionCore.Tests.VersionCalculation;
using Microsoft.Extensions.Options;

namespace GitVersionCore.Tests
{
    [TestFixture]
    public class DynamicRepositoryTests : TestBase
    {
        private string workDirectory;

        private void ClearReadOnly(DirectoryInfo parentDirectory)
        {
            if (parentDirectory == null) return;
            parentDirectory.Attributes = FileAttributes.Normal;
            foreach (var fi in parentDirectory.GetFiles())
            {
                fi.Attributes = FileAttributes.Normal;
            }
            foreach (var di in parentDirectory.GetDirectories())
            {
                ClearReadOnly(di);
            }
        }

        [OneTimeSetUp]
        public void CreateTemporaryRepository()
        {
            // Note: we can't use guid because paths will be too long
            workDirectory = Path.Combine(Path.GetTempPath(), "GV");

            // Clean directory upfront, some build agents are having troubles
            if (Directory.Exists(workDirectory))
            {
                var di = new DirectoryInfo(workDirectory);
                ClearReadOnly(di);

                Directory.Delete(workDirectory, true);
            }

            Directory.CreateDirectory(workDirectory);
        }


        [OneTimeTearDown]
        public void Cleanup()
        {

        }

        // Note: use same name twice to see if changing commits works on same (cached) repository
        [NonParallelizable]
        [TestCase("GV_master", "https://github.com/GitTools/GitVersion", "master", "4783d325521463cd6cf1b61074352da84451f25d", "4.0.0+1086")]
        [TestCase("GV_master", "https://github.com/GitTools/GitVersion", "master", "3bdcd899530b4e9b37d13639f317da04a749e728", "4.0.0+1092")]
        [TestCase("Ctl_develop", "https://github.com/Catel/Catel", "develop", "0e2b6c125a730d2fa5e24394ef64abe62c98e9e9", "5.12.0-alpha.188")]
        [TestCase("Ctl_develop", "https://github.com/Catel/Catel", "develop", "71e71359f37581784e18c94e7a45eee72cbeeb30", "5.12.0-alpha.192")]
        [TestCase("Ctl_master", "https://github.com/Catel/Catel", "master", "f5de8964c35180a5f8607f5954007d5828aa849f", "5.10.0")]
        public void FindsVersionInDynamicRepo(string name, string url, string targetBranch, string commitId, string expectedFullSemVer)
        {
            var root = Path.Combine(workDirectory, name);
            var dynamicDirectory = Path.Combine(root, "D"); // dynamic, keeping directory as short as possible
            var workingDirectory = Path.Combine(root, "W"); // working, keeping directory as short as possible
            var arguments = new Arguments
            {
                TargetUrl = url,
                DynamicRepositoryLocation = dynamicDirectory,
                TargetBranch = targetBranch,
                NoFetch = false,
                TargetPath = workingDirectory,
                CommitId = commitId
            };
            var options = Options.Create(arguments);

            Directory.CreateDirectory(dynamicDirectory);
            Directory.CreateDirectory(workingDirectory);

            var testFileSystem = new TestFileSystem();
            var log = new NullLog();
            var configFileLocator = new DefaultConfigFileLocator(testFileSystem, log);
            var gitVersionCache = new GitVersionCache(testFileSystem, log);
            var buildServerResolver = new BuildServerResolver(null, log);

            var metadataCalculator = new MetaDataCalculator();
            var baseVersionCalculator = new TestBaseVersionStrategiesCalculator(log);
            var mainlineVersionCalculator = new MainlineVersionCalculator(log, metadataCalculator);
            var nextVersionCalculator = new NextVersionCalculator(log, metadataCalculator, baseVersionCalculator, mainlineVersionCalculator);
            var gitVersionFinder = new GitVersionFinder(log, nextVersionCalculator);

            var gitPreparer = new GitPreparer(log, arguments);
            var stepFactory = new ConfigInitStepFactory();
            var configInitWizard = new ConfigInitWizard(new ConsoleAdapter(), stepFactory);

            var configurationProvider = new ConfigurationProvider(testFileSystem, log, configFileLocator, gitPreparer, configInitWizard);
            
            var variableProvider = new VariableProvider(nextVersionCalculator);
            var gitVersionCalculator = new GitVersionCalculator(testFileSystem, log, configFileLocator, configurationProvider, buildServerResolver, gitVersionCache, gitVersionFinder, gitPreparer, variableProvider, options);

            var versionVariables = gitVersionCalculator.CalculateVersionVariables();

            Assert.AreEqual(expectedFullSemVer, versionVariables.FullSemVer);
        }
    }
}
