using GitVersion.Extensions;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests.Helpers;

internal sealed class GitHubFlowConfigurationBuilder : TestConfigurationBuilderBase<GitHubFlowConfigurationBuilder>
{
    public static GitHubFlowConfigurationBuilder New => new();

    private GitHubFlowConfigurationBuilder()
    {
        WithConfiguration(new()
        {
            AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatch,
            AssemblyFileVersioningScheme = AssemblyFileVersioningScheme.MajorMinorPatch,
            TagPrefix = "[vV]",
            VersioningMode = VersioningMode.ContinuousDelivery,
            ContinuousDeploymentFallbackTag = "ci",
            MajorVersionBumpMessage = IncrementStrategyFinder.DefaultMajorPattern,
            MinorVersionBumpMessage = IncrementStrategyFinder.DefaultMinorPattern,
            PatchVersionBumpMessage = IncrementStrategyFinder.DefaultPatchPattern,
            NoBumpMessage = IncrementStrategyFinder.DefaultNoBumpPattern,
            CommitMessageIncrementing = CommitMessageIncrementMode.Enabled,
            CommitDateFormat = "yyyy-MM-dd",
            UpdateBuildNumber = true,
            SemanticVersionFormat = SemanticVersionFormat.Strict,
            HandleDetachedBranch = false,
            TagPreReleaseWeight = 60000,
            Increment = IncrementStrategy.Inherit
        });

        WithBranch(MainBranch.Name).WithConfiguration(new()
        {
            VersioningMode = VersioningMode.ContinuousDelivery,
            Increment = IncrementStrategy.Patch,
            Regex = MainBranch.RegexPattern,
            SourceBranches = new HashSet<string> {
                ReleaseBranch.Name
            },
            Tag = string.Empty,
            PreventIncrementOfMergedBranchVersion = true,
            TrackMergeTarget = false,
            TracksReleaseBranches = false,
            IsMainline = true,
            IsReleaseBranch = false,
            PreReleaseWeight = 55000
        });

        WithBranch(ReleaseBranch.Name).WithConfiguration(new()
        {
            VersioningMode = VersioningMode.ContinuousDelivery,
            Increment = IncrementStrategy.None,
            Regex = ReleaseBranch.RegexPattern,
            SourceBranches = new HashSet<string> {
                MainBranch.Name,
                ReleaseBranch.Name
            },
            Tag = "beta",
            PreventIncrementOfMergedBranchVersion = true,
            TrackMergeTarget = false,
            TracksReleaseBranches = false,
            IsMainline = false,
            IsReleaseBranch = true,
            PreReleaseWeight = 30000
        });

        WithBranch(FeatureBranch.Name).WithConfiguration(new()
        {
            VersioningMode = VersioningMode.ContinuousDelivery,
            Increment = IncrementStrategy.Inherit,
            Regex = FeatureBranch.RegexPattern,
            SourceBranches = new HashSet<string> {
                MainBranch.Name,
                ReleaseBranch.Name,
                FeatureBranch.Name
            },
            Tag = "{BranchName}",
            PreReleaseWeight = 30000
        });

        WithBranch(PullRequestBranch.Name).WithConfiguration(new()
        {
            VersioningMode = VersioningMode.ContinuousDelivery,
            Increment = IncrementStrategy.Inherit,
            Regex = PullRequestBranch.RegexPattern,
            SourceBranches = new HashSet<string> {
                MainBranch.Name,
                ReleaseBranch.Name,
                FeatureBranch.Name
            },
            Tag = "PullRequest",
            TagNumberPattern = @"[/-](?<number>\d+)",
            PreReleaseWeight = 30000
        });
    }

    public static BranchMetaData MainBranch = new()
    {
        Name = "main",
        RegexPattern = "^master$|^main$"
    };

    public static BranchMetaData ReleaseBranch = new()
    {
        Name = "release",
        RegexPattern = "^releases?[/-]"
    };

    public static BranchMetaData FeatureBranch = new()
    {
        Name = "feature",
        RegexPattern = "^features?[/-]"
    };

    public static BranchMetaData PullRequestBranch = new()
    {
        Name = "pull-request",
        RegexPattern = @"^(pull|pull\-requests|pr)[/-]"
    };
}
