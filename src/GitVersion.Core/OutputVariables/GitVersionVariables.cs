using static GitVersion.Extensions.ObjectExtensions;

namespace GitVersion.OutputVariables;

public class GitVersionVariables : IEnumerable<KeyValuePair<string, string?>>
{
    public static readonly List<string> AvailableVariables = new()
    {
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
        nameof(CommitDate),
        nameof(VersionSourceSha),
        nameof(CommitsSinceVersionSource),
        nameof(UncommittedChanges),
    };

    private readonly Dictionary<string, string?> _variables;
    public GitVersionVariables(string major,
                               string minor,
                               string patch,
                               string? buildMetaData,
                               string? fullBuildMetaData,
                               string? branchName,
                               string? escapedBranchName,
                               string? sha,
                               string? shortSha,
                               string majorMinorPatch,
                               string semVer,
                               string fullSemVer,
                               string? assemblySemVer,
                               string? assemblySemFileVer,
                               string? preReleaseTag,
                               string? preReleaseTagWithDash,
                               string? preReleaseLabel,
                               string? preReleaseLabelWithDash,
                               string? preReleaseNumber,
                               string weightedPreReleaseNumber,
                               string? informationalVersion,
                               string? commitDate,
                               string? versionSourceSha,
                               string? commitsSinceVersionSource,
                               string? uncommittedChanges)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        BuildMetaData = buildMetaData;
        FullBuildMetaData = fullBuildMetaData;
        BranchName = branchName;
        EscapedBranchName = escapedBranchName;
        Sha = sha;
        ShortSha = shortSha;
        MajorMinorPatch = majorMinorPatch;
        SemVer = semVer;
        FullSemVer = fullSemVer;
        AssemblySemVer = assemblySemVer;
        AssemblySemFileVer = assemblySemFileVer;
        PreReleaseTag = preReleaseTag;
        PreReleaseTagWithDash = preReleaseTagWithDash;
        PreReleaseLabel = preReleaseLabel;
        PreReleaseLabelWithDash = preReleaseLabelWithDash;
        PreReleaseNumber = preReleaseNumber;
        WeightedPreReleaseNumber = weightedPreReleaseNumber;
        InformationalVersion = informationalVersion;
        CommitDate = commitDate;
        VersionSourceSha = versionSourceSha;
        CommitsSinceVersionSource = commitsSinceVersionSource;
        UncommittedChanges = uncommittedChanges;

        _variables = new Dictionary<string, string?>
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
            { nameof(CommitDate), CommitDate },
            { nameof(VersionSourceSha), VersionSourceSha },
            { nameof(CommitsSinceVersionSource), CommitsSinceVersionSource },
            { nameof(UncommittedChanges), UncommittedChanges }
        };
    }

    public string Major { get; }
    public string Minor { get; }
    public string Patch { get; }
    public string? PreReleaseTag { get; }
    public string? PreReleaseTagWithDash { get; }
    public string? PreReleaseLabel { get; }
    public string? PreReleaseLabelWithDash { get; }
    public string? PreReleaseNumber { get; }
    public string WeightedPreReleaseNumber { get; }
    public string? BuildMetaData { get; }
    public string? FullBuildMetaData { get; }
    public string MajorMinorPatch { get; }
    public string SemVer { get; }
    public string? AssemblySemVer { get; }
    public string? AssemblySemFileVer { get; }
    public string FullSemVer { get; }
    public string? InformationalVersion { get; }
    public string? BranchName { get; }
    public string? EscapedBranchName { get; }
    public string? Sha { get; }
    public string? ShortSha { get; }
    public string? VersionSourceSha { get; }
    public string? CommitsSinceVersionSource { get; }
    public string? CommitDate { get; set; }
    public string? UncommittedChanges { get; }

    [ReflectionIgnore]
    public string? FileName { get; set; }

    public IEnumerator<KeyValuePair<string, string?>> GetEnumerator() => _variables.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _variables.GetEnumerator();

    public bool TryGetValue(string variable, out string? variableValue)
    {
        if (_variables.TryGetValue(variable, out variableValue)) return true;
        variableValue = null;
        return false;
    }
}
