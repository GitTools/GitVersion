using GitVersion.Core;
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
            MajorVersionBumpMessage = RegexPatterns.VersionCalculation.DefaultMajorPattern,
            MinorVersionBumpMessage = RegexPatterns.VersionCalculation.DefaultMinorPattern,
            NoBumpMessage = RegexPatterns.VersionCalculation.DefaultNoBumpPattern,
            PatchVersionBumpMessage = RegexPatterns.VersionCalculation.DefaultPatchPattern,
            SemanticVersionFormat = ConfigurationConstants.DefaultSemanticVersionFormat,
            VersionStrategies = ConfigurationConstants.DefaultVersionStrategies,
            TagPrefix = RegexPatterns.Configuration.DefaultTagPrefixPattern,
            VersionInBranchPattern = RegexPatterns.Configuration.DefaultVersionInBranchPattern,
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

        WithBranch(MainBranch.Name).WithConfiguration(new BranchConfiguration
        {
            Label = string.Empty,
            Increment = IncrementStrategy.Patch,
            PreventIncrement = new PreventIncrementConfiguration
            {
                OfMergedBranch = true
            },
            TrackMergeTarget = false,
            TrackMergeMessage = true,
            RegularExpression = MainBranch.RegexPattern,
            SourceBranches = [],
            TracksReleaseBranches = false,
            IsReleaseBranch = false,
            IsMainBranch = true,
            PreReleaseWeight = 55000
        });

        WithBranch(ReleaseBranch.Name).WithConfiguration(new BranchConfiguration
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
            RegularExpression = ReleaseBranch.RegexPattern,
            SourceBranches =
            [
                this.MainBranch.Name
            ],
            TracksReleaseBranches = false,
            IsReleaseBranch = true,
            IsMainBranch = false,
            PreReleaseWeight = 30000
        });

        WithBranch(FeatureBranch.Name).WithConfiguration(new BranchConfiguration
        {
            DeploymentMode = DeploymentMode.ManualDeployment,
            Label = ConfigurationConstants.BranchNamePlaceholder,
            Increment = IncrementStrategy.Inherit,
            PreventIncrement = new PreventIncrementConfiguration
            {
                WhenCurrentCommitTagged = false
            },
            RegularExpression = FeatureBranch.RegexPattern,
            SourceBranches =
            [
                this.MainBranch.Name,
                this.ReleaseBranch.Name
            ],
            TrackMergeMessage = true,
            IsMainBranch = false,
            PreReleaseWeight = 30000
        });

        WithBranch(PullRequestBranch.Name).WithConfiguration(new BranchConfiguration
        {
            DeploymentMode = DeploymentMode.ContinuousDelivery,
            Label = "PullRequest",
            Increment = IncrementStrategy.Inherit,
            PreventIncrement = new PreventIncrementConfiguration
            {
                OfMergedBranch = true,
                WhenCurrentCommitTagged = false
            },
            LabelNumberPattern = RegexPatterns.Configuration.DefaultLabelNumberPattern,
            RegularExpression = PullRequestBranch.RegexPattern,
            SourceBranches =
            [
                this.MainBranch.Name,
                this.ReleaseBranch.Name,
                this.FeatureBranch.Name
            ],
            TrackMergeMessage = true,
            PreReleaseWeight = 30000
        });

        WithBranch(UnknownBranch.Name).WithConfiguration(new BranchConfiguration
        {
            DeploymentMode = DeploymentMode.ManualDeployment,
            Label = ConfigurationConstants.BranchNamePlaceholder,
            Increment = IncrementStrategy.Inherit,
            PreventIncrement = new PreventIncrementConfiguration
            {
                WhenCurrentCommitTagged = false
            },
            RegularExpression = UnknownBranch.RegexPattern,
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
