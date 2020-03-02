using System;
using System.IO;
using NUnit.Framework;
using Shouldly;
using GitVersion.Configuration;
using GitVersion.Exceptions;
using GitVersion;
using GitVersion.Logging;
using GitVersionCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
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
        private IConfigFileLocator configFileLocator;
        private Arguments arguments;

        [SetUp]
        public void Setup()
        {
            arguments = new Arguments { ConfigFile = "my-config.yaml" };
            repoPath = DefaultRepoPath;
            workingPath = DefaultWorkingPath;

            ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestAttribute>();
        }

        [Test]
        public void ThrowsExceptionOnAmbiguousConfigFileLocation()
        {
            var sp = GetServiceProvider(arguments);
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

            var sp = GetServiceProvider(arguments);
            configFileLocator = sp.GetService<IConfigFileLocator>();
            fileSystem = sp.GetService<IFileSystem>();

            SetupConfigFileContent(string.Empty, path: workingPath);

            Should.NotThrow(() => { configFileLocator.Verify(workingPath, repoPath); });
        }

        [Test]
        public void DoNotThrowWhenWorkingAndRepoPathsAreSame_WithDifferentCasing()
        {
            workingPath = DefaultRepoPath.ToLower();

            var sp = GetServiceProvider(arguments);
            configFileLocator = sp.GetService<IConfigFileLocator>();
            fileSystem = sp.GetService<IFileSystem>();

            SetupConfigFileContent(string.Empty, path: workingPath);

            Should.NotThrow(() => { configFileLocator.Verify(workingPath, repoPath); });
        }

        [Test]
        public void DoNotThrowWhenConfigFileIsInSubDirectoryOfRepoPath()
        {
            workingPath = DefaultRepoPath;

            arguments = new Arguments { ConfigFile = "./src/my-config.yaml" };
            var sp = GetServiceProvider(arguments);
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

            var sp = GetServiceProvider(arguments, log);
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

            var sp = GetServiceProvider(arguments, log);
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
            var sp = GetServiceProvider(arguments);
            configFileLocator = sp.GetService<IConfigFileLocator>();

            var exception = Should.Throw<WarningException>(() => { configFileLocator.Verify(workingPath, repoPath); });

            var workingPathFileConfig = Path.Combine(workingPath, arguments.ConfigFile);
            var repoPathFileConfig = Path.Combine(repoPath, arguments.ConfigFile);
            var expectedMessage = $"The configuration file was not found at '{workingPathFileConfig}' or '{repoPathFileConfig}'";
            exception.Message.ShouldBe(expectedMessage);
        }

        private string SetupConfigFileContent(string text, string fileName = null, string path = null)
        {
            if (string.IsNullOrEmpty(fileName)) fileName = ((NamedConfigFileLocator)configFileLocator).FilePath;
            var filePath = fileName;
            if (!string.IsNullOrEmpty(path))
                filePath = Path.Combine(path, filePath);
            fileSystem.WriteAllText(filePath, text);
            return filePath;
        }

        private static IServiceProvider GetServiceProvider(Arguments arguments, ILog log = null)
        {
            return ConfigureServices(services =>
            {
                if (log != null) services.AddSingleton(log);
                services.AddSingleton(Options.Create(arguments));
            });
        }
    }
}
