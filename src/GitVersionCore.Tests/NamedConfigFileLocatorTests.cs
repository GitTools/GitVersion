using System.IO;
using NUnit.Framework;
using Shouldly;
using GitVersion.Configuration;
using GitVersion.Exceptions;
using GitVersion;
using GitVersion.Configuration.Init.Wizard;
using GitVersion.Logging;
using Microsoft.Extensions.Options;

namespace GitVersionCore.Tests
{
    [TestFixture]
    public class NamedConfigFileLocatorTests : TestBase
    {
        private const string DefaultRepoPath = @"c:\MyGitRepo";
        private const string DefaultWorkingPath = @"c:\MyGitRepo\Working";

        private string repoPath;
        private string workingPath;
        private IFileSystem fileSystem;
        private NamedConfigFileLocator configFileLocator;
        private ILog log;
        private IConfigInitStepFactory stepFactory;
        private IOptions<Arguments> options;

        [SetUp]
        public void Setup()
        {
            fileSystem = new TestFileSystem();
            log = new NullLog();

            options = Options.Create(new Arguments { ConfigFile = "my-config.yaml" });
            configFileLocator = new NamedConfigFileLocator(fileSystem, log, options);
            repoPath = DefaultRepoPath;
            workingPath = DefaultWorkingPath;
            stepFactory = new ConfigInitStepFactory();

            ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestAttribute>();
        }

        [Test]
        public void ThrowsExceptionOnAmbiguousConfigFileLocation()
        {
            var repositoryConfigFilePath = SetupConfigFileContent(string.Empty, path: repoPath);
            var workingDirectoryConfigFilePath = SetupConfigFileContent(string.Empty, path: workingPath);

            var exception = Should.Throw<WarningException>(() => { configFileLocator.Verify(workingPath, repoPath); });

            var expectedMessage = $"Ambiguous config file selection from '{workingDirectoryConfigFilePath}' and '{repositoryConfigFilePath}'";
            exception.Message.ShouldBe(expectedMessage);
        }

        [Test]
        public void DoNotThrowWhenWorkingAndRepoPathsAreSame()
        {
            workingPath = DefaultRepoPath;
            SetupConfigFileContent(string.Empty, path: workingPath);

            Should.NotThrow(() => { configFileLocator.Verify(workingPath, repoPath); });
        }

        [Test]
        [Platform(Exclude = "Linux,Unix")]
        public void DoNotThrowWhenWorkingAndRepoPathsAreSame_WithDifferentCasing()
        {
            workingPath = DefaultRepoPath.ToLower();
            SetupConfigFileContent(string.Empty, path: workingPath);

            Should.NotThrow(() => { configFileLocator.Verify(workingPath, repoPath); });
        }

        [Test]
        public void DoNotThrowWhenConfigFileIsInSubDirectoryOfRepoPath()
        {
            workingPath = DefaultRepoPath;

            options = Options.Create(new Arguments { ConfigFile = "./src/my-config.yaml" });
            configFileLocator = new NamedConfigFileLocator(fileSystem, log, options);
            SetupConfigFileContent(string.Empty, path: workingPath);

            Should.NotThrow(() => { configFileLocator.Verify(workingPath, repoPath); });
        }

        [Test]
        public void NoWarnOnCustomYmlFile()
        {
            SetupConfigFileContent(string.Empty);

            var stringLogger = string.Empty;
            void Action(string info) => stringLogger = info;

            var logAppender = new TestLogAppender(Action);
            log = new Log(logAppender);

            configFileLocator = new NamedConfigFileLocator(fileSystem, log, options);

            var gitPreparer = new GitPreparer(log, new TestEnvironment(), Options.Create(new Arguments { TargetPath = repoPath }));
            var configInitWizard = new ConfigInitWizard(new ConsoleAdapter(), stepFactory);
            var configurationProvider = new ConfigProvider(fileSystem, log, configFileLocator, gitPreparer, configInitWizard);

            configurationProvider.Provide(repoPath);
            stringLogger.Length.ShouldBe(0);
        }

        [Test]
        public void NoWarnOnCustomYmlFileOutsideRepoPath()
        {
            SetupConfigFileContent(string.Empty, path: @"c:\\Unrelated\\path");

            var stringLogger = string.Empty;
            void Action(string info) => stringLogger = info;

            var logAppender = new TestLogAppender(Action);
            log = new Log(logAppender);

            configFileLocator = new NamedConfigFileLocator(fileSystem, log, options);
            var gitPreparer = new GitPreparer(log, new TestEnvironment(), Options.Create(new Arguments { TargetPath = repoPath }));
            var configInitWizard = new ConfigInitWizard(new ConsoleAdapter(), stepFactory);
            var configurationProvider = new ConfigProvider(fileSystem, log, configFileLocator, gitPreparer, configInitWizard);

            configurationProvider.Provide(repoPath);
            stringLogger.Length.ShouldBe(0);
        }

        private string SetupConfigFileContent(string text, string fileName = null, string path = null)
        {
            if (string.IsNullOrEmpty(fileName)) fileName = configFileLocator.FilePath;
            var filePath = fileName;
            if (!string.IsNullOrEmpty(path))
                filePath = Path.Combine(path, filePath);
            fileSystem.WriteAllText(filePath, text);
            return filePath;
        }
    }
}
