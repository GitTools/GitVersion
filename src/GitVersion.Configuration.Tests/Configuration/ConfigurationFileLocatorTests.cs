using GitVersion.Configuration;
using GitVersion.Configuration.Tests.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Helpers;
using GitVersion.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GitVersion.Core.Tests;

[TestFixture]
public static class ConfigurationFileLocatorTests
{
    public class DefaultConfigFileLocatorTests : TestBase
    {
        private string repoPath;
        private string workingPath;
        private IFileSystem fileSystem;
        private ConfigurationProvider configurationProvider;
        private IConfigurationFileLocator configFileLocator;

        [SetUp]
        public void Setup()
        {
            this.repoPath = PathHelper.Combine(PathHelper.GetTempPath(), "MyGitRepo");
            this.workingPath = PathHelper.Combine(PathHelper.GetTempPath(), "MyGitRepo", "Working");
            var options = Options.Create(new GitVersionOptions { WorkingDirectory = repoPath });

            var sp = ConfigureServices(services => services.AddSingleton(options));

            this.fileSystem = sp.GetRequiredService<IFileSystem>();
            this.configurationProvider = (ConfigurationProvider)sp.GetRequiredService<IConfigurationProvider>();
            this.configFileLocator = sp.GetRequiredService<IConfigurationFileLocator>();

            ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestAttribute>();
        }

        [TestCase(ConfigurationFileLocator.DefaultFileName, ConfigurationFileLocator.DefaultFileName)]
        [TestCase(ConfigurationFileLocator.DefaultFileName, ConfigurationFileLocator.DefaultAlternativeFileName)]
        [TestCase(ConfigurationFileLocator.DefaultAlternativeFileName, ConfigurationFileLocator.DefaultFileName)]
        [TestCase(ConfigurationFileLocator.DefaultAlternativeFileName, ConfigurationFileLocator.DefaultAlternativeFileName)]
        public void ThrowsExceptionOnAmbiguousConfigFileLocation(string repoConfigFile, string workingConfigFile)
        {
            var repositoryConfigFilePath = fileSystem.SetupConfigFile(string.Empty, this.repoPath, repoConfigFile);
            var workingDirectoryConfigFilePath = fileSystem.SetupConfigFile(string.Empty, this.workingPath, workingConfigFile);

            var exception = Should.Throw<WarningException>(() => this.configFileLocator.Verify(this.workingPath, this.repoPath));

            var expectedMessage = $"Ambiguous configuration file selection from '{workingDirectoryConfigFilePath}' and '{repositoryConfigFilePath}'";
            exception.Message.ShouldBe(expectedMessage);
        }

        [TestCase(ConfigurationFileLocator.DefaultFileName)]
        [TestCase(ConfigurationFileLocator.DefaultAlternativeFileName)]
        public void NoWarnOnGitVersionYmlFile(string configurationFile)
        {
            fileSystem.SetupConfigFile(string.Empty, this.repoPath, configurationFile);

            Should.NotThrow(() => this.configurationProvider.ProvideForDirectory(this.repoPath));
        }

        [Test]
        public void NoWarnOnNoGitVersionYmlFile() => Should.NotThrow(() => this.configurationProvider.ProvideForDirectory(this.repoPath));
    }

    public class NamedConfigurationFileLocatorTests : TestBase
    {
        private string repoPath;
        private string workingPath;
        private IFileSystem fileSystem;
        private IConfigurationFileLocator configFileLocator;
        private GitVersionOptions gitVersionOptions;

        [SetUp]
        public void Setup()
        {
            this.gitVersionOptions = new() { ConfigurationInfo = { ConfigurationFile = "my-config.yaml" } };
            this.repoPath = PathHelper.Combine(PathHelper.GetTempPath(), "MyGitRepo");
            this.workingPath = PathHelper.Combine(PathHelper.GetTempPath(), "MyGitRepo", "Working");

            ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestAttribute>();
        }

        [Test]
        public void ThrowsExceptionOnAmbiguousConfigFileLocation()
        {
            var sp = GetServiceProvider(this.gitVersionOptions);
            this.configFileLocator = sp.GetRequiredService<IConfigurationFileLocator>();
            this.fileSystem = sp.GetRequiredService<IFileSystem>();

            var repositoryConfigFilePath = SetupConfigFile(string.Empty, path: this.repoPath);
            var workDirConfigFilePath = SetupConfigFile(string.Empty, path: this.workingPath);

            var exception = Should.Throw<WarningException>(() => this.configFileLocator.Verify(this.workingPath, this.repoPath));

            var expectedMessage = $"Ambiguous configuration file selection from '{workDirConfigFilePath}' and '{repositoryConfigFilePath}'";
            exception.Message.ShouldBe(expectedMessage);
        }

        [Test]
        public void DoNotThrowWhenWorkingAndRepoPathsAreSame()
        {
            this.workingPath = this.repoPath;

            var sp = GetServiceProvider(this.gitVersionOptions);
            this.configFileLocator = sp.GetRequiredService<IConfigurationFileLocator>();
            this.fileSystem = sp.GetRequiredService<IFileSystem>();

            SetupConfigFile(string.Empty, path: this.workingPath);

            Should.NotThrow(() => this.configFileLocator.Verify(this.workingPath, this.repoPath));
        }

        [Test]
        public void DoNotThrowWhenWorkingAndRepoPathsAreSame_WithDifferentCasing()
        {
            this.workingPath = this.repoPath.ToLower();

            var sp = GetServiceProvider(this.gitVersionOptions);
            this.configFileLocator = sp.GetRequiredService<IConfigurationFileLocator>();
            this.fileSystem = sp.GetRequiredService<IFileSystem>();

            SetupConfigFile(string.Empty, path: this.workingPath);

            Should.NotThrow(() => this.configFileLocator.Verify(this.workingPath, this.repoPath));
        }

        [Test]
        public void DoNotThrowWhenConfigFileIsInSubDirectoryOfRepoPath()
        {
            this.workingPath = this.repoPath;

            this.gitVersionOptions = new() { ConfigurationInfo = { ConfigurationFile = "./src/my-config.yaml" } };
            var sp = GetServiceProvider(this.gitVersionOptions);
            this.configFileLocator = sp.GetRequiredService<IConfigurationFileLocator>();
            this.fileSystem = sp.GetRequiredService<IFileSystem>();

            SetupConfigFile(string.Empty, path: this.workingPath);

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

            SetupConfigFile(string.Empty);

            var configurationProvider = (ConfigurationProvider)sp.GetRequiredService<IConfigurationProvider>();

            configurationProvider.ProvideForDirectory(this.repoPath);
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

            SetupConfigFile(string.Empty, path: PathHelper.Combine(PathHelper.GetTempPath(), "unrelatedPath"));

            var configurationProvider = (ConfigurationProvider)sp.GetRequiredService<IConfigurationProvider>();

            configurationProvider.ProvideForDirectory(this.repoPath);
            stringLogger.Length.ShouldBe(0);
        }

        [Test]
        public void ThrowsExceptionOnCustomYmlFileDoesNotExist()
        {
            var sp = GetServiceProvider(this.gitVersionOptions);
            this.configFileLocator = sp.GetRequiredService<IConfigurationFileLocator>();

            var exception = Should.Throw<WarningException>(() => this.configFileLocator.Verify(this.workingPath, this.repoPath));

            var configurationFile = this.gitVersionOptions.ConfigurationInfo.ConfigurationFile;
            var workingPathFileConfig = PathHelper.Combine(this.workingPath, configurationFile);
            var repoPathFileConfig = PathHelper.Combine(this.repoPath, configurationFile);
            var expectedMessage = $"The configuration file was not found at '{workingPathFileConfig}' or '{repoPathFileConfig}'";
            exception.Message.ShouldBe(expectedMessage);
        }

        private string SetupConfigFile(string text, string? path = null)
            => this.fileSystem.SetupConfigFile(text, path, this.gitVersionOptions.ConfigurationInfo.ConfigurationFile!);

        private static IServiceProvider GetServiceProvider(GitVersionOptions gitVersionOptions, ILog? log = null) =>
            ConfigureServices(services =>
            {
                if (log != null) services.AddSingleton(log);
                services.AddSingleton(Options.Create(gitVersionOptions));
            });
    }
}
