using System.Globalization;
using System.Text.RegularExpressions;
using GitVersion.Extensions;
using GitVersion.Helpers;

namespace GitVersion;

public class SemanticVersionBuildMetaData : IFormattable, IEquatable<SemanticVersionBuildMetaData?>
{
    private static readonly Regex ParseRegex = new(
        @"(?<BuildNumber>\d+)?(\.?Branch(Name)?\.(?<BranchName>[^\.]+))?(\.?Sha?\.(?<Sha>[^\.]+))?(?<Other>.*)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly LambdaEqualityHelper<SemanticVersionBuildMetaData> EqualityHelper =
        new(x => x.CommitsSinceTag, x => x.Branch, x => x.Sha);

    public int? CommitsSinceTag;
    public string? Branch;
    public string? Sha;
    public string? ShortSha;
    public string? OtherMetaData;
    public DateTimeOffset? CommitDate;
    public string? VersionSourceSha;
    public int? CommitsSinceVersionSource;
    public int UncommittedChanges;

    public SemanticVersionBuildMetaData()
    {
    }

    public SemanticVersionBuildMetaData(string? versionSourceSha, int? commitsSinceTag, string? branch, string? commitSha, string? commitShortSha, DateTimeOffset? commitDate, int numbeerOfUncommitedChanges, string? otherMetadata = null)
    {
        this.Sha = commitSha;
        this.ShortSha = commitShortSha;
        this.CommitsSinceTag = commitsSinceTag;
        this.Branch = branch;
        this.CommitDate = commitDate;
        this.OtherMetaData = otherMetadata;
        this.VersionSourceSha = versionSourceSha;
        this.CommitsSinceVersionSource = commitsSinceTag ?? 0;
        this.UncommittedChanges = numbeerOfUncommitedChanges;
    }

    public SemanticVersionBuildMetaData(SemanticVersionBuildMetaData? buildMetaData)
    {
        this.Sha = buildMetaData?.Sha;
        this.ShortSha = buildMetaData?.ShortSha;
        this.CommitsSinceTag = buildMetaData?.CommitsSinceTag;
        this.Branch = buildMetaData?.Branch;
        this.CommitDate = buildMetaData?.CommitDate;
        this.OtherMetaData = buildMetaData?.OtherMetaData;
        this.VersionSourceSha = buildMetaData?.VersionSourceSha;
        this.CommitsSinceVersionSource = buildMetaData?.CommitsSinceVersionSource;
        this.UncommittedChanges = buildMetaData?.UncommittedChanges ?? 0;
    }

    public override bool Equals(object obj) => Equals(obj as SemanticVersionBuildMetaData);

    public bool Equals(SemanticVersionBuildMetaData? other) => EqualityHelper.Equals(this, other);

    public override int GetHashCode() => EqualityHelper.GetHashCode(this);

    public override string ToString() => ToString("b");

    public string ToString(string format) => ToString(format, CultureInfo.CurrentCulture);

    /// <summary>
    /// <para>b - Formats just the build number</para>
    /// <para>s - Formats the build number and the Git Sha</para>
    /// <para>f - Formats the full build metadata</para>
    /// <para>p - Formats the padded build number. Can specify an integer for padding, default is 4. (i.e., p5)</para>
    /// </summary>
    public string ToString(string format, IFormatProvider formatProvider)
    {
        if (formatProvider != null)
        {
            if (formatProvider.GetFormat(GetType()) is ICustomFormatter formatter)
                return formatter.Format(format, this, formatProvider);
        }

        if (format.IsNullOrEmpty())
            format = "b";

        format = format.ToLower();
        if (format.StartsWith("p", StringComparison.Ordinal))
        {
            // Handle format
            var padding = 4;
            if (format.Length > 1)
            {
                // try to parse
                if (int.TryParse(format.Substring(1), out var p))
                {
                    padding = p;
                }
            }

            return this.CommitsSinceTag != null ? this.CommitsSinceTag.Value.ToString("D" + padding) : string.Empty;
        }

        return format.ToLower() switch
        {
            "b" => this.CommitsSinceTag.ToString(),
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
        var semanticVersionBuildMetaData = new SemanticVersionBuildMetaData();
        if (buildMetaData.IsNullOrEmpty())
            return semanticVersionBuildMetaData;

        var parsed = ParseRegex.Match(buildMetaData);

        if (parsed.Groups["BuildNumber"].Success)
        {
            semanticVersionBuildMetaData.CommitsSinceTag = int.Parse(parsed.Groups["BuildNumber"].Value);
            semanticVersionBuildMetaData.CommitsSinceVersionSource = semanticVersionBuildMetaData.CommitsSinceTag ?? 0;
        }

        if (parsed.Groups["BranchName"].Success)
            semanticVersionBuildMetaData.Branch = parsed.Groups["BranchName"].Value;

        if (parsed.Groups["Sha"].Success)
            semanticVersionBuildMetaData.Sha = parsed.Groups["Sha"].Value;

        if (parsed.Groups["Other"].Success && !parsed.Groups["Other"].Value.IsNullOrEmpty())
            semanticVersionBuildMetaData.OtherMetaData = parsed.Groups["Other"].Value.TrimStart('.');

        return semanticVersionBuildMetaData;
    }

    private static string FormatMetaDataPart(string value)
    {
        if (!value.IsNullOrEmpty())
            value = Regex.Replace(value, "[^0-9A-Za-z-.]", "-");
        return value;
    }
}
