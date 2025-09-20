namespace GitVersion.OutputVariables;

public record GitVersionVariables(string Major,
                                  string Minor,
                                  string Patch,
                                  string? BuildMetaData,
                                  string? FullBuildMetaData,
                                  string? BranchName,
                                  string? EscapedBranchName,
                                  string? Sha,
                                  string? ShortSha,
                                  string MajorMinorPatch,
                                  string SemVer,
                                  string FullSemVer,
                                  string? AssemblySemVer,
                                  string? AssemblySemFileVer,
                                  string? PreReleaseTag,
                                  string? PreReleaseTagWithDash,
                                  string? PreReleaseLabel,
                                  string? PreReleaseLabelWithDash,
                                  string? PreReleaseNumber,
                                  string WeightedPreReleaseNumber,
                                  string? InformationalVersion,
                                  string? CustomVersion,
                                  string? CommitDate,
                                  string? VersionSourceSha,
                                  string? CommitsSinceVersionSource,
                                  string? UncommittedChanges) : IEnumerable<KeyValuePair<string, string?>>
{
    internal static readonly List<string> AvailableVariables =
    [
        nameof(Major),
        nameof(Minor),
        nameof(Patch),
        nameof(BuildMetaData),
        nameof(FullBuildMetaData),
        nameof(BranchName),
        nameof(EscapedBranchName),
        nameof(Sha),
        nameof(ShortSha),
        nameof(MajorMinorPatch),
        nameof(SemVer),
        nameof(FullSemVer),
        nameof(AssemblySemVer),
        nameof(AssemblySemFileVer),
        nameof(PreReleaseTag),
        nameof(PreReleaseTagWithDash),
        nameof(PreReleaseLabel),
        nameof(PreReleaseLabelWithDash),
        nameof(PreReleaseNumber),
        nameof(WeightedPreReleaseNumber),
        nameof(InformationalVersion),
        nameof(CustomVersion),
        nameof(CommitDate),
        nameof(VersionSourceSha),
        nameof(CommitsSinceVersionSource),
        nameof(UncommittedChanges)
    ];

    private Dictionary<string, string?> Instance => new()
    {
        { nameof(Major), Major },
        { nameof(Minor), Minor },
        { nameof(Patch), Patch },
        { nameof(BuildMetaData), BuildMetaData },
        { nameof(FullBuildMetaData), FullBuildMetaData },
        { nameof(BranchName), BranchName },
        { nameof(EscapedBranchName), EscapedBranchName },
        { nameof(Sha), Sha },
        { nameof(ShortSha), ShortSha },
        { nameof(MajorMinorPatch), MajorMinorPatch },
        { nameof(SemVer), SemVer },
        { nameof(FullSemVer), FullSemVer },
        { nameof(AssemblySemVer), AssemblySemVer },
        { nameof(AssemblySemFileVer), AssemblySemFileVer },
        { nameof(PreReleaseTag), PreReleaseTag },
        { nameof(PreReleaseTagWithDash), PreReleaseTagWithDash },
        { nameof(PreReleaseLabel), PreReleaseLabel },
        { nameof(PreReleaseLabelWithDash), PreReleaseLabelWithDash },
        { nameof(PreReleaseNumber), PreReleaseNumber },
        { nameof(WeightedPreReleaseNumber), WeightedPreReleaseNumber },
        { nameof(InformationalVersion), InformationalVersion },
        { nameof(CustomVersion), CustomVersion },
        { nameof(CommitDate), CommitDate },
        { nameof(VersionSourceSha), VersionSourceSha },
        { nameof(CommitsSinceVersionSource), CommitsSinceVersionSource },
        { nameof(UncommittedChanges), UncommittedChanges }
    };

    public IEnumerator<KeyValuePair<string, string?>> GetEnumerator() => Instance.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Instance.GetEnumerator();

    public bool TryGetValue(string variable, out string? variableValue)
    {
        if (Instance.TryGetValue(variable, out variableValue)) return true;
        variableValue = null;
        return false;
    }
}
