using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using GitVersion.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.Core.Tests;

[TestFixture]
public class ConfigFileLocatorTests
{
    public class DefaultConfigFileLocatorTests : TestBase
    {
        private const string DefaultRepoPath = @"c:\MyGitRepo";
        private const string DefaultWorkingPath = @"c:\MyGitRepo\Working";

        private string repoPath;
        private string workingPath;
        private IFileSystem fileSystem;
        private IConfigProvider configurationProvider;
        private IConfigFileLocator configFileLocator;

        [SetUp]
        public void Setup()
        {
            this.repoPath = DefaultRepoPath;
            this.workingPath = DefaultWorkingPath;
            var options = Options.Create(new GitVersionOptions { WorkingDirectory = repoPath });

            var sp = ConfigureServices(services => services.AddSingleton(options));

            this.fileSystem = sp.GetService<IFileSystem>();
            this.configurationProvider = sp.GetService<IConfigProvider>();
            this.configFileLocator = sp.GetService<IConfigFileLocator>();

            ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestAttribute>();
        }

        [TestCase(ConfigFileLocator.DefaultFileName, ConfigFileLocator.DefaultFileName)]
        public void ThrowsExceptionOnAmbiguousConfigFileLocation(string repoConfigFile, string workingConfigFile)
        {
            var repositoryConfigFilePath = SetupConfigFileContent(string.Empty, repoConfigFile, this.repoPath);
            var workingDirectoryConfigFilePath = SetupConfigFileContent(string.Empty, workingConfigFile, this.workingPath);

            var exception = Should.Throw<WarningException>(() => this.configFileLocator.Verify(this.workingPath, this.repoPath));

            var expecedMessage = $"Ambiguous config file selection from '{workingDirectoryConfigFilePath}' and '{repositoryConfigFilePath}'";
            exception.Message.ShouldBe(expecedMessage);
        }

        [Test]
        public void NoWarnOnGitVersionYmlFile()
        {
            SetupConfigFileContent(string.Empty, ConfigFileLocator.DefaultFileName, this.repoPath);

            Should.NotThrow(() => this.configurationProvider.Provide(this.repoPath));
        }

        [Test]
        public void NoWarnOnNoGitVersionYmlFile() => Should.NotThrow(() => this.configurationProvider.Provide(this.repoPath));

        private string SetupConfigFileContent(string text, string fileName, string path)
        {
            var fullPath = Path.Combine(path, fileName);
            this.fileSystem.WriteAllText(fullPath, text);

            return fullPath;
        }
    }

    public class NamedConfigFileLocatorTests : TestBase
    {
        private const string DefaultRepoPath = @"c:\MyGitRepo";
        private const string DefaultWorkingPath = @"c:\MyGitRepo\Working";

        private string repoPath;
        private string workingPath;
        private IFileSystem fileSystem;
        private IConfigFileLocator configFileLocator;
        private GitVersionOptions gitVersionOptions;

        [SetUp]
        public void Setup()
        {
            this.gitVersionOptions = new GitVersionOptions { ConfigInfo = { ConfigFile = "my-config.yaml" } };
            this.repoPath = DefaultRepoPath;
            this.workingPath = DefaultWorkingPath;

            ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestAttribute>();
        }

        [Test]
        public void ThrowsExceptionOnAmbiguousConfigFileLocation()
        {
            var sp = GetServiceProvider(this.gitVersionOptions);
            this.configFileLocator = sp.GetService<IConfigFileLocator>();
            this.fileSystem = sp.GetService<IFileSystem>();

            var repositoryConfigFilePath = SetupConfigFileContent(string.Empty, path: this.repoPath);
            var workingDirectoryConfigFilePath = SetupConfigFileContent(string.Empty, path: this.workingPath);

            var exception = Should.Throw<WarningException>(() => this.configFileLocator.Verify(this.workingPath, this.repoPath));

            var expectedMessage = $"Ambiguous config file selection from '{workingDirectoryConfigFilePath}' and '{repositoryConfigFilePath}'";
            exception.Message.ShouldBe(expectedMessage);
        }

        [Test]
        public void DoNotThrowWhenWorkingAndRepoPathsAreSame()
        {
            this.workingPath = DefaultRepoPath;

            var sp = GetServiceProvider(this.gitVersionOptions);
            this.configFileLocator = sp.GetService<IConfigFileLocator>();
            this.fileSystem = sp.GetService<IFileSystem>();

            SetupConfigFileContent(string.Empty, path: this.workingPath);

            Should.NotThrow(() => this.configFileLocator.Verify(this.workingPath, this.repoPath));
        }

        [Test]
        public void DoNotThrowWhenWorkingAndRepoPathsAreSame_WithDifferentCasing()
        {
            this.workingPath = DefaultRepoPath.ToLower();

            var sp = GetServiceProvider(this.gitVersionOptions);
            this.configFileLocator = sp.GetService<IConfigFileLocator>();
            this.fileSystem = sp.GetService<IFileSystem>();

            SetupConfigFileContent(string.Empty, path: this.workingPath);

            Should.NotThrow(() => this.configFileLocator.Verify(this.workingPath, this.repoPath));
        }

        [Test]
        public void DoNotThrowWhenConfigFileIsInSubDirectoryOfRepoPath()
        {
            this.workingPath = DefaultRepoPath;

            this.gitVersionOptions = new GitVersionOptions { ConfigInfo = { ConfigFile = "./src/my-config.yaml" } };
            var sp = GetServiceProvider(this.gitVersionOptions);
            this.configFileLocator = sp.GetService<IConfigFileLocator>();
            this.fileSystem = sp.GetService<IFileSystem>();

            SetupConfigFileContent(string.Empty, path: this.workingPath);

            Should.NotThrow(() => this.configFileLocator.Verify(this.workingPath, this.repoPath));
        }

        [Test]
        public void NoWarnOnCustomYmlFile()
        {
            var stringLogger = string.Empty;
            void Action(string info) => stringLogger = info;

            var logAppender = new TestLogAppender(Action);
            var log = new Log(logAppender);

            var sp = GetServiceProvider(this.gitVersionOptions, log);
            this.configFileLocator = sp.GetService<IConfigFileLocator>();
            this.fileSystem = sp.GetService<IFileSystem>();

            SetupConfigFileContent(string.Empty);

            var configurationProvider = sp.GetService<IConfigProvider>();

            configurationProvider.Provide(this.repoPath);
            stringLogger.Length.ShouldBe(0);
        }

        [Test]
        public void NoWarnOnCustomYmlFileOutsideRepoPath()
        {
            var stringLogger = string.Empty;
            void Action(string info) => stringLogger = info;

            var logAppender = new TestLogAppender(Action);
            var log = new Log(logAppender);

            var sp = GetServiceProvider(this.gitVersionOptions, log);
            this.configFileLocator = sp.GetService<IConfigFileLocator>();
            this.fileSystem = sp.GetService<IFileSystem>();

            SetupConfigFileContent(string.Empty, path: @"c:\\Unrelated\\path");

            var configurationProvider = sp.GetService<IConfigProvider>();

            configurationProvider.Provide(this.repoPath);
            stringLogger.Length.ShouldBe(0);
        }

        [Test]
        public void ThrowsExceptionOnCustomYmlFileDoesNotExist()
        {
            var sp = GetServiceProvider(this.gitVersionOptions);
            this.configFileLocator = sp.GetService<IConfigFileLocator>();

            var exception = Should.Throw<WarningException>(() => this.configFileLocator.Verify(this.workingPath, this.repoPath));

            var workingPathFileConfig = Path.Combine(this.workingPath, this.gitVersionOptions.ConfigInfo.ConfigFile);
            var repoPathFileConfig = Path.Combine(this.repoPath, this.gitVersionOptions.ConfigInfo.ConfigFile);
            var expectedMessage = $"The configuration file was not found at '{workingPathFileConfig}' or '{repoPathFileConfig}'";
            exception.Message.ShouldBe(expectedMessage);
        }

        private string SetupConfigFileContent(string text, string fileName = null, string path = null)
        {
            if (fileName.IsNullOrEmpty()) fileName = this.configFileLocator.FilePath;
            var filePath = fileName;
            if (!path.IsNullOrEmpty())
                filePath = Path.Combine(path, filePath);
            this.fileSystem.WriteAllText(filePath, text);
            return filePath;
        }

        private static IServiceProvider GetServiceProvider(GitVersionOptions gitVersionOptions, ILog log = null) =>
            ConfigureServices(services =>
            {
                if (log != null) services.AddSingleton(log);
                services.AddSingleton(Options.Create(gitVersionOptions));
            });
    }
}
