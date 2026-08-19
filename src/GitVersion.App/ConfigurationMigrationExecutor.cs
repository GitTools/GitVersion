using System.IO.Abstractions;
using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion;

internal class ConfigurationMigrationExecutor(
    IFileSystem fileSystem,
    IConsole console,
    ILogger<ConfigurationMigrationExecutor> logger,
    IConfigurationFileLocator configurationFileLocator,
    IConfigurationMigrationService migrationService) : IConfigurationMigrationExecutor
{
    private readonly IFileSystem fileSystem = fileSystem.NotNull();
    private readonly IConsole console = console.NotNull();
    private readonly ILogger<ConfigurationMigrationExecutor> logger = logger.NotNull();
    private readonly IConfigurationFileLocator configurationFileLocator = configurationFileLocator.NotNull();
    private readonly IConfigurationMigrationService migrationService = migrationService.NotNull();

    public int Execute(GitVersionOptions options)
    {
        var migration = options.ConfigurationMigrationInfo;
        var inputFile = migration.InputFile is null
            ? this.configurationFileLocator.GetConfigurationFile(options.WorkingDirectory)
            : Path.GetFullPath(migration.InputFile, options.WorkingDirectory);
        if (inputFile is null || !this.fileSystem.File.Exists(inputFile))
        {
            throw new WarningException("Could not find a configuration file to migrate. Specify one with --config.");
        }

        var migrated = this.migrationService.Migrate(this.fileSystem.File.ReadAllText(inputFile));
        if (!migration.InPlace && migration.OutputFile is null)
        {
            this.console.Write(migrated);
            return 0;
        }

        var outputFile = migration.InPlace ? inputFile : Path.GetFullPath(migration.OutputFile!, options.WorkingDirectory);
        var overwritesExistingFile = this.fileSystem.File.Exists(outputFile);
        if (!migration.InPlace && overwritesExistingFile && !migration.Force)
        {
            throw new WarningException($"The output file '{outputFile}' already exists. Use --force to replace it.");
        }

        if (overwritesExistingFile)
        {
            Console.Error.WriteLine($"Replacing '{outputFile}'. Comments cannot be preserved during migration.");
            this.logger.LogWarning("Replacing '{ConfigurationFile}'. Comments cannot be preserved during migration.", outputFile);
        }

        WriteAtomically(outputFile, migrated);
        return 0;
    }

    private void WriteAtomically(string outputFile, string migrated)
    {
        var directory = this.fileSystem.Path.GetDirectoryName(outputFile)!;
        var temporaryFile = this.fileSystem.Path.Combine(directory, $".{this.fileSystem.Path.GetFileName(outputFile)}.{Guid.NewGuid():N}.tmp");

        try
        {
            this.fileSystem.File.WriteAllText(temporaryFile, migrated);
            this.fileSystem.File.Move(temporaryFile, outputFile, overwrite: true);
        }
        finally
        {
            if (this.fileSystem.File.Exists(temporaryFile))
            {
                this.fileSystem.File.Delete(temporaryFile);
            }
        }
    }
}
