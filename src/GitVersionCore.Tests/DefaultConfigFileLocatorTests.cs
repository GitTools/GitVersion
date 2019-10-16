using System;
using System.IO;
using NUnit.Framework;
using Shouldly;
using GitVersion.Configuration;
using GitVersion.Exceptions;
using GitVersion.Common;
using GitVersion.Log;

namespace GitVersionCore.Tests
{
    [TestFixture]
    public class DefaultConfigFileLocatorTests : TestBase
    {
        private const string DefaultRepoPath = @"c:\MyGitRepo";
        private const string DefaultWorkingPath = @"c:\MyGitRepo\Working";

        string repoPath;
        string workingPath;
        IFileSystem fileSystem;

        [SetUp]
        public void Setup()
        {
            fileSystem = new TestFileSystem();
            repoPath = DefaultRepoPath;
            workingPath = DefaultWorkingPath;

            ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestAttribute>();
        }
    
        [TestCase(DefaultRepoPath)]
        [TestCase(DefaultWorkingPath)]
        public void WarnOnExistingGitVersionConfigYamlFile(string path)
        {
            SetupConfigFileContent(string.Empty, DefaultConfigFileLocator.ObsoleteFileName, path);

            var output = WithDefaultConfigFileLocator(configFileLocator =>
            {
                configFileLocator.Verify(workingPath, repoPath);
            });

            var configFileDeprecatedWarning = $"{DefaultConfigFileLocator.ObsoleteFileName}' is deprecated, use '{DefaultConfigFileLocator.DefaultFileName}' instead";
            output.Contains(configFileDeprecatedWarning).ShouldBe(true);
        }

        [TestCase(DefaultRepoPath)]
        [TestCase(DefaultWorkingPath)]
        public void WarnOnAmbiguousConfigFilesAtTheSameProjectRootDirectory(string path)
        {
            SetupConfigFileContent(string.Empty, DefaultConfigFileLocator.ObsoleteFileName, path);
            SetupConfigFileContent(string.Empty, DefaultConfigFileLocator.DefaultFileName, path);

            var output = WithDefaultConfigFileLocator(configFileLocator =>
            {
                configFileLocator.Verify(workingPath, repoPath);
            });

            var configFileDeprecatedWarning = $"Ambiguous config files at '{path}'";
            output.Contains(configFileDeprecatedWarning).ShouldBe(true);
        }

        [TestCase(DefaultConfigFileLocator.DefaultFileName, DefaultConfigFileLocator.DefaultFileName)]
        [TestCase(DefaultConfigFileLocator.DefaultFileName, DefaultConfigFileLocator.ObsoleteFileName)]
        [TestCase(DefaultConfigFileLocator.ObsoleteFileName, DefaultConfigFileLocator.DefaultFileName)]
        [TestCase(DefaultConfigFileLocator.ObsoleteFileName, DefaultConfigFileLocator.ObsoleteFileName)]
        public void ThrowsExceptionOnAmbiguousConfigFileLocation(string repoConfigFile, string workingConfigFile)
        {
            var repositoryConfigFilePath = SetupConfigFileContent(string.Empty, repoConfigFile, repoPath);
            var workingDirectoryConfigFilePath = SetupConfigFileContent(string.Empty, workingConfigFile, workingPath);

            WarningException exception = Should.Throw<WarningException>(() =>
            {
                WithDefaultConfigFileLocator(configFileLocator =>
                {
                    configFileLocator.Verify(workingPath, repoPath);
                });
            });

            var expecedMessage = $"Ambiguous config file selection from '{workingDirectoryConfigFilePath}' and '{repositoryConfigFilePath}'";
            exception.Message.ShouldBe(expecedMessage);
        }

        [Test]
        public void NoWarnOnGitVersionYmlFile()
        {
            SetupConfigFileContent(string.Empty);

            var output = WithDefaultConfigFileLocator(configFileLocator =>
            {
                ConfigurationProvider.Provide(repoPath, configFileLocator); 
            });

            output.Length.ShouldBe(0);
        }

        string SetupConfigFileContent(string text, string fileName = DefaultConfigFileLocator.DefaultFileName)
        {
            return SetupConfigFileContent(text, fileName, repoPath);
        }

        string SetupConfigFileContent(string text, string fileName, string path)
        {
            var fullPath = Path.Combine(path, fileName);
            fileSystem.WriteAllText(fullPath, text);

            return fullPath;
        }

        [Test]
        public void WarnOnObsoleteIsDevelopBranchConfigurationSetting()
        {
            const string text = @"
assembly-versioning-scheme: MajorMinorPatch
branches:
  master:
    tag: beta
    is-develop: true";

            OldConfigurationException exception = Should.Throw<OldConfigurationException>(() =>
            {
                LegacyConfigNotifier.Notify(new StringReader(text));
            });

            const string expectedMessage = @"'is-develop' is deprecated, use 'tracks-release-branches' instead.";
            exception.Message.ShouldContain(expectedMessage);
        }

        private string WithDefaultConfigFileLocator(Action<IConfigFileLocator> action)
        {
            var stringLogger = string.Empty;
            void Action(string info) => stringLogger = info;

            var logAppender = new TestLogAppender(Action);
            var log = new Log(logAppender);

            var configFileLocator = new DefaultConfigFileLocator(fileSystem, log);
            action(configFileLocator);

            return stringLogger;
        }
    }
}
