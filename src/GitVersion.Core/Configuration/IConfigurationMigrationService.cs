namespace GitVersion.Configuration;

internal interface IConfigurationMigrationService
{
    string Migrate(string input);
}
