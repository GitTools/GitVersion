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

        this.fileSystem.File.WriteAllText(outputFile, migrated);
        return 0;
    }
}
