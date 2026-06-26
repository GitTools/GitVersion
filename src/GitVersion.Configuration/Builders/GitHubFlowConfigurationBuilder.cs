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
            MajorVersionBumpMessage = RegexPatterns.VersionCalculation.DefaultMajorRegexPattern,
            MinorVersionBumpMessage = RegexPatterns.VersionCalculation.DefaultMinorRegexPattern,
            NoBumpMessage = RegexPatterns.VersionCalculation.DefaultNoBumpRegexPattern,
            PatchVersionBumpMessage = RegexPatterns.VersionCalculation.DefaultPatchRegexPattern,
            SemanticVersionFormat = ConfigurationConstants.DefaultSemanticVersionFormat,
            VersionStrategies = ConfigurationConstants.DefaultVersionStrategies,
            TagPrefixPattern = RegexPatterns.Configuration.DefaultTagPrefixRegexPattern,
            VersionInBranchPattern = RegexPatterns.Configuration.DefaultVersionInBranchRegexPattern,
            TagPreReleaseWeight = ConfigurationConstants.DefaultTagPreReleaseWeight,
            UpdateBuildNumber = ConfigurationConstants.DefaultUpdateBuildNumber,
            DeploymentMode = DeploymentMode.ContinuousDelivery,
            Label = ConfigurationConstants.BranchNamePlaceholder,
            Increment = IncrementStrategy.Inherit,
            PreventIncrement = new PreventIncrementConfiguration
            {
                OfMergedBranch = false,
                WhenBranchMerged = false,
                WhenCurrentCommitTagged = true
            },
            TrackMergeTarget = false,
            TrackMergeMessage = true,
            CommitMessageIncrementing = CommitMessageIncrementMode.Enabled,
            RegularExpression = string.Empty,
            TracksReleaseBranches = false,
            IsReleaseBranch = false,
            IsMainBranch = false
        });

        WithBranch(this.MainBranch.Name).WithConfiguration(new BranchConfiguration
        {
            Label = string.Empty,
            Increment = IncrementStrategy.Patch,
            PreventIncrement = new PreventIncrementConfiguration
            {
                OfMergedBranch = true
            },
            TrackMergeTarget = false,
            TrackMergeMessage = true,
            RegularExpression = this.MainBranch.RegexPattern,
            SourceBranches = [],
            TracksReleaseBranches = false,
            IsReleaseBranch = false,
            IsMainBranch = true,
            PreReleaseWeight = 55000
        });

        WithBranch(this.ReleaseBranch.Name).WithConfiguration(new BranchConfiguration
        {
            DeploymentMode = DeploymentMode.ManualDeployment,
            Label = "beta",
            Increment = IncrementStrategy.Patch,
            PreventIncrement = new PreventIncrementConfiguration
            {
                OfMergedBranch = true,
                WhenBranchMerged = false,
                WhenCurrentCommitTagged = false
            },
            TrackMergeTarget = false,
            TrackMergeMessage = true,
            RegularExpression = this.ReleaseBranch.RegexPattern,
            SourceBranches =
            [
                this.MainBranch.Name
            ],
            TracksReleaseBranches = false,
            IsReleaseBranch = true,
            IsMainBranch = false,
            PreReleaseWeight = 30000
        });

        WithBranch(this.FeatureBranch.Name).WithConfiguration(new BranchConfiguration
        {
            DeploymentMode = DeploymentMode.ManualDeployment,
            Label = ConfigurationConstants.BranchNamePlaceholder,
            Increment = IncrementStrategy.Inherit,
            PreventIncrement = new PreventIncrementConfiguration
            {
                WhenCurrentCommitTagged = false
            },
            RegularExpression = this.FeatureBranch.RegexPattern,
            SourceBranches =
            [
                this.MainBranch.Name,
                this.ReleaseBranch.Name
            ],
            TrackMergeMessage = true,
            IsMainBranch = false,
            PreReleaseWeight = 30000
        });

        WithBranch(this.PullRequestBranch.Name).WithConfiguration(new BranchConfiguration
        {
            DeploymentMode = DeploymentMode.ContinuousDelivery,
            Label = $"PullRequest{ConfigurationConstants.PullRequestNumberPlaceholder}",
            Increment = IncrementStrategy.Inherit,
            PreventIncrement = new PreventIncrementConfiguration
            {
                OfMergedBranch = true,
                WhenCurrentCommitTagged = false
            },
            RegularExpression = this.PullRequestBranch.RegexPattern,
            SourceBranches =
            [
                this.MainBranch.Name,
                this.ReleaseBranch.Name,
                this.FeatureBranch.Name
            ],
            TrackMergeMessage = true,
            PreReleaseWeight = 30000
        });

        WithBranch(this.UnknownBranch.Name).WithConfiguration(new BranchConfiguration
        {
            DeploymentMode = DeploymentMode.ManualDeployment,
            Label = ConfigurationConstants.BranchNamePlaceholder,
            Increment = IncrementStrategy.Inherit,
            PreventIncrement = new PreventIncrementConfiguration
            {
                WhenCurrentCommitTagged = false
            },
            RegularExpression = this.UnknownBranch.RegexPattern,
            SourceBranches =
            [
                this.MainBranch.Name,
                this.ReleaseBranch.Name,
                this.FeatureBranch.Name,
                this.PullRequestBranch.Name
            ],
            TrackMergeMessage = false,
            IsMainBranch = false
        });
    }
}
