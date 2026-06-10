namespace GitVersion.Configuration;

/// <summary>Builds an <see cref="IGitVersionConfiguration"/> instance, optionally merging override values.</summary>
public interface IConfigurationBuilder
{
    /// <summary>Merges the supplied key/value pairs into the configuration, overriding any existing values.</summary>
    void AddOverride(IReadOnlyDictionary<object, object?> value);

    /// <summary>Produces the fully resolved <see cref="IGitVersionConfiguration"/>.</summary>
    IGitVersionConfiguration Build();
}
