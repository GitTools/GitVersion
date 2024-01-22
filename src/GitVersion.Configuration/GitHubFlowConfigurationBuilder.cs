using GitVersion.VersionCalculation;

namespace GitVersion.Configuration;

internal sealed class GitHubFlowConfigurationBuilder : ConfigurationBuilderBase<GitHubFlowConfigurationBuilder>
{
    public static GitHubFlowConfigurationBuilder New => new();

    private GitHubFlowConfigurationBuilder()
    {
        WithConfiguration(new GitVersionConfiguration
        {
            AssemblyFileVersioningScheme = ConfigurationConstants.DefaultAssemblyFileVersioningScheme,
            AssemblyVersioningScheme = ConfigurationConstants.DefaultAssemblyVersioningScheme,
            CommitDateFormat = ConfigurationConstants.DefaultCommitDateFormat,
            MajorVersionBumpMessage = IncrementStrategyFinder.DefaultMajorPattern,
            MinorVersionBumpMessage = IncrementStrategyFinder.DefaultMinorPattern,
            NoBumpMessage = IncrementStrategyFinder.DefaultNoBumpPattern,
            PatchVersionBumpMessage = IncrementStrategyFinder.DefaultPatchPattern,
            SemanticVersionFormat = ConfigurationConstants.DefaultSemanticVersionFormat,
            TagPrefix = ConfigurationConstants.DefaultTagPrefix,
            VersionInBranchPattern = ConfigurationConstants.DefaultVersionInBranchPattern,
            TagPreReleaseWeight = ConfigurationConstants.DefaultTagPreReleaseWeight,
            UpdateBuildNumber = ConfigurationConstants.DefaultUpdateBuildNumber,
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

        WithBranch(MainBranch.Name).WithConfiguration(new BranchConfiguration
        {
            Increment = IncrementStrategy.Patch,
            RegularExpression = MainBranch.RegexPattern,
            SourceBranches = [this.ReleaseBranch.Name],
            Label = string.Empty,
            PreventIncrementOfMergedBranchVersion = true,
            TrackMergeTarget = false,
            TracksReleaseBranches = false,
            IsMainline = true,
            IsReleaseBranch = false,
            PreReleaseWeight = 55000
        });

        WithBranch(ReleaseBranch.Name).WithConfiguration(new BranchConfiguration
        {
            Increment = IncrementStrategy.None,
            RegularExpression = ReleaseBranch.RegexPattern,
            VersioningMode = VersioningMode.ManualDeployment,
            SourceBranches =
            [
                this.MainBranch.Name,
                this.ReleaseBranch.Name
            ],
            Label = "beta",
            PreventIncrementOfMergedBranchVersion = true,
            TrackMergeTarget = false,
            TracksReleaseBranches = false,
            IsMainline = false,
            IsReleaseBranch = true,
            PreReleaseWeight = 30000
        });

        WithBranch(FeatureBranch.Name).WithConfiguration(new BranchConfiguration
        {
            Increment = IncrementStrategy.Inherit,
            RegularExpression = FeatureBranch.RegexPattern,
            VersioningMode = VersioningMode.ManualDeployment,
            SourceBranches =
            [
                this.MainBranch.Name,
                this.ReleaseBranch.Name,
                this.FeatureBranch.Name
            ],
            Label = ConfigurationConstants.BranchNamePlaceholder,
            PreReleaseWeight = 30000
        });

        WithBranch(PullRequestBranch.Name).WithConfiguration(new BranchConfiguration
        {
            Increment = IncrementStrategy.Inherit,
            RegularExpression = PullRequestBranch.RegexPattern,
            VersioningMode = VersioningMode.ContinuousDelivery,
            SourceBranches =
            [
                this.MainBranch.Name,
                this.ReleaseBranch.Name,
                this.FeatureBranch.Name
            ],
            Label = "PullRequest",
            LabelNumberPattern = ConfigurationConstants.DefaultLabelNumberPattern,
            PreReleaseWeight = 30000
        });

        WithBranch(UnknownBranch.Name).WithConfiguration(new BranchConfiguration
        {
            RegularExpression = UnknownBranch.RegexPattern,
            Label = ConfigurationConstants.BranchNamePlaceholder,
            VersioningMode = VersioningMode.ManualDeployment,
            Increment = IncrementStrategy.Inherit,
            SourceBranches =
            [
                this.MainBranch.Name,
                this.ReleaseBranch.Name,
                this.FeatureBranch.Name,
                this.PullRequestBranch.Name
            ]
        });
    }
}
