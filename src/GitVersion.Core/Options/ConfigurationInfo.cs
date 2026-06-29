namespace GitVersion;

/// <summary>Holds options that control how the GitVersion configuration file is located and applied.</summary>
public record ConfigurationInfo
{
    /// <summary>Gets or sets an explicit path to the GitVersion configuration file, overriding automatic discovery.</summary>
    public string? ConfigurationFile { get; set; }

    /// <summary>Gets or sets a value indicating whether the effective configuration should be printed to the output.</summary>
    public bool ShowConfiguration { get; set; }

    /// <summary>Gets or sets a dictionary of key/value pairs that override specific configuration values at runtime.</summary>
    public IReadOnlyDictionary<object, object?>? OverrideConfiguration { get; set; }
}
