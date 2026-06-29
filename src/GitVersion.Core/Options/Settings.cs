namespace GitVersion;

/// <summary>Controls general runtime behaviours such as caching, fetching, and branch normalisation.</summary>
public record Settings
{
    /// <summary>Gets or sets a value indicating whether fetching from the remote should be skipped.</summary>
    public bool NoFetch { get; set; }

    /// <summary>Gets or sets a value indicating whether the on-disk version cache should be ignored.</summary>
    public bool NoCache { get; set; }

    /// <summary>Gets or sets a value indicating whether branch normalisation should be skipped.</summary>
    public bool NoNormalize { get; set; }

    /// <summary>Gets or sets a value indicating whether only tracked (remote-tracking) branches are considered during version calculation.</summary>
    public bool OnlyTrackedBranches { get; set; }

    /// <summary>Gets or sets a value indicating whether GitVersion should continue even when a shallow clone is detected.</summary>
    public bool AllowShallow { get; set; }
}
