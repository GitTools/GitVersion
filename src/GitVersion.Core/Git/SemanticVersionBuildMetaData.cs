using System.Globalization;
using System.Text.RegularExpressions;
using GitVersion.Extensions;
using GitVersion.Helpers;

namespace GitVersion;

public class SemanticVersionBuildMetaData : IFormattable, IEquatable<SemanticVersionBuildMetaData?>
{
    public static readonly SemanticVersionBuildMetaData Empty = new();

    private static readonly Regex ParseRegex = new(
        @"(?<BuildNumber>\d+)?(\.?Branch(Name)?\.(?<BranchName>[^\.]+))?(\.?Sha?\.(?<Sha>[^\.]+))?(?<Other>.*)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly LambdaEqualityHelper<SemanticVersionBuildMetaData> EqualityHelper =
        new(x => x.CommitsSinceTag, x => x.Branch, x => x.Sha);

    public long? CommitsSinceTag { get; init; }

    public string? Branch { get; init; }

    public string? Sha { get; init; }

    public string? ShortSha { get; init; }

    public string? OtherMetaData { get; init; }

    public DateTimeOffset? CommitDate { get; init; }

    public string? VersionSourceSha { get; init; }

    public long? CommitsSinceVersionSource { get; init; }

    public long UncommittedChanges { get; init; }

    public SemanticVersionBuildMetaData()
    {
    }

    public SemanticVersionBuildMetaData(string? versionSourceSha, int? commitsSinceTag, string? branch, string? commitSha,
        string? commitShortSha, DateTimeOffset? commitDate, int numberOfUnCommittedChanges, string? otherMetadata = null)
    {
        this.Sha = commitSha;
        this.ShortSha = commitShortSha;
        this.CommitsSinceTag = commitsSinceTag;
        this.Branch = branch;
        this.CommitDate = commitDate;
        this.OtherMetaData = otherMetadata;
        this.VersionSourceSha = versionSourceSha;
        this.CommitsSinceVersionSource = commitsSinceTag ?? 0;
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
        this.VersionSourceSha = buildMetaData.VersionSourceSha;
        this.CommitsSinceVersionSource = buildMetaData.CommitsSinceVersionSource;
        this.UncommittedChanges = buildMetaData.UncommittedChanges;
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

        if (string.IsNullOrEmpty(format))
            format = "b";

        format = format.ToLower();
        return format.ToLower() switch
        {
            "b" => $"{this.CommitsSinceTag}",
            "s" => $"{this.CommitsSinceTag}{(string.IsNullOrEmpty(this.Sha) ? null : ".Sha." + this.Sha)}".TrimStart('.'),
            "f" => $"{this.CommitsSinceTag}{(string.IsNullOrEmpty(this.Branch) ? null : ".Branch." + FormatMetaDataPart(this.Branch))}{(string.IsNullOrEmpty(this.Sha) ? null : ".Sha." + this.Sha)}{(string.IsNullOrEmpty(this.OtherMetaData) ? null : "." + FormatMetaDataPart(this.OtherMetaData))}".TrimStart('.'),
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
        if (string.IsNullOrEmpty(buildMetaData))
            return Empty;

        var parsed = ParseRegex.Match(buildMetaData);

        long? buildMetaDataCommitsSinceTag = null;
        long? buildMetaDataCommitsSinceVersionSource = null;
        if (parsed.Groups["BuildNumber"].Success)
        {
            if (long.TryParse(parsed.Groups["BuildNumber"].Value, out var buildNumber))
                buildMetaDataCommitsSinceTag = buildNumber;
            buildMetaDataCommitsSinceVersionSource = buildMetaDataCommitsSinceTag ?? 0;
        }

        string? buildMetaDataBranch = null;
        if (parsed.Groups["BranchName"].Success)
            buildMetaDataBranch = parsed.Groups["BranchName"].Value;

        string? buildMetaDataSha = null;
        if (parsed.Groups["Sha"].Success)
            buildMetaDataSha = parsed.Groups["Sha"].Value;

        string? buildMetaDataOtherMetaData = null;
        if (parsed.Groups["Other"].Success && !string.IsNullOrEmpty(parsed.Groups["Other"].Value))
            buildMetaDataOtherMetaData = parsed.Groups["Other"].Value.TrimStart('.');

        return new()
        {
            CommitsSinceTag = buildMetaDataCommitsSinceTag,
            CommitsSinceVersionSource = buildMetaDataCommitsSinceVersionSource,
            Branch = buildMetaDataBranch,
            Sha = buildMetaDataSha,
            OtherMetaData = buildMetaDataOtherMetaData
        };
    }

    private static string FormatMetaDataPart(string value)
    {
        if (!string.IsNullOrEmpty(value))
            value = Regex.Replace(value, "[^0-9A-Za-z-.]", "-");
        return value;
    }
}
