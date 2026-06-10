namespace GitVersion.Configuration;

/// <summary>Specifies which commits or time ranges should be excluded from version calculation.</summary>
public interface IIgnoreConfiguration
{
    /// <summary>Gets the cut-off date before which commits are ignored; <see langword="null"/> means no date filter is applied.</summary>
    DateTimeOffset? Before { get; }

    /// <summary>Gets the set of commit SHAs that should be excluded from version calculation.</summary>
    IReadOnlySet<string> Shas { get; }

    /// <summary>Gets the set of file paths whose changes should be ignored during version calculation.</summary>
    IReadOnlySet<string> Paths { get; }

    /// <summary>Gets a value indicating whether this configuration contains no ignore rules.</summary>
    bool IsEmpty { get; }
}
