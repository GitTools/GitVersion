using System.IO.Abstractions;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Helpers;

namespace GitVersion.Configuration.Tests;

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
            this.repoPath = FileSystemHelper.Path.Combine(FileSystemHelper.Path.GetTempPath(), "MyGitRepo");
            this.workingPath = FileSystemHelper.Path.Combine(FileSystemHelper.Path.GetTempPath(), "MyGitRepo", "Working");
            var options = Options.Create(new GitVersionOptions { WorkingDirectory = repoPath });

            var sp = ConfigureServices(services => services.AddSingleton(options));

            this.fileSystem = sp.GetRequiredService<IFileSystem>();
            this.configurationProvider = (ConfigurationProvider)sp.GetRequiredService<IConfigurationProvider>();
            this.configFileLocator = sp.GetRequiredService<IConfigurationFileLocator>();

            ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestAttribute>();
        }

        [TestCase(ConfigurationFileLocator.DefaultFileName, ConfigurationFileLocator.DefaultFileName)]
        [TestCase(ConfigurationFileLocator.DefaultFileName, ConfigurationFileLocator.DefaultAlternativeFileName)]
        [TestCase(ConfigurationFileLocator.DefaultFileName, ConfigurationFileLocator.DefaultFileNameDotted)]
        [TestCase(ConfigurationFileLocator.DefaultFileName, ConfigurationFileLocator.DefaultAlternativeFileNameDotted)]
        [TestCase(ConfigurationFileLocator.DefaultAlternativeFileName, ConfigurationFileLocator.DefaultFileName)]
        [TestCase(ConfigurationFileLocator.DefaultAlternativeFileName, ConfigurationFileLocator.DefaultAlternativeFileName)]
        [TestCase(ConfigurationFileLocator.DefaultAlternativeFileName, ConfigurationFileLocator.DefaultFileNameDotted)]
        [TestCase(ConfigurationFileLocator.DefaultAlternativeFileName, ConfigurationFileLocator.DefaultAlternativeFileNameDotted)]
        [TestCase(ConfigurationFileLocator.DefaultFileNameDotted, ConfigurationFileLocator.DefaultFileName)]
        [TestCase(ConfigurationFileLocator.DefaultFileNameDotted, ConfigurationFileLocator.DefaultAlternativeFileName)]
        [TestCase(ConfigurationFileLocator.DefaultFileNameDotted, ConfigurationFileLocator.DefaultFileNameDotted)]
        [TestCase(ConfigurationFileLocator.DefaultFileNameDotted, ConfigurationFileLocator.DefaultAlternativeFileNameDotted)]
        [TestCase(ConfigurationFileLocator.DefaultAlternativeFileNameDotted, ConfigurationFileLocator.DefaultFileName)]
        [TestCase(ConfigurationFileLocator.DefaultAlternativeFileNameDotted, ConfigurationFileLocator.DefaultAlternativeFileName)]
        [TestCase(ConfigurationFileLocator.DefaultAlternativeFileNameDotted, ConfigurationFileLocator.DefaultFileNameDotted)]
        [TestCase(ConfigurationFileLocator.DefaultAlternativeFileNameDotted, ConfigurationFileLocator.DefaultAlternativeFileNameDotted)]
        public void ThrowsExceptionOnAmbiguousConfigFileLocation(string repoConfigFile, string workingConfigFile)
        {
            using var repositoryConfigFilePath = this.fileSystem.SetupConfigFile(path: this.repoPath, fileName: repoConfigFile);
            using var workingDirectoryConfigFilePath = this.fileSystem.SetupConfigFile(path: this.workingPath, fileName: workingConfigFile);

            var exception = Should.Throw<WarningException>(() => this.configFileLocator.Verify(this.workingPath, this.repoPath));

            var expectedMessage = $"Ambiguous configuration file selection from '{workingDirectoryConfigFilePath.Value}' and '{repositoryConfigFilePath.Value}'";
            exception.Message.ShouldBe(expectedMessage);
        }

        [TestCase(ConfigurationFileLocator.DefaultFileName)]
        [TestCase(ConfigurationFileLocator.DefaultAlternativeFileName)]
        [TestCase(ConfigurationFileLocator.DefaultFileNameDotted)]
        [TestCase(ConfigurationFileLocator.DefaultAlternativeFileNameDotted)]
        public void NoWarnOnGitVersionYmlFile(string configurationFile)
        {
            using var _ = this.fileSystem.SetupConfigFile(path: this.repoPath, fileName: configurationFile);

            Should.NotThrow(() => this.configurationProvider.ProvideForDirectory(this.repoPath));
        }

        [TestCase(ConfigurationFileLocator.DefaultFileName)]
        [TestCase(ConfigurationFileLocator.DefaultAlternativeFileName)]
        [TestCase(ConfigurationFileLocator.DefaultFileNameDotted)]
        [TestCase(ConfigurationFileLocator.DefaultAlternativeFileNameDotted)]
        public void NoWarnOnLowercasedGitVersionYmlFile(string configurationFile)
        {
            var lowercasedConfigurationFile = configurationFile.ToLower();
            using var _ = this.fileSystem.SetupConfigFile(path: this.repoPath, fileName: lowercasedConfigurationFile);

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
        private string ConfigFile => this.gitVersionOptions.ConfigurationInfo.ConfigurationFile!;

        [SetUp]
        public void Setup()
        {
            this.gitVersionOptions = new() { ConfigurationInfo = { ConfigurationFile = "my-config.yaml" } };
            this.repoPath = FileSystemHelper.Path.Combine(FileSystemHelper.Path.GetTempPath(), "MyGitRepo");
            this.workingPath = FileSystemHelper.Path.Combine(FileSystemHelper.Path.GetTempPath(), "MyGitRepo", "Working");

            ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestAttribute>();
        }

        [Test]
        public void ThrowsExceptionOnAmbiguousConfigFileLocation()
        {
            var sp = GetServiceProvider(this.gitVersionOptions);
            this.configFileLocator = sp.GetRequiredService<IConfigurationFileLocator>();
            this.fileSystem = sp.GetRequiredService<IFileSystem>();

            using var repositoryConfigFilePath = this.fileSystem.SetupConfigFile(path: this.repoPath, fileName: ConfigFile);
            using var workDirConfigFilePath = this.fileSystem.SetupConfigFile(path: this.workingPath, fileName: ConfigFile);

            var exception = Should.Throw<WarningException>(() => this.configFileLocator.Verify(this.workingPath, this.repoPath));

            var expectedMessage = $"Ambiguous configuration file selection from '{workDirConfigFilePath.Value}' and '{repositoryConfigFilePath.Value}'";
            exception.Message.ShouldBe(expectedMessage);
        }

        [Test]
        public void DoNotThrowWhenWorkingAndRepoPathsAreSame()
        {
            this.workingPath = this.repoPath;

            var sp = GetServiceProvider(this.gitVersionOptions);
            this.configFileLocator = sp.GetRequiredService<IConfigurationFileLocator>();
            this.fileSystem = sp.GetRequiredService<IFileSystem>();

            using var _ = this.fileSystem.SetupConfigFile(path: this.workingPath, fileName: ConfigFile);

            Should.NotThrow(() => this.configFileLocator.Verify(this.workingPath, this.repoPath));
        }

        [Test]
        public void DoNotThrowWhenWorkingAndRepoPathsAreSame_WithDifferentCasing()
        {
            this.workingPath = this.repoPath.ToLower();

            var sp = GetServiceProvider(this.gitVersionOptions);
            this.configFileLocator = sp.GetRequiredService<IConfigurationFileLocator>();
            this.fileSystem = sp.GetRequiredService<IFileSystem>();

            using var _ = this.fileSystem.SetupConfigFile(path: this.workingPath, fileName: ConfigFile);

            Should.NotThrow(() => this.configFileLocator.Verify(this.workingPath, this.repoPath));
        }

        [Test]
        public void ReturnConfigurationFilePathIfCustomConfigurationIsSet()
        {
            this.workingPath = this.repoPath;
            var configurationFilePath = FileSystemHelper.Path.Combine(this.workingPath, "Configuration", "CustomConfig.yaml");

            this.gitVersionOptions = new() { ConfigurationInfo = { ConfigurationFile = configurationFilePath } };

            var serviceProvider = GetServiceProvider(this.gitVersionOptions);
            this.fileSystem = serviceProvider.GetRequiredService<IFileSystem>();

            using var _ = this.fileSystem.SetupConfigFile(
                path: FileSystemHelper.Path.Combine(this.workingPath, "Configuration"), fileName: "CustomConfig.yaml"
            );
            this.configFileLocator = serviceProvider.GetRequiredService<IConfigurationFileLocator>();

            var config = this.configFileLocator.GetConfigurationFile(this.workingPath);
            config.ShouldBe(configurationFilePath);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("Configuration/CustomConfig2.yaml")]
        public void ReturnConfigurationFilePathIfCustomConfigurationIsSet_InvalidConfigurationFilePaths(string? configFile)
        {
            this.workingPath = this.repoPath;

            this.gitVersionOptions = new() { ConfigurationInfo = { ConfigurationFile = configFile } };
            var sp = GetServiceProvider(this.gitVersionOptions);
            this.configFileLocator = sp.GetRequiredService<IConfigurationFileLocator>();

            var config = this.configFileLocator.GetConfigurationFile(this.workingPath);
            config.ShouldBe(null);
        }

        [Test]
        public void DoNotThrowWhenConfigFileIsInSubDirectoryOfRepoPath()
        {
            this.workingPath = this.repoPath;

            this.gitVersionOptions = new() { ConfigurationInfo = { ConfigurationFile = "./src/my-config.yaml" } };
            var sp = GetServiceProvider(this.gitVersionOptions);
            this.configFileLocator = sp.GetRequiredService<IConfigurationFileLocator>();
            this.fileSystem = sp.GetRequiredService<IFileSystem>();

            using var _ = this.fileSystem.SetupConfigFile(path: this.workingPath, fileName: ConfigFile);

            Should.NotThrow(() => this.configFileLocator.Verify(this.workingPath, this.repoPath));
        }

        [Test]
        public void NoWarnOnCustomYmlFile()
        {
            var stringLogger = string.Empty;

            var loggerFactory = new TestLoggerFactory(message => stringLogger = message);

            var sp = GetServiceProvider(this.gitVersionOptions, loggerFactory);
            this.configFileLocator = sp.GetRequiredService<IConfigurationFileLocator>();
            this.fileSystem = sp.GetRequiredService<IFileSystem>();

            using var _ = this.fileSystem.SetupConfigFile(path: null, fileName: ConfigFile);

            var configurationProvider = (ConfigurationProvider)sp.GetRequiredService<IConfigurationProvider>();

            configurationProvider.ProvideForDirectory(this.repoPath);
            stringLogger.ShouldMatch("No configuration file found, using default configuration");
        }

        [Test]
        public void NoWarnOnCustomYmlFileOutsideRepoPath()
        {
            var stringLogger = string.Empty;

            var loggerFactory = new TestLoggerFactory(message => stringLogger = message);

            var sp = GetServiceProvider(this.gitVersionOptions, loggerFactory);
            this.configFileLocator = sp.GetRequiredService<IConfigurationFileLocator>();
            this.fileSystem = sp.GetRequiredService<IFileSystem>();

            var path = FileSystemHelper.Path.Combine(FileSystemHelper.Path.GetTempPath(), "unrelatedPath");
            using var _ = this.fileSystem.SetupConfigFile(path: path, fileName: ConfigFile);

            var configurationProvider = (ConfigurationProvider)sp.GetRequiredService<IConfigurationProvider>();

            configurationProvider.ProvideForDirectory(this.repoPath);
            stringLogger.ShouldMatch("No configuration file found, using default configuration");
        }

        [Test]
        public void ThrowsExceptionOnCustomYmlFileDoesNotExist()
        {
            var sp = GetServiceProvider(this.gitVersionOptions);
            this.configFileLocator = sp.GetRequiredService<IConfigurationFileLocator>();

            var exception = Should.Throw<WarningException>(() => this.configFileLocator.Verify(this.workingPath, this.repoPath));

            var configurationFile = this.gitVersionOptions.ConfigurationInfo.ConfigurationFile;
            var workingPathFileConfig = FileSystemHelper.Path.Combine(this.workingPath, configurationFile);
            var repoPathFileConfig = FileSystemHelper.Path.Combine(this.repoPath, configurationFile);
            var expectedMessage = $"The configuration file was not found at '{workingPathFileConfig}' or '{repoPathFileConfig}'";
            exception.Message.ShouldBe(expectedMessage);
        }

        private static IServiceProvider GetServiceProvider(GitVersionOptions gitVersionOptions, TestLoggerFactory? loggerFactory = null) =>
            ConfigureServices(services =>
            {
                loggerFactory?.RegisterWith(services);
                services.AddSingleton(Options.Create(gitVersionOptions));
            });
    }
}
