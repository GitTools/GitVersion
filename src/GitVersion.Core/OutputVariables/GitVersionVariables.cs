namespace GitVersion.OutputVariables;

public record GitVersionVariables(
    string? AssemblySemFileVer,
    string? AssemblySemVer,
    string? BranchName,
    string? BuildMetaData,
    string? CommitDate,
    [property: Obsolete("CommitsSinceVersionSource has been deprecated. Use VersionSourceDistance instead.")]
    string? CommitsSinceVersionSource,
    string? EscapedBranchName,
    string? FullBuildMetaData,
    string FullSemVer,
    string? InformationalVersion,
    string Major,
    string MajorMinorPatch,
    string Minor,
    string Patch,
    string? PreReleaseLabel,
    string? PreReleaseLabelWithDash,
    string? PreReleaseNumber,
    string? PreReleaseTag,
    string? PreReleaseTagWithDash,
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
    internal static readonly List<string> AvailableVariables =
    [
        nameof(AssemblySemFileVer),
        nameof(AssemblySemVer),
        nameof(BranchName),
        nameof(BuildMetaData),
        nameof(CommitDate),
        nameof(CommitsSinceVersionSource),
        nameof(EscapedBranchName),
        nameof(FullBuildMetaData),
        nameof(FullSemVer),
        nameof(InformationalVersion),
        nameof(Major),
        nameof(MajorMinorPatch),
        nameof(Minor),
        nameof(Patch),
        nameof(PreReleaseLabel),
        nameof(PreReleaseLabelWithDash),
        nameof(PreReleaseNumber),
        nameof(PreReleaseTag),
        nameof(PreReleaseTagWithDash),
        nameof(SemVer),
        nameof(Sha),
        nameof(ShortSha),
        nameof(UncommittedChanges),
        nameof(VersionSourceDistance),
        nameof(VersionSourceIncrement),
        nameof(VersionSourceSemVer),
        nameof(VersionSourceSha),
        nameof(WeightedPreReleaseNumber)
    ];

    private Dictionary<string, string?> Instance => field ??= new()
    {
        { nameof(AssemblySemFileVer), AssemblySemFileVer },
        { nameof(AssemblySemVer), AssemblySemVer },
        { nameof(BranchName), BranchName },
        { nameof(BuildMetaData), BuildMetaData },
        { nameof(CommitDate), CommitDate },
        { nameof(CommitsSinceVersionSource), CommitsSinceVersionSource },
        { nameof(EscapedBranchName), EscapedBranchName },
        { nameof(FullBuildMetaData), FullBuildMetaData },
        { nameof(FullSemVer), FullSemVer },
        { nameof(InformationalVersion), InformationalVersion },
        { nameof(Major), Major },
        { nameof(MajorMinorPatch), MajorMinorPatch },
        { nameof(Minor), Minor },
        { nameof(Patch), Patch },
        { nameof(PreReleaseLabel), PreReleaseLabel },
        { nameof(PreReleaseLabelWithDash), PreReleaseLabelWithDash },
        { nameof(PreReleaseNumber), PreReleaseNumber },
        { nameof(PreReleaseTag), PreReleaseTag },
        { nameof(PreReleaseTagWithDash), PreReleaseTagWithDash },
        { nameof(SemVer), SemVer },
        { nameof(Sha), Sha },
        { nameof(ShortSha), ShortSha },
        { nameof(UncommittedChanges), UncommittedChanges },
        { nameof(VersionSourceDistance), VersionSourceDistance },
        { nameof(VersionSourceIncrement), VersionSourceIncrement },
        { nameof(VersionSourceSemVer), VersionSourceSemVer },
        { nameof(VersionSourceSha), VersionSourceSha },
        { nameof(WeightedPreReleaseNumber), WeightedPreReleaseNumber }
    };

    public IEnumerator<KeyValuePair<string, string?>> GetEnumerator() => Instance.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool TryGetValue(string variable, out string? variableValue) => Instance.TryGetValue(variable, out variableValue);
}
