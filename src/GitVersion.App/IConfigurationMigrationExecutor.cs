namespace GitVersion;

internal interface IConfigurationMigrationExecutor
{
    int Execute(GitVersionOptions options);
}
