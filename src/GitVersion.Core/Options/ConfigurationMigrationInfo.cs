namespace GitVersion;

/// <summary>Settings that control a configuration migration operation.</summary>
public class ConfigurationMigrationInfo
{
    /// <summary>Gets or sets a value indicating whether the configuration migration command should be executed.</summary>
    public bool IsMigration { get; set; }

    /// <summary>Gets or sets the configuration file to migrate.</summary>
    public string? InputFile { get; set; }

    /// <summary>Gets or sets the file to which the migrated configuration should be written.</summary>
    public string? OutputFile { get; set; }

    /// <summary>Gets or sets a value indicating whether the source configuration file should be replaced.</summary>
    public bool InPlace { get; set; }

    /// <summary>Gets or sets a value indicating whether an existing output file may be replaced.</summary>
    public bool Force { get; set; }
}
