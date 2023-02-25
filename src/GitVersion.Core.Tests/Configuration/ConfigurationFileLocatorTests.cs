using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GitVersion.Core.Tests;

[TestFixture]
public class ConfigurationFileLocatorTests
{
    public class DefaultConfigFileLocatorTests : TestBase
    {
        private const string DefaultRepoPath = @"c:\MyGitRepo";
        private const string DefaultWorkingPath = @"c:\MyGitRepo\Working";

        private string repoPath;
        private string workingPath;
        private IFileSystem fileSystem;
        private ConfigurationProvider configurationProvider;
        private IConfigurationFileLocator configFileLocator;

        [SetUp]
        public void Setup()
        {
            this.repoPath = DefaultRepoPath;
            this.workingPath = DefaultWorkingPath;
            var options = Options.Create(new GitVersionOptions { WorkingDirectory = repoPath });

            var sp = ConfigureServices(services => services.AddSingleton(options));

            this.fileSystem = sp.GetRequiredService<IFileSystem>();
            this.configurationProvider = (ConfigurationProvider)sp.GetRequiredService<IConfigurationProvider>();
            this.configFileLocator = sp.GetRequiredService<IConfigurationFileLocator>();

            ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestAttribute>();
        }

        [TestCase(ConfigurationFileLocator.DefaultFileName, ConfigurationFileLocator.DefaultFileName)]
        public void ThrowsExceptionOnAmbiguousConfigFileLocation(string repoConfigFile, string workingConfigFile)
        {
            var repositoryConfigFilePath = SetupConfigFileContent(string.Empty, repoConfigFile, this.repoPath);
            var workingDirectoryConfigFilePath = SetupConfigFileContent(string.Empty, workingConfigFile, this.workingPath);

            var exception = Should.Throw<WarningException>(() => this.configFileLocator.Verify(this.workingPath, this.repoPath));

            var expectedMessage = $"Ambiguous configuration file selection from '{workingDirectoryConfigFilePath}' and '{repositoryConfigFilePath}'";
            exception.Message.ShouldBe(expectedMessage);
        }

        [Test]
        public void NoWarnOnGitVersionYmlFile()
        {
            SetupConfigFileContent(string.Empty, ConfigurationFileLocator.DefaultFileName, this.repoPath);

            Should.NotThrow(() => this.configurationProvider.ProvideInternal(this.repoPath));
        }

        [Test]
        public void NoWarnOnNoGitVersionYmlFile() => Should.NotThrow(() => this.configurationProvider.ProvideInternal(this.repoPath));

        private string SetupConfigFileContent(string text, string fileName, string path)
        {
            var fullPath = PathHelper.Combine(path, fileName);
            this.fileSystem.WriteAllText(fullPath, text);

            return fullPath;
        }
    }

    public class NamedConfigurationFileLocatorTests : TestBase
    {
        private const string DefaultRepoPath = @"c:\MyGitRepo";
        private const string DefaultWorkingPath = @"c:\MyGitRepo\Working";

        private string repoPath;
        private string workingPath;
        private IFileSystem fileSystem;
        private IConfigurationFileLocator configFileLocator;
        private GitVersionOptions gitVersionOptions;

        [SetUp]
        public void Setup()
        {
            this.gitVersionOptions = new GitVersionOptions { ConfigurationInfo = { ConfigurationFile = "my-config.yaml" } };
            this.repoPath = DefaultRepoPath;
            this.workingPath = DefaultWorkingPath;

            ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestAttribute>();
        }

        [Test]
        public void ThrowsExceptionOnAmbiguousConfigFileLocation()
        {
            var sp = GetServiceProvider(this.gitVersionOptions);
            this.configFileLocator = sp.GetRequiredService<IConfigurationFileLocator>();
            this.fileSystem = sp.GetRequiredService<IFileSystem>();

            var repositoryConfigFilePath = SetupConfigFileContent(string.Empty, path: this.repoPath);
            var workingDirectoryConfigFilePath = SetupConfigFileContent(string.Empty, path: this.workingPath);

            var exception = Should.Throw<WarningException>(() => this.configFileLocator.Verify(this.workingPath, this.repoPath));

            var expectedMessage = $"Ambiguous configuration file selection from '{workingDirectoryConfigFilePath}' and '{repositoryConfigFilePath}'";
            exception.Message.ShouldBe(expectedMessage);
        }

        [Test]
        public void DoNotThrowWhenWorkingAndRepoPathsAreSame()
        {
            this.workingPath = DefaultRepoPath;

            var sp = GetServiceProvider(this.gitVersionOptions);
            this.configFileLocator = sp.GetRequiredService<IConfigurationFileLocator>();
            this.fileSystem = sp.GetRequiredService<IFileSystem>();

            SetupConfigFileContent(string.Empty, path: this.workingPath);

            Should.NotThrow(() => this.configFileLocator.Verify(this.workingPath, this.repoPath));
        }

        [Test]
        public void DoNotThrowWhenWorkingAndRepoPathsAreSame_WithDifferentCasing()
        {
            this.workingPath = DefaultRepoPath.ToLower();

            var sp = GetServiceProvider(this.gitVersionOptions);
            this.configFileLocator = sp.GetRequiredService<IConfigurationFileLocator>();
            this.fileSystem = sp.GetRequiredService<IFileSystem>();

            SetupConfigFileContent(string.Empty, path: this.workingPath);

            Should.NotThrow(() => this.configFileLocator.Verify(this.workingPath, this.repoPath));
        }

        [Test]
        public void DoNotThrowWhenConfigFileIsInSubDirectoryOfRepoPath()
        {
            this.workingPath = DefaultRepoPath;

            this.gitVersionOptions = new GitVersionOptions { ConfigurationInfo = { ConfigurationFile = "./src/my-config.yaml" } };
            var sp = GetServiceProvider(this.gitVersionOptions);
            this.configFileLocator = sp.GetRequiredService<IConfigurationFileLocator>();
            this.fileSystem = sp.GetRequiredService<IFileSystem>();

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
            this.configFileLocator = sp.GetRequiredService<IConfigurationFileLocator>();
            this.fileSystem = sp.GetRequiredService<IFileSystem>();

            SetupConfigFileContent(string.Empty);

            var configurationProvider = (ConfigurationProvider)sp.GetRequiredService<IConfigurationProvider>();

            configurationProvider.ProvideInternal(this.repoPath);
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
            this.configFileLocator = sp.GetRequiredService<IConfigurationFileLocator>();
            this.fileSystem = sp.GetRequiredService<IFileSystem>();

            SetupConfigFileContent(string.Empty, path: @"c:\\Unrelated\\path");

            var configurationProvider = (ConfigurationProvider)sp.GetRequiredService<IConfigurationProvider>();

            configurationProvider.ProvideInternal(this.repoPath);
            stringLogger.Length.ShouldBe(0);
        }

        [Test]
        public void ThrowsExceptionOnCustomYmlFileDoesNotExist()
        {
            var sp = GetServiceProvider(this.gitVersionOptions);
            this.configFileLocator = sp.GetRequiredService<IConfigurationFileLocator>();

            var exception = Should.Throw<WarningException>(() => this.configFileLocator.Verify(this.workingPath, this.repoPath));

            var workingPathFileConfig = PathHelper.Combine(this.workingPath, this.gitVersionOptions.ConfigurationInfo.ConfigurationFile);
            var repoPathFileConfig = PathHelper.Combine(this.repoPath, this.gitVersionOptions.ConfigurationInfo.ConfigurationFile);
            var expectedMessage = $"The configuration file was not found at '{workingPathFileConfig}' or '{repoPathFileConfig}'";
            exception.Message.ShouldBe(expectedMessage);
        }

        private string SetupConfigFileContent(string text, string? fileName = null, string? path = null)
        {
            if (fileName.IsNullOrEmpty()) fileName = this.configFileLocator.FilePath;
            var filePath = fileName;
            if (!path.IsNullOrEmpty())
                filePath = PathHelper.Combine(path, filePath);
            this.fileSystem.WriteAllText(filePath, text);
            return filePath;
        }

        private static IServiceProvider GetServiceProvider(GitVersionOptions gitVersionOptions, ILog? log = null) =>
            ConfigureServices(services =>
            {
                if (log != null) services.AddSingleton(log);
                services.AddSingleton(Options.Create(gitVersionOptions));
            });
    }
}
