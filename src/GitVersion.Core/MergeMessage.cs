using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using GitVersion.Configuration;
using GitVersion.Core;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion;

public class MergeMessage
{
    private static readonly IList<MergeMessageFormat> DefaultFormats =
    [
        new("Default", RegexPatterns.MergeMessage.DefaultMergeMessageRegex),
        new("SmartGit", RegexPatterns.MergeMessage.SmartGitMergeMessageRegex),
        new("BitBucketPull", RegexPatterns.MergeMessage.BitBucketPullMergeMessageRegex),
        new("BitBucketPullv7", RegexPatterns.MergeMessage.BitBucketPullv7MergeMessageRegex),
        new("BitBucketCloudPull", RegexPatterns.MergeMessage.BitBucketCloudPullMergeMessageRegex),
        new("GitHubPull", RegexPatterns.MergeMessage.GitHubPullMergeMessageRegex),
        new("RemoteTracking", RegexPatterns.MergeMessage.RemoteTrackingMergeMessageRegex),
        new("AzureDevOpsPull", RegexPatterns.MergeMessage.AzureDevOpsPullMergeMessageRegex)
    ];

    public MergeMessage(string mergeMessage, IGitVersionConfiguration configuration)
    {
        mergeMessage.NotNull();

        if (mergeMessage == string.Empty) return;

        // Concatenate configuration formats with the defaults.
        // Ensure configurations are processed first.
        var allFormats = configuration.MergeMessageFormats
            .Select(x => new MergeMessageFormat(x.Key, RegexPatterns.Cache.GetOrAdd(x.Value)))
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

    private class MergeMessageFormat(string name, Regex pattern)
    {
        public string Name { get; } = name;

        public Regex Pattern { get; } = pattern;
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
        ICommit mergeCommit, IGitVersionConfiguration configuration, [NotNullWhen(true)] out MergeMessage? mergeMessage)
    {
        mergeCommit.NotNull();
        configuration.NotNull();

        mergeMessage = null;

        var mergedBranch = new MergeMessage(mergeCommit.Message, configuration).MergedBranch;
        var isReleaseBranch = mergedBranch is not null && configuration.IsReleaseBranch(mergedBranch);
        var isValidMergeCommit = mergeCommit.IsMergeCommit() || isReleaseBranch;

        if (isValidMergeCommit)
        {
            mergeMessage = new(mergeCommit.Message, configuration);
        }

        return isValidMergeCommit;
    }
}
