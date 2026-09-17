namespace GitVersion.OutputVariables;

/// <summary>Contains all version variables calculated by GitVersion for a given repository state.</summary>
public record GitVersionVariables(
    string? AssemblySemFileVer,
    string? AssemblySemVer,
    string? BranchName,
    string? BuildMetaData,
    string? CommitDate,
    string? CustomVersion,
    string? EscapedBranchName,
    string? FullBuildMetaData,
    string FullSemVer,
    string? InformationalVersion,
    string Major,
    string MajorMinorPatch,
    string Minor,
    string Patch,
    string? PreReleaseLabelName,
    string? PreReleaseLabelNameWithDash,
    string? PreReleaseNumber,
    string? PreReleaseLabel,
    string? PreReleaseLabelWithDash,
    string SemVer,
    string? Sha,
    string? ShortSha,
    string? UncommittedChanges,
    string? VersionSourceDistance,
    string? VersionSourceIncrement,
    string? VersionSourceSemVer,
    string? VersionSourceSha,
    string WeightedPreReleaseNumber
) : IEnumerable<KeyValuePair<string, string?>>
{
    /// <summary>Gets or initializes the semantic baseline supplied by the selected artifact or derivation.</summary>
    public string? SemVerSourceSemVer { get; init; }

    /// <summary>Gets or initializes the semantic source SHA, or null for configuration and branch-name sources.</summary>
    public string? SemVerSourceSha { get; init; }

    /// <summary>Gets or initializes the final increment relative to the semantic baseline.</summary>
    public string? SemVerSourceIncrement { get; init; }

    /// <summary>Gets the commit used as the counting anchor, or null when counting all reachable history.</summary>
    public string? CommitCountSourceSha => string.IsNullOrEmpty(VersionSourceSha) ? null : VersionSourceSha;

    /// <summary>Gets the count of non-ignored commits beyond the counting anchor.</summary>
    public string? CommitCountSourceDistance => VersionSourceDistance;

    internal static readonly List<string> AvailableVariables =
    [
        nameof(AssemblySemFileVer),
        nameof(AssemblySemVer),
        nameof(BranchName),
        nameof(BuildMetaData),
        nameof(CommitDate),
        nameof(CommitCountSourceDistance),
        nameof(CommitCountSourceSha),
        nameof(CustomVersion),
        nameof(EscapedBranchName),
        nameof(FullBuildMetaData),
        nameof(FullSemVer),
        nameof(InformationalVersion),
        nameof(Major),
        nameof(MajorMinorPatch),
        nameof(Minor),
        nameof(Patch),
        nameof(PreReleaseLabelName),
        nameof(PreReleaseLabelNameWithDash),
        nameof(PreReleaseNumber),
        nameof(PreReleaseLabel),
        nameof(PreReleaseLabelWithDash),
        nameof(SemVer),
        nameof(SemVerSourceIncrement),
        nameof(SemVerSourceSemVer),
        nameof(SemVerSourceSha),
        nameof(Sha),
        nameof(ShortSha),
        nameof(UncommittedChanges),
        nameof(VersionSourceDistance),
        nameof(VersionSourceIncrement),
        nameof(VersionSourceSemVer),
        nameof(VersionSourceSha),
        nameof(WeightedPreReleaseNumber)
    ];

    private Dictionary<string, string?> Instance => new()
    {
        { nameof(AssemblySemFileVer), AssemblySemFileVer },
        { nameof(AssemblySemVer), AssemblySemVer },
        { nameof(BranchName), BranchName },
        { nameof(BuildMetaData), BuildMetaData },
        { nameof(CommitDate), CommitDate },
        { nameof(CommitCountSourceDistance), CommitCountSourceDistance },
        { nameof(CommitCountSourceSha), CommitCountSourceSha },
        { nameof(CustomVersion), CustomVersion },
        { nameof(EscapedBranchName), EscapedBranchName },
        { nameof(FullBuildMetaData), FullBuildMetaData },
        { nameof(FullSemVer), FullSemVer },
        { nameof(InformationalVersion), InformationalVersion },
        { nameof(Major), Major },
        { nameof(MajorMinorPatch), MajorMinorPatch },
        { nameof(Minor), Minor },
        { nameof(Patch), Patch },
        { nameof(PreReleaseLabelName), PreReleaseLabelName },
        { nameof(PreReleaseLabelNameWithDash), PreReleaseLabelNameWithDash },
        { nameof(PreReleaseNumber), PreReleaseNumber },
        { nameof(PreReleaseLabel), PreReleaseLabel },
        { nameof(PreReleaseLabelWithDash), PreReleaseLabelWithDash },
        { nameof(SemVer), SemVer },
        { nameof(SemVerSourceIncrement), SemVerSourceIncrement },
        { nameof(SemVerSourceSemVer), SemVerSourceSemVer },
        { nameof(SemVerSourceSha), SemVerSourceSha },
        { nameof(Sha), Sha },
        { nameof(ShortSha), ShortSha },
        { nameof(UncommittedChanges), UncommittedChanges },
        { nameof(VersionSourceDistance), VersionSourceDistance },
        { nameof(VersionSourceIncrement), VersionSourceIncrement },
        { nameof(VersionSourceSemVer), VersionSourceSemVer },
        { nameof(VersionSourceSha), VersionSourceSha },
        { nameof(WeightedPreReleaseNumber), WeightedPreReleaseNumber }
    };

    /// <summary>Returns an enumerator that iterates over all version variable name/value pairs.</summary>
    public IEnumerator<KeyValuePair<string, string?>> GetEnumerator() => Instance.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Attempts to retrieve the value of the version variable identified by <paramref name="variable"/>.</summary>
    public bool TryGetValue(string variable, out string? variableValue) => Instance.TryGetValue(variable, out variableValue);
}
