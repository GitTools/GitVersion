using System.IO;
using GitVersion;
using GitVersion.Configuration;
using GitVersionCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace GitVersionCore.Tests
{
    [TestFixture]
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

        [TestCase(DefaultConfigFileLocator.DefaultFileName, DefaultConfigFileLocator.DefaultFileName)]
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
            SetupConfigFileContent(string.Empty, DefaultConfigFileLocator.DefaultFileName, repoPath);

            Should.NotThrow(() => { configurationProvider.Provide(repoPath); });
        }

        private string SetupConfigFileContent(string text, string fileName, string path)
        {
            var fullPath = Path.Combine(path, fileName);
            fileSystem.WriteAllText(fullPath, text);

            return fullPath;
        }
    }
}
