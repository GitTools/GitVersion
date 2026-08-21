using System.IO.Abstractions;
using GitVersion.Configuration;
using GitVersion.Tests;

namespace GitVersion.App.Tests;

[TestFixture]
public class ConfigurationMigrationExecutorTests
{
    [Test]
    public void InPlaceMigrationWarnsThatCommentsCannotBePreserved()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var inputFile = Path.Combine(directory.FullName, "legacy.yml");
            File.WriteAllText(inputFile, "next-version: 2.0.0");
            var logMessages = new List<string>();
            var executor = new ConfigurationMigrationExecutor(
                new FileSystem(),
                new TestConsoleAdapter(new StringBuilder()),
                new TestLogger<ConfigurationMigrationExecutor>(logMessages.Add),
                Substitute.For<IConfigurationFileLocator>(),
                new ConfigurationMigrationService(new ConfigurationSerializer()));
            var options = new GitVersionOptions { WorkingDirectory = directory.FullName };
            options.ConfigurationMigrationInfo.IsMigration = true;
            options.ConfigurationMigrationInfo.InputFile = inputFile;
            options.ConfigurationMigrationInfo.InPlace = true;

            executor.Execute(options).ShouldBe(0);

            logMessages.ShouldContain(message => message.Contains("Comments cannot be preserved during migration.", StringComparison.Ordinal));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
