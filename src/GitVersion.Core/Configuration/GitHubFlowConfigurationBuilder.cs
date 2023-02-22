using GitVersion.Extensions;
using GitVersion.VersionCalculation;

namespace GitVersion.Configuration;

internal sealed class GitHubFlowConfigurationBuilder : ConfigurationBuilderBase<GitHubFlowConfigurationBuilder>
{
    public static GitHubFlowConfigurationBuilder New => new();

    private GitHubFlowConfigurationBuilder()
    {
        WithConfiguration(new()
        {
            AssemblyFileVersioningScheme = AssemblyFileVersioningScheme.MajorMinorPatch,
            AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatch,
            CommitDateFormat = "yyyy-MM-dd",
            MajorVersionBumpMessage = IncrementStrategyFinder.DefaultMajorPattern,
            MinorVersionBumpMessage = IncrementStrategyFinder.DefaultMinorPattern,
            NoBumpMessage = IncrementStrategyFinder.DefaultNoBumpPattern,
            PatchVersionBumpMessage = IncrementStrategyFinder.DefaultPatchPattern,
            SemanticVersionFormat = SemanticVersionFormat.Strict,
            LabelPrefix = GitVersionConfiguration.DefaultLabelPrefix,
            LabelPreReleaseWeight = 60000,
            UpdateBuildNumber = true,
            VersioningMode = VersioningMode.ContinuousDelivery,
            Regex = string.Empty,
            Label = "{BranchName}",
            Increment = IncrementStrategy.Inherit,
            CommitMessageIncrementing = CommitMessageIncrementMode.Enabled,
            PreventIncrementOfMergedBranchVersion = false,
            TrackMergeTarget = false,
            TracksReleaseBranches = false,
            IsReleaseBranch = false,
            IsMainline = false
        });

        WithBranch(MainBranch.Name).WithConfiguration(new()
        {
            Increment = IncrementStrategy.Patch,
            Regex = MainBranch.RegexPattern,
            SourceBranches = new HashSet<string> {
                ReleaseBranch.Name
            },
            Label = string.Empty,
            PreventIncrementOfMergedBranchVersion = true,
            TrackMergeTarget = false,
            TracksReleaseBranches = false,
            IsMainline = true,
            IsReleaseBranch = false,
            PreReleaseWeight = 55000
        });

        WithBranch(ReleaseBranch.Name).WithConfiguration(new()
        {
            Increment = IncrementStrategy.None,
            Regex = ReleaseBranch.RegexPattern,
            SourceBranches = new HashSet<string> {
                MainBranch.Name,
                ReleaseBranch.Name
            },
            Label = "beta",
            PreventIncrementOfMergedBranchVersion = true,
            TrackMergeTarget = false,
            TracksReleaseBranches = false,
            IsMainline = false,
            IsReleaseBranch = true,
            PreReleaseWeight = 30000
        });

        WithBranch(FeatureBranch.Name).WithConfiguration(new()
        {
            Increment = IncrementStrategy.Inherit,
            Regex = FeatureBranch.RegexPattern,
            VersioningMode = VersioningMode.ContinuousDelivery,
            SourceBranches = new HashSet<string> {
                MainBranch.Name,
                ReleaseBranch.Name,
                FeatureBranch.Name
            },
            Label = "{BranchName}",
            PreReleaseWeight = 30000
        });

        WithBranch(PullRequestBranch.Name).WithConfiguration(new()
        {
            Increment = IncrementStrategy.Inherit,
            Regex = PullRequestBranch.RegexPattern,
            VersioningMode = VersioningMode.ContinuousDelivery,
            SourceBranches = new HashSet<string> {
                MainBranch.Name,
                ReleaseBranch.Name,
                FeatureBranch.Name
            },
            Label = "PullRequest",
            LabelNumberPattern = @"[/-](?<number>\d+)",
            PreReleaseWeight = 30000
        });

        WithBranch(UnknownBranch.Name).WithConfiguration(new()
        {
            Regex = UnknownBranch.RegexPattern,
            Label = "{BranchName}",
            VersioningMode = VersioningMode.ContinuousDelivery,
            Increment = IncrementStrategy.Inherit,
            SourceBranches = new HashSet<string> {
                MainBranch.Name,
                ReleaseBranch.Name,
                FeatureBranch.Name,
                PullRequestBranch.Name
            }
        });
    }

    public static readonly BranchMetaData MainBranch = new()
    {
        Name = "main",
        RegexPattern = "^master$|^main$"
    };

    public static readonly BranchMetaData ReleaseBranch = new()
    {
        Name = "release",
        RegexPattern = "^releases?[/-]"
    };

    public static readonly BranchMetaData FeatureBranch = new()
    {
        Name = "feature",
        RegexPattern = "^features?[/-]"
    };

    public static readonly BranchMetaData PullRequestBranch = new()
    {
        Name = "pull-request",
        RegexPattern = @"^(pull|pull\-requests|pr)[/-]"
    };

    public static readonly BranchMetaData UnknownBranch = new()
    {
        Name = "unknown",
        RegexPattern = ".*"
    };
}
