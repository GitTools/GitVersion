using System.IO;
using NUnit.Framework;
using Shouldly;
using GitVersion.Configuration;
using GitVersion.Exceptions;
using GitVersion;
using GitVersion.Configuration.Init.Wizard;
using GitVersion.Logging;

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

        [SetUp]
        public void Setup()
        {
            fileSystem = new TestFileSystem();
            log = new NullLog();
            configFileLocator = new NamedConfigFileLocator("my-config.yaml", fileSystem, log);
            repoPath = DefaultRepoPath;
            workingPath = DefaultWorkingPath;

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
        public void NoWarnOnCustomYmlFile()
        {
            SetupConfigFileContent(string.Empty);

            var stringLogger = string.Empty;
            void Action(string info) => stringLogger = info;

            var logAppender = new TestLogAppender(Action);
            log = new Log(logAppender);

            configFileLocator = new NamedConfigFileLocator("my-config.yaml", fileSystem, log);

            var gitPreparer = new GitPreparer(log, new Arguments { TargetPath = repoPath });
            var configInitWizard = new ConfigInitWizard(new ConsoleAdapter(), fileSystem, log);
            var configurationProvider = new ConfigurationProvider(fileSystem, log, configFileLocator, gitPreparer, configInitWizard);

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

            configFileLocator = new NamedConfigFileLocator("my-config.yaml", fileSystem, log);
            var gitPreparer = new GitPreparer(log, new Arguments { TargetPath = repoPath });
            var configInitWizard = new ConfigInitWizard(new ConsoleAdapter(), fileSystem, log);
            var configurationProvider = new ConfigurationProvider(fileSystem, log, configFileLocator, gitPreparer, configInitWizard);

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
