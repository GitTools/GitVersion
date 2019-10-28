using System;
using System.IO;
using GitVersion.Cache;
using GitVersion.Configuration;
using GitVersion.Configuration.Init.Wizard;
using GitVersion.Logging;
using GitVersion.OutputVariables;
using GitVersion.VersionCalculation;
using LibGit2Sharp;
using NUnit.Framework;
using Microsoft.Extensions.Options;
using GitVersion.MSBuildTask.Tests.Helpers;

namespace GitVersion.MSBuildTask.Tests
{
    [TestFixture]
    public class GitVersionTaskDirectoryTests : TestBase
    {
        private string gitDirectory;
        private string workDirectory;
        private ILog log;
        private IConfigFileLocator configFileLocator;
        private IGitVersionCache gitVersionCache;
        private IBuildServerResolver buildServerResolver;
        private IMetaDataCalculator metaDataCalculator;
        private IGitVersionFinder gitVersionFinder;
        private IFileSystem testFileSystem;
        private IConfigInitWizard configInitWizard;
        private IEnvironment environment;

        [SetUp]
        public void CreateTemporaryRepository()
        {
            workDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            gitDirectory = Repository.Init(workDirectory).TrimEnd(Path.DirectorySeparatorChar);

            testFileSystem = new TestFileSystem();
            log = new NullLog();
            environment = new TestEnvironment();
            var stepFactory = new ConfigInitStepFactory();
            configInitWizard = new ConfigInitWizard(new ConsoleAdapter(), stepFactory);
            configFileLocator = new DefaultConfigFileLocator(testFileSystem, log);
            gitVersionCache = new GitVersionCache(testFileSystem, log);

            buildServerResolver = new BuildServerResolver(null, log);

            metaDataCalculator = new MetaDataCalculator();
            var baseVersionCalculator = new BaseVersionCalculator(log, null);
            var mainlineVersionCalculator = new MainlineVersionCalculator(log, metaDataCalculator);
            var nextVersionCalculator = new NextVersionCalculator(log, metaDataCalculator, baseVersionCalculator, mainlineVersionCalculator);
            gitVersionFinder = new GitVersionFinder(log, nextVersionCalculator);
            
            Assert.NotNull(gitDirectory);
        }


        [TearDown]
        public void Cleanup()
        {
            Directory.Delete(workDirectory, true);
        }


        [Test]
        public void FindsGitDirectory()
        {
            try
            {
                var arguments = new Arguments { TargetPath = workDirectory, NoFetch = true };
                var options = Options.Create(arguments);

                var gitPreparer = new GitPreparer(log, new TestEnvironment(), options);
                var configurationProvider = new ConfigProvider(testFileSystem, log, configFileLocator, gitPreparer, configInitWizard);

                var baseVersionCalculator = new BaseVersionCalculator(log, null);
                var mainlineVersionCalculator = new MainlineVersionCalculator(log, metaDataCalculator);
                var nextVersionCalculator = new NextVersionCalculator(log, metaDataCalculator, baseVersionCalculator, mainlineVersionCalculator);
                var variableProvider = new VariableProvider(nextVersionCalculator, environment);

                var gitVersionCalculator = new GitVersionCalculator(testFileSystem, log, configFileLocator, configurationProvider, buildServerResolver, gitVersionCache, gitVersionFinder, gitPreparer, variableProvider, options);

                gitVersionCalculator.CalculateVersionVariables();
            }
            catch (Exception ex)
            {
                // `RepositoryNotFoundException` means that it couldn't find the .git directory,
                // any other exception means that the .git was found but there was some other issue that this test doesn't care about.
                Assert.IsNotAssignableFrom<RepositoryNotFoundException>(ex);
            }
        }


        [Test]
        public void FindsGitDirectoryInParent()
        {
            var childDir = Path.Combine(workDirectory, "child");
            Directory.CreateDirectory(childDir);

            try
            {
                var arguments = new Arguments { TargetPath = childDir, NoFetch = true };
                var options = Options.Create(arguments);

                var gitPreparer = new GitPreparer(log, environment, options);
                var configurationProvider = new ConfigProvider(testFileSystem, log, configFileLocator, gitPreparer, configInitWizard);
                var baseVersionCalculator = new BaseVersionCalculator(log, null);
                var mainlineVersionCalculator = new MainlineVersionCalculator(log, metaDataCalculator);
                var nextVersionCalculator = new NextVersionCalculator(log, metaDataCalculator, baseVersionCalculator, mainlineVersionCalculator);
                var variableProvider = new VariableProvider(nextVersionCalculator, environment);

                var gitVersionCalculator = new GitVersionCalculator(testFileSystem, log, configFileLocator, configurationProvider, buildServerResolver, gitVersionCache, gitVersionFinder, gitPreparer, variableProvider, options);

                gitVersionCalculator.CalculateVersionVariables();
            }
            catch (Exception ex)
            {
                // TODO I think this test is wrong.. It throws a different exception
                // `RepositoryNotFoundException` means that it couldn't find the .git directory,
                // any other exception means that the .git was found but there was some other issue that this test doesn't care about.
                Assert.IsNotAssignableFrom<RepositoryNotFoundException>(ex);
            }
        }
    }
}
