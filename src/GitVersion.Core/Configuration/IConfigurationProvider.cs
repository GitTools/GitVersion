namespace GitVersion.Configuration;

/// <summary>Loads and provides the <see cref="IGitVersionConfiguration"/> for the current repository.</summary>
public interface IConfigurationProvider
{
    /// <summary>Returns the resolved configuration, optionally applying the supplied override values.</summary>
    IGitVersionConfiguration Provide(IReadOnlyDictionary<object, object?>? overrideConfiguration = null);
}
