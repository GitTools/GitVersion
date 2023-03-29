using GitVersion.Extensions;
using GitVersion.VersionCalculation;

namespace GitVersion.Configuration;

internal sealed class GitHubFlowConfigurationBuilder : ConfigurationBuilderBase<GitHubFlowConfigurationBuilder>
{
    public static GitHubFlowConfigurationBuilder New => new();

    private GitHubFlowConfigurationBuilder()
    {
        WithConfiguration(new GitVersionConfiguration()
        {
            AssemblyFileVersioningScheme = AssemblyFileVersioningScheme.MajorMinorPatch,
            AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatch,
            CommitDateFormat = "yyyy-MM-dd",
            MajorVersionBumpMessage = IncrementStrategyFinder.DefaultMajorPattern,
            MinorVersionBumpMessage = IncrementStrategyFinder.DefaultMinorPattern,
            NoBumpMessage = IncrementStrategyFinder.DefaultNoBumpPattern,
            PatchVersionBumpMessage = IncrementStrategyFinder.DefaultPatchPattern,
            SemanticVersionFormat = SemanticVersionFormat.Strict,
            LabelPrefix = ConfigurationConstants.DefaultLabelPrefix,
            LabelPreReleaseWeight = 60000,
            UpdateBuildNumber = true,
            VersioningMode = VersioningMode.ContinuousDelivery,
            RegularExpression = string.Empty,
            Label = ConfigurationConstants.BranchNamePlaceholder,
            Increment = IncrementStrategy.Inherit,
            CommitMessageIncrementing = CommitMessageIncrementMode.Enabled,
            PreventIncrementOfMergedBranchVersion = false,
            TrackMergeTarget = false,
            TrackMergeMessage = true,
            TracksReleaseBranches = false,
            IsReleaseBranch = false,
            IsMainline = false
        });

        WithBranch(MainBranch.Name).WithConfiguration(new BranchConfiguration()
        {
            Increment = IncrementStrategy.Patch,
            RegularExpression = MainBranch.RegexPattern,
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

        WithBranch(ReleaseBranch.Name).WithConfiguration(new BranchConfiguration()
        {
            Increment = IncrementStrategy.None,
            RegularExpression = ReleaseBranch.RegexPattern,
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

        WithBranch(FeatureBranch.Name).WithConfiguration(new BranchConfiguration()
        {
            Increment = IncrementStrategy.Inherit,
            RegularExpression = FeatureBranch.RegexPattern,
            VersioningMode = VersioningMode.ContinuousDelivery,
            SourceBranches = new HashSet<string> {
                MainBranch.Name,
                ReleaseBranch.Name,
                FeatureBranch.Name
            },
            Label = ConfigurationConstants.BranchNamePlaceholder,
            PreReleaseWeight = 30000
        });

        WithBranch(PullRequestBranch.Name).WithConfiguration(new BranchConfiguration()
        {
            Increment = IncrementStrategy.Inherit,
            RegularExpression = PullRequestBranch.RegexPattern,
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

        WithBranch(UnknownBranch.Name).WithConfiguration(new BranchConfiguration()
        {
            RegularExpression = UnknownBranch.RegexPattern,
            Label = ConfigurationConstants.BranchNamePlaceholder,
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
}
