namespace GitVersion;

/// <summary>Holds options that control how the GitVersion configuration file is located and applied.</summary>
public record ConfigurationInfo
{
    /// <summary>Gets or sets whether to validate configuration without calculating a version.</summary>
    internal bool Validate { get; set; }

    /// <summary>Gets or sets whether validation emits a JSON result.</summary>
    internal bool ValidationJson { get; set; }

    /// <summary>Gets or sets raw command-local overrides, parsed during validation.</summary>
    internal string[] ValidationOverrides { get; set; } = [];

    /// <summary>Gets or sets a command invocation error to report through validation output.</summary>
    internal string? ValidationError { get; set; }

    /// <summary>Gets or sets an explicit path to the GitVersion configuration file, overriding automatic discovery.</summary>
    public string? ConfigurationFile { get; set; }

    /// <summary>Gets or sets a value indicating whether the effective configuration should be printed to the output.</summary>
    public bool ShowConfiguration { get; set; }

    /// <summary>Gets or sets the public v7 configuration property path to print as a JSON scalar.</summary>
    public string? PropertyPath { get; set; }

    /// <summary>Gets or sets a dictionary of key/value pairs that override specific configuration values at runtime.</summary>
    public IReadOnlyDictionary<object, object?>? OverrideConfiguration { get; set; }
}
