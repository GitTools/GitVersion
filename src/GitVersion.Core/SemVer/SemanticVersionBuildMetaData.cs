using System.Globalization;
using GitVersion.Extensions;
using GitVersion.Helpers;

namespace GitVersion;

/// <summary>Holds the build metadata attached to a semantic version (commits-since-tag, branch, SHA, commit date, etc.).</summary>
public sealed class SemanticVersionBuildMetaData : IFormattable, IEquatable<SemanticVersionBuildMetaData?>
{
    /// <summary>An empty build metadata instance with no fields set.</summary>
    public static readonly SemanticVersionBuildMetaData Empty = new();

    private static readonly LambdaEqualityHelper<SemanticVersionBuildMetaData> EqualityHelper =
        new(x => x.CommitsSinceTag, x => x.Branch, x => x.Sha);

    /// <summary>Gets or initializes the number of commits since the last version tag.</summary>
    public long? CommitsSinceTag { get; init; }

    /// <summary>Gets or initializes the name of the branch on which this version was calculated.</summary>
    public string? Branch { get; init; }

    /// <summary>Gets or initializes the full SHA of the current commit.</summary>
    public string? Sha { get; init; }

    /// <summary>Gets or initializes the abbreviated SHA of the current commit.</summary>
    public string? ShortSha { get; init; }

    /// <summary>Gets or initializes any additional free-form metadata included in the build metadata string.</summary>
    public string? OtherMetaData { get; init; }

    /// <summary>Gets or initializes the date of the current commit.</summary>
    public DateTimeOffset? CommitDate { get; init; }

    /// <summary>Gets or initializes the semantic version of the source tag from which the version was calculated.</summary>
    public SemanticVersion? VersionSourceSemVer { get; init; }

    /// <summary>Gets or initializes the SHA of the source tag commit.</summary>
    public string? VersionSourceSha { get; init; }

    /// <summary>Gets the number of commits since the version source (alias for <see cref="VersionSourceDistance"/>).</summary>
    public long CommitsSinceVersionSource => VersionSourceDistance;

    /// <summary>Gets or initializes the number of commits between the version source tag and the current commit.</summary>
    public long VersionSourceDistance { get; init; }

    /// <summary>Gets or initializes the number of uncommitted changes in the working tree.</summary>
    public long UncommittedChanges { get; init; }

    /// <summary>Gets or initializes the version field that was incremented relative to the version source.</summary>
    public VersionField VersionSourceIncrement { get; init; }

    /// <summary>Initializes a new empty build metadata instance.</summary>
    public SemanticVersionBuildMetaData()
    {
    }

    /// <summary>Initializes a new build metadata instance with all fields specified.</summary>
    public SemanticVersionBuildMetaData(
        SemanticVersion? versionSourceSemVer,
        string? versionSourceSha,
        long? commitsSinceTag,
        string? branch,
        string? commitSha,
        string? commitShortSha,
        DateTimeOffset? commitDate,
        long numberOfUnCommittedChanges,
        VersionField versionSourceIncrement,
        string? otherMetadata = null)
    {
        Sha = commitSha;
        ShortSha = commitShortSha;
        CommitsSinceTag = commitsSinceTag;
        Branch = branch;
        CommitDate = commitDate;
        OtherMetaData = otherMetadata;
        VersionSourceIncrement = versionSourceIncrement;
        VersionSourceSemVer = versionSourceSemVer;
        VersionSourceSha = versionSourceSha;
        VersionSourceDistance = commitsSinceTag ?? 0;
        UncommittedChanges = numberOfUnCommittedChanges;
    }

    /// <summary>Initializes a new build metadata instance as a copy of <paramref name="buildMetaData"/>.</summary>
    public SemanticVersionBuildMetaData(SemanticVersionBuildMetaData buildMetaData)
    {
        buildMetaData.NotNull();

        Sha = buildMetaData.Sha;
        ShortSha = buildMetaData.ShortSha;
        CommitsSinceTag = buildMetaData.CommitsSinceTag;
        Branch = buildMetaData.Branch;
        CommitDate = buildMetaData.CommitDate;
        OtherMetaData = buildMetaData.OtherMetaData;
        VersionSourceSemVer = buildMetaData.VersionSourceSemVer;
        VersionSourceSha = buildMetaData.VersionSourceSha;
        VersionSourceDistance = buildMetaData.VersionSourceDistance;
        UncommittedChanges = buildMetaData.UncommittedChanges;
        VersionSourceIncrement = buildMetaData.VersionSourceIncrement;
    }

    /// <summary>Returns <see langword="true"/> when <paramref name="obj"/> is a <see cref="SemanticVersionBuildMetaData"/> equal to this instance.</summary>
    public override bool Equals(object? obj) => Equals(obj as SemanticVersionBuildMetaData);

    /// <summary>Returns <see langword="true"/> when this instance is equal to <paramref name="other"/> by commit count, branch, and SHA.</summary>
    public bool Equals(SemanticVersionBuildMetaData? other) => EqualityHelper.Equals(this, other);

    /// <summary>Returns a hash code based on commit count, branch, and SHA.</summary>
    public override int GetHashCode() => EqualityHelper.GetHashCode(this);

    /// <summary>Returns the default build metadata string (commits-since-tag).</summary>
    public override string ToString() => ToString("b");

    /// <summary>Returns a string representation of this build metadata using the given format specifier.</summary>
    public string ToString(string format) => ToString(format, CultureInfo.CurrentCulture);

    /// <summary>
    /// <para>b - Formats just the build number</para>
    /// <para>s - Formats the build number and the Git Sha</para>
    /// <para>f - Formats the full build metadata</para>
    /// </summary>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        if (formatProvider?.GetFormat(GetType()) is ICustomFormatter formatter)
            return formatter.Format(format, this, formatProvider);

        if (format.IsNullOrEmpty())
            format = "b";

        format = format.ToLower();
        return format.ToLower() switch
        {
            "b" => $"{CommitsSinceTag}",
            "s" => $"{CommitsSinceTag}{(Sha.IsNullOrEmpty() ? null : ".Sha." + Sha)}".TrimStart('.'),
            "f" => $"{CommitsSinceTag}{(Branch.IsNullOrEmpty() ? null : ".Branch." + FormatMetaDataPart(Branch))}{(Sha.IsNullOrEmpty() ? null : ".Sha." + Sha)}{(OtherMetaData.IsNullOrEmpty() ? null : "." + FormatMetaDataPart(OtherMetaData))}".TrimStart('.'),
            _ => throw new FormatException($"Unknown format '{format}'.")
        };
    }

    /// <summary>Returns <see langword="true"/> when <paramref name="left"/> and <paramref name="right"/> are equal.</summary>
    public static bool operator ==(SemanticVersionBuildMetaData? left, SemanticVersionBuildMetaData? right) =>
        Equals(left, right);

    /// <summary>Returns <see langword="true"/> when <paramref name="left"/> and <paramref name="right"/> are not equal.</summary>
    public static bool operator !=(SemanticVersionBuildMetaData? left, SemanticVersionBuildMetaData? right) =>
        !Equals(left, right);

    /// <summary>Implicitly converts a build metadata instance to its string representation.</summary>
    public static implicit operator string?(SemanticVersionBuildMetaData? preReleaseTag) => preReleaseTag?.ToString();

    /// <summary>Implicitly parses a string into a <see cref="SemanticVersionBuildMetaData"/> instance.</summary>
    public static implicit operator SemanticVersionBuildMetaData(string preReleaseTag) => Parse(preReleaseTag);

    /// <summary>Parses the build metadata string, returning <see cref="Empty"/> when the input is null or empty.</summary>
    public static SemanticVersionBuildMetaData Parse(string? buildMetaData)
    {
        if (buildMetaData.IsNullOrEmpty())
            return Empty;

        var parsed = RegexPatterns.SemanticVersion.ParseBuildMetaDataRegex.Match(buildMetaData);

        long? buildMetaDataCommitsSinceTag = null;
        long? buildMetaDataVersionSourceDistance = null;
        if (parsed.Groups["BuildNumber"].Success)
        {
            if (long.TryParse(parsed.Groups["BuildNumber"].Value, out var buildNumber))
                buildMetaDataCommitsSinceTag = buildNumber;
            buildMetaDataVersionSourceDistance = buildMetaDataCommitsSinceTag ?? 0;
        }

        string? buildMetaDataBranch = null;
        if (parsed.Groups["BranchName"].Success)
            buildMetaDataBranch = parsed.Groups["BranchName"].Value;

        string? buildMetaDataSha = null;
        if (parsed.Groups["Sha"].Success)
            buildMetaDataSha = parsed.Groups["Sha"].Value;

        string? buildMetaDataOtherMetaData = null;
        if (parsed.Groups["Other"].Success && !parsed.Groups["Other"].Value.IsNullOrEmpty())
            buildMetaDataOtherMetaData = parsed.Groups["Other"].Value.TrimStart('.');

        return new()
        {
            CommitsSinceTag = buildMetaDataCommitsSinceTag,
            VersionSourceDistance = buildMetaDataVersionSourceDistance ?? 0,
            Branch = buildMetaDataBranch,
            Sha = buildMetaDataSha,
            OtherMetaData = buildMetaDataOtherMetaData
        };
    }

    private static string FormatMetaDataPart(string value)
    {
        if (!value.IsNullOrEmpty())
            value = RegexPatterns.SemanticVersion.FormatBuildMetaDataRegex.Replace(value, "-");
        return value;
    }
}
