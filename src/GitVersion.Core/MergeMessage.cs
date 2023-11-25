using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion;

public class MergeMessage
{
    private static readonly IList<MergeMessageFormat> DefaultFormats = new List<MergeMessageFormat>
    {
        new("Default", @"^Merge (branch|tag) '(?<SourceBranch>[^']*)'(?: into (?<TargetBranch>[^\s]*))*"),
        new("SmartGit",  @"^Finish (?<SourceBranch>[^\s]*)(?: into (?<TargetBranch>[^\s]*))*"),
        new("BitBucketPull", @"^Merge pull request #(?<PullRequestNumber>\d+) (from|in) (?<Source>.*) from (?<SourceBranch>[^\s]*) to (?<TargetBranch>[^\s]*)"),
        new("BitBucketPullv7", @"^Pull request #(?<PullRequestNumber>\d+).*\r?\n\r?\nMerge in (?<Source>.*) from (?<SourceBranch>[^\s]*) to (?<TargetBranch>[^\s]*)"),
        new("BitBucketCloudPull", @"^Merged in (?<SourceBranch>[^\s]*) \(pull request #(?<PullRequestNumber>\d+)\)"),
        new("GitHubPull", @"^Merge pull request #(?<PullRequestNumber>\d+) (from|in) (?:[^\s\/]+\/)?(?<SourceBranch>[^\s]*)(?: into (?<TargetBranch>[^\s]*))*"),
        new("RemoteTracking", @"^Merge remote-tracking branch '(?<SourceBranch>[^\s]*)'(?: into (?<TargetBranch>[^\s]*))*")
    };

    public MergeMessage(string mergeMessage, IGitVersionConfiguration configuration)
    {
        mergeMessage.NotNull();

        if (mergeMessage == string.Empty) return;

        // Concatenate configuration formats with the defaults.
        // Ensure configurations are processed first.
        var allFormats = configuration.MergeMessageFormats
            .Select(x => new MergeMessageFormat(x.Key, x.Value))
            .Concat(DefaultFormats);

        foreach (var format in allFormats)
        {
            var match = format.Pattern.Match(mergeMessage);
            if (!match.Success)
                continue;

            FormatName = format.Name;
            var sourceBranch = match.Groups["SourceBranch"].Value;
            MergedBranch = GetMergedBranchName(sourceBranch);

            if (match.Groups["TargetBranch"].Success)
            {
                TargetBranch = match.Groups["TargetBranch"].Value;
            }

            if (int.TryParse(match.Groups["PullRequestNumber"].Value, out var pullNumber))
            {
                PullRequestNumber = pullNumber;
            }

            Version = ParseVersion(
                configuration.VersionInBranchRegex, configuration.TagPrefix, configuration.SemanticVersionFormat
            );

            break;
        }
    }

    public string? FormatName { get; }
    public string? TargetBranch { get; }
    public ReferenceName? MergedBranch { get; }

    public bool IsMergedPullRequest => PullRequestNumber != null;
    public int? PullRequestNumber { get; }
    public SemanticVersion? Version { get; }

    private SemanticVersion? ParseVersion(Regex versionInBranchRegex, string? tagPrefix, SemanticVersionFormat format)
    {
        if (MergedBranch?.TryGetSemanticVersion(out var result, versionInBranchRegex, tagPrefix, format) == true)
            return result.Value;
        return null;
    }

    private class MergeMessageFormat
    {
        public MergeMessageFormat(string name, string pattern)
        {
            Name = name;
            Pattern = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public string Name { get; }

        public Regex Pattern { get; }
    }

    private ReferenceName GetMergedBranchName(string mergedBranch)
    {
        if (FormatName == "RemoteTracking" && !mergedBranch.StartsWith(ReferenceName.RemoteTrackingBranchPrefix))
        {
            mergedBranch = $"{ReferenceName.RemoteTrackingBranchPrefix}{mergedBranch}";
        }
        return ReferenceName.FromBranchName(mergedBranch);
    }

    public static bool TryParse(
        [NotNullWhen(true)] out MergeMessage? mergeMessage, ICommit mergeCommit, IGitVersionConfiguration configuration)
    {
        mergeCommit.NotNull();
        configuration.NotNull();

        mergeMessage = null;

        if (mergeCommit.IsMergeCommit)
        {
            mergeMessage = new MergeMessage(mergeCommit.Message, configuration);
        }

        return mergeMessage != null;
    }
}
