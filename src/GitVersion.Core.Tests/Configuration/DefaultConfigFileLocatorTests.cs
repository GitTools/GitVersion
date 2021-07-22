using System;
using System.IO;
using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using GitVersion.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.Core.Tests
{
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
                repoPath = DefaultRepoPath;
                workingPath = DefaultWorkingPath;
                var options = Options.Create(new GitVersionOptions { WorkingDirectory = repoPath });

                var sp = ConfigureServices(services =>
                {
                    services.AddSingleton(options);
                });

                fileSystem = sp.GetService<IFileSystem>();
                configurationProvider = sp.GetService<IConfigProvider>();
                configFileLocator = sp.GetService<IConfigFileLocator>();

                ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestAttribute>();
            }

            [TestCase(ConfigFileLocator.DefaultFileName, ConfigFileLocator.DefaultFileName)]
            public void ThrowsExceptionOnAmbiguousConfigFileLocation(string repoConfigFile, string workingConfigFile)
            {
                var repositoryConfigFilePath = SetupConfigFileContent(string.Empty, repoConfigFile, repoPath);
                var workingDirectoryConfigFilePath = SetupConfigFileContent(string.Empty, workingConfigFile, workingPath);

                var exception = Should.Throw<WarningException>(() =>
                {
                    configFileLocator.Verify(workingPath, repoPath);
                });

                var expecedMessage = $"Ambiguous config file selection from '{workingDirectoryConfigFilePath}' and '{repositoryConfigFilePath}'";
                exception.Message.ShouldBe(expecedMessage);
            }

            [Test]
            public void NoWarnOnGitVersionYmlFile()
            {
                SetupConfigFileContent(string.Empty, ConfigFileLocator.DefaultFileName, repoPath);

                Should.NotThrow(() => { configurationProvider.Provide(repoPath); });
            }

            [Test]
            public void NoWarnOnNoGitVersionYmlFile()
            {
                Should.NotThrow(() => { configurationProvider.Provide(repoPath); });
            }

            private string SetupConfigFileContent(string text, string fileName, string path)
            {
                var fullPath = Path.Combine(path, fileName);
                fileSystem.WriteAllText(fullPath, text);

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
                gitVersionOptions = new GitVersionOptions { ConfigInfo = { ConfigFile = "my-config.yaml" } };
                repoPath = DefaultRepoPath;
                workingPath = DefaultWorkingPath;

                ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestAttribute>();
            }

            [Test]
            public void ThrowsExceptionOnAmbiguousConfigFileLocation()
            {
                var sp = GetServiceProvider(gitVersionOptions);
                configFileLocator = sp.GetService<IConfigFileLocator>();
                fileSystem = sp.GetService<IFileSystem>();

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

                var sp = GetServiceProvider(gitVersionOptions);
                configFileLocator = sp.GetService<IConfigFileLocator>();
                fileSystem = sp.GetService<IFileSystem>();

                SetupConfigFileContent(string.Empty, path: workingPath);

                Should.NotThrow(() => { configFileLocator.Verify(workingPath, repoPath); });
            }

            [Test]
            public void DoNotThrowWhenWorkingAndRepoPathsAreSame_WithDifferentCasing()
            {
                workingPath = DefaultRepoPath.ToLower();

                var sp = GetServiceProvider(gitVersionOptions);
                configFileLocator = sp.GetService<IConfigFileLocator>();
                fileSystem = sp.GetService<IFileSystem>();

                SetupConfigFileContent(string.Empty, path: workingPath);

                Should.NotThrow(() => { configFileLocator.Verify(workingPath, repoPath); });
            }

            [Test]
            public void DoNotThrowWhenConfigFileIsInSubDirectoryOfRepoPath()
            {
                workingPath = DefaultRepoPath;

                gitVersionOptions = new GitVersionOptions { ConfigInfo = { ConfigFile = "./src/my-config.yaml" } };
                var sp = GetServiceProvider(gitVersionOptions);
                configFileLocator = sp.GetService<IConfigFileLocator>();
                fileSystem = sp.GetService<IFileSystem>();

                SetupConfigFileContent(string.Empty, path: workingPath);

                Should.NotThrow(() => { configFileLocator.Verify(workingPath, repoPath); });
            }

            [Test]
            public void NoWarnOnCustomYmlFile()
            {
                var stringLogger = string.Empty;
                void Action(string info) => stringLogger = info;

                var logAppender = new TestLogAppender(Action);
                var log = new Log(logAppender);

                var sp = GetServiceProvider(gitVersionOptions, log);
                configFileLocator = sp.GetService<IConfigFileLocator>();
                fileSystem = sp.GetService<IFileSystem>();

                SetupConfigFileContent(string.Empty);

                var configurationProvider = sp.GetService<IConfigProvider>();

                configurationProvider.Provide(repoPath);
                stringLogger.Length.ShouldBe(0);
            }

            [Test]
            public void NoWarnOnCustomYmlFileOutsideRepoPath()
            {
                var stringLogger = string.Empty;
                void Action(string info) => stringLogger = info;

                var logAppender = new TestLogAppender(Action);
                var log = new Log(logAppender);

                var sp = GetServiceProvider(gitVersionOptions, log);
                configFileLocator = sp.GetService<IConfigFileLocator>();
                fileSystem = sp.GetService<IFileSystem>();

                SetupConfigFileContent(string.Empty, path: @"c:\\Unrelated\\path");

                var configurationProvider = sp.GetService<IConfigProvider>();

                configurationProvider.Provide(repoPath);
                stringLogger.Length.ShouldBe(0);
            }

            [Test]
            public void ThrowsExceptionOnCustomYmlFileDoesNotExist()
            {
                var sp = GetServiceProvider(gitVersionOptions);
                configFileLocator = sp.GetService<IConfigFileLocator>();

                var exception = Should.Throw<WarningException>(() => { configFileLocator.Verify(workingPath, repoPath); });

                var workingPathFileConfig = Path.Combine(workingPath, gitVersionOptions.ConfigInfo.ConfigFile);
                var repoPathFileConfig = Path.Combine(repoPath, gitVersionOptions.ConfigInfo.ConfigFile);
                var expectedMessage = $"The configuration file was not found at '{workingPathFileConfig}' or '{repoPathFileConfig}'";
                exception.Message.ShouldBe(expectedMessage);
            }

            private string SetupConfigFileContent(string text, string fileName = null, string path = null)
            {
                if (fileName.IsNullOrEmpty()) fileName = configFileLocator.FilePath;
                var filePath = fileName;
                if (!path.IsNullOrEmpty())
                    filePath = Path.Combine(path, filePath);
                fileSystem.WriteAllText(filePath, text);
                return filePath;
            }

            private static IServiceProvider GetServiceProvider(GitVersionOptions gitVersionOptions, ILog log = null)
            {
                return ConfigureServices(services =>
                {
                    if (log != null) services.AddSingleton(log);
                    services.AddSingleton(Options.Create(gitVersionOptions));
                });
            }
        }
    }
}
