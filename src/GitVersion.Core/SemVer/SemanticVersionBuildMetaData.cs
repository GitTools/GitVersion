using System.Globalization;
using GitVersion.Core;
using GitVersion.Extensions;
using GitVersion.Helpers;

namespace GitVersion;

public class SemanticVersionBuildMetaData : IFormattable, IEquatable<SemanticVersionBuildMetaData?>
{
    public static readonly SemanticVersionBuildMetaData Empty = new();

    private static readonly LambdaEqualityHelper<SemanticVersionBuildMetaData> EqualityHelper =
        new(x => x.CommitsSinceTag, x => x.Branch, x => x.Sha);

    public long? CommitsSinceTag { get; init; }

    public string? Branch { get; init; }

    public string? Sha { get; init; }

    public string? ShortSha { get; init; }

    public string? OtherMetaData { get; init; }

    public DateTimeOffset? CommitDate { get; init; }

    public SemanticVersion? VersionSourceSemVer { get; init; }

    public string? VersionSourceSha { get; init; }

    public long CommitsSinceVersionSource => VersionSourceDistance;

    public long VersionSourceDistance { get; init; }

    public long UncommittedChanges { get; init; }

    public VersionField VersionSourceIncrement { get; init; }

    public SemanticVersionBuildMetaData()
    {
    }

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
        this.Sha = commitSha;
        this.ShortSha = commitShortSha;
        this.CommitsSinceTag = commitsSinceTag;
        this.Branch = branch;
        this.CommitDate = commitDate;
        this.OtherMetaData = otherMetadata;
        this.VersionSourceIncrement = versionSourceIncrement;
        this.VersionSourceSemVer = versionSourceSemVer;
        this.VersionSourceSha = versionSourceSha;
        this.VersionSourceDistance = commitsSinceTag ?? 0;
        this.UncommittedChanges = numberOfUnCommittedChanges;
    }

    public SemanticVersionBuildMetaData(SemanticVersionBuildMetaData buildMetaData)
    {
        buildMetaData.NotNull();

        this.Sha = buildMetaData.Sha;
        this.ShortSha = buildMetaData.ShortSha;
        this.CommitsSinceTag = buildMetaData.CommitsSinceTag;
        this.Branch = buildMetaData.Branch;
        this.CommitDate = buildMetaData.CommitDate;
        this.OtherMetaData = buildMetaData.OtherMetaData;
        this.VersionSourceSemVer = buildMetaData.VersionSourceSemVer;
        this.VersionSourceSha = buildMetaData.VersionSourceSha;
        this.VersionSourceDistance = buildMetaData.VersionSourceDistance;
        this.UncommittedChanges = buildMetaData.UncommittedChanges;
        this.VersionSourceIncrement = buildMetaData.VersionSourceIncrement;
    }

    public override bool Equals(object? obj) => Equals(obj as SemanticVersionBuildMetaData);

    public bool Equals(SemanticVersionBuildMetaData? other) => EqualityHelper.Equals(this, other);

    public override int GetHashCode() => EqualityHelper.GetHashCode(this);

    public override string ToString() => ToString("b");

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
            "b" => $"{this.CommitsSinceTag}",
            "s" => $"{this.CommitsSinceTag}{(this.Sha.IsNullOrEmpty() ? null : ".Sha." + this.Sha)}".TrimStart('.'),
            "f" => $"{this.CommitsSinceTag}{(this.Branch.IsNullOrEmpty() ? null : ".Branch." + FormatMetaDataPart(this.Branch))}{(this.Sha.IsNullOrEmpty() ? null : ".Sha." + this.Sha)}{(this.OtherMetaData.IsNullOrEmpty() ? null : "." + FormatMetaDataPart(this.OtherMetaData))}".TrimStart('.'),
            _ => throw new FormatException($"Unknown format '{format}'.")
        };
    }

    public static bool operator ==(SemanticVersionBuildMetaData? left, SemanticVersionBuildMetaData? right) =>
        Equals(left, right);

    public static bool operator !=(SemanticVersionBuildMetaData? left, SemanticVersionBuildMetaData? right) =>
        !Equals(left, right);

    public static implicit operator string?(SemanticVersionBuildMetaData? preReleaseTag) => preReleaseTag?.ToString();

    public static implicit operator SemanticVersionBuildMetaData(string preReleaseTag) => Parse(preReleaseTag);

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
