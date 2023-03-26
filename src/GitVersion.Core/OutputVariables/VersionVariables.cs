using GitVersion.Extensions;
using static GitVersion.Extensions.ObjectExtensions;

namespace GitVersion.OutputVariables;

public class VersionVariables : IEnumerable<KeyValuePair<string, string>>
{
    public VersionVariables(string major,
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
    public static IEnumerable<string> AvailableVariables => typeof(VersionVariables)
        .GetProperties()
        .Where(p => !p.GetCustomAttributes(typeof(ReflectionIgnoreAttribute), false).Any())
        .Select(p => p.Name)
        .OrderBy(a => a, StringComparer.Ordinal);

    [ReflectionIgnore]
    public string? FileName { get; set; }

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => this.GetProperties().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool TryGetValue(string variable, out string? variableValue)
    {
        var propertyInfo = typeof(VersionVariables).GetProperty(variable);
        if (propertyInfo != null)
        {
            variableValue = propertyInfo.GetValue(this, null) as string;
            return true;
        }

        variableValue = null;
        return false;
    }
}
