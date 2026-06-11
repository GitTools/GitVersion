using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using GitVersion.Configuration;
using GitVersion.Core;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion;

/// <summary>Parses the message of a merge commit to extract the merged branch name, target branch, pull-request number, and embedded semantic version.</summary>
public class MergeMessage
{
    private static readonly IList<(string Name, Regex Pattern)> DefaultFormats =
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

    /// <summary>Parses <paramref name="mergeMessage"/> using the configured and built-in merge message formats.</summary>
    public MergeMessage(string mergeMessage, IGitVersionConfiguration configuration)
    {
        mergeMessage.NotNull();

        if (mergeMessage.Length == 0) return;

        // Concatenate configuration formats with the defaults.
        // Ensure configurations are processed first.
        var allFormats = configuration.MergeMessageFormats
            .Select(x => (Name: x.Key, Pattern: RegexPatterns.Cache.GetOrAdd(x.Value)))
            .Concat(DefaultFormats);

        foreach (var (Name, Pattern) in allFormats)
        {
            var match = Pattern.Match(mergeMessage);
            if (!match.Success)
                continue;

            FormatName = Name;
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

            Version = MergedBranch?.TryGetSemanticVersion(configuration, out var result) == true ? result.Value : null;

            break;
        }
    }

    /// <summary>Gets the name of the merge message format pattern that was matched, or <see langword="null"/> if none matched.</summary>
    public string? FormatName { get; }

    /// <summary>Gets the name of the branch that was the merge target, or <see langword="null"/> if not captured.</summary>
    public string? TargetBranch { get; }

    /// <summary>Gets the reference name of the branch that was merged in, or <see langword="null"/> if not captured.</summary>
    public ReferenceName? MergedBranch { get; }

    /// <summary>Gets a value indicating whether this merge message represents a merged pull request.</summary>
    public bool IsMergedPullRequest => PullRequestNumber != null;

    /// <summary>Gets the pull-request number extracted from the merge message, or <see langword="null"/> if this is not a pull-request merge.</summary>
    public int? PullRequestNumber { get; }

    /// <summary>Gets the semantic version embedded in the merged branch name, or <see langword="null"/> if none was found.</summary>
    public SemanticVersion? Version { get; }

    private ReferenceName GetMergedBranchName(string mergedBranch)
    {
        if (FormatName == "RemoteTracking" && !mergedBranch.StartsWith(ReferenceName.RemoteTrackingBranchPrefix))
        {
            mergedBranch = $"{ReferenceName.RemoteTrackingBranchPrefix}{mergedBranch}";
        }
        return ReferenceName.FromBranchName(mergedBranch);
    }

    /// <summary>Attempts to parse a valid merge message from <paramref name="mergeCommit"/>, setting <paramref name="mergeMessage"/> when successful.</summary>
    public static bool TryParse(
        ICommit mergeCommit, IGitVersionConfiguration configuration, [NotNullWhen(true)] out MergeMessage? mergeMessage)
    {
        mergeCommit.NotNull();
        configuration.NotNull();

        mergeMessage = null;

        var mergedBranch = new MergeMessage(mergeCommit.Message, configuration).MergedBranch;
        var isReleaseBranch = mergedBranch is not null && configuration.IsReleaseBranch(mergedBranch);
        var isValidMergeCommit = mergeCommit.IsMergeCommit || isReleaseBranch;

        if (isValidMergeCommit)
        {
            mergeMessage = new MergeMessage(mergeCommit.Message, configuration);
        }

        return isValidMergeCommit;
    }
}
