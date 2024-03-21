using GitVersion.VersionCalculation;

namespace GitVersion.Configuration;

internal sealed class GitFlowConfigurationBuilder : ConfigurationBuilderBase<GitFlowConfigurationBuilder>
{
    public static GitFlowConfigurationBuilder New => new();

    private GitFlowConfigurationBuilder()
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
            VersionStrategies = ConfigurationConstants.DefaultVersionStrategies,
            TagPrefix = ConfigurationConstants.DefaultTagPrefix,
            VersionInBranchPattern = ConfigurationConstants.DefaultVersionInBranchPattern,
            TagPreReleaseWeight = ConfigurationConstants.DefaultTagPreReleaseWeight,
            UpdateBuildNumber = ConfigurationConstants.DefaultUpdateBuildNumber,
            DeploymentMode = DeploymentMode.ContinuousDelivery,
            RegularExpression = string.Empty,
            Label = ConfigurationConstants.BranchNamePlaceholder,
            Increment = IncrementStrategy.Inherit,
            CommitMessageIncrementing = CommitMessageIncrementMode.Enabled,
            PreventIncrement = new PreventIncrementConfiguration()
            {
                OfMergedBranch = false,
                WhenBranchMerged = false,
                WhenCurrentCommitTagged = true
            },
            TrackMergeTarget = false,
            TrackMergeMessage = true,
            TracksReleaseBranches = false,
            IsReleaseBranch = false,
            IsMainBranch = false
        });

        WithBranch(DevelopBranch.Name).WithConfiguration(new BranchConfiguration
        {
            Increment = IncrementStrategy.Minor,
            RegularExpression = DevelopBranch.RegexPattern,
            SourceBranches = [],
            Label = "alpha",
            PreventIncrement = new PreventIncrementConfiguration()
            {
                WhenCurrentCommitTagged = false
            },
            TrackMergeTarget = true,
            TracksReleaseBranches = true,
            IsMainBranch = false,
            IsReleaseBranch = false,
            PreReleaseWeight = 0
        });

        WithBranch(MainBranch.Name).WithConfiguration(new BranchConfiguration
        {
            Increment = IncrementStrategy.Patch,
            RegularExpression = MainBranch.RegexPattern,
            SourceBranches =
            [
                this.DevelopBranch.Name,
                this.ReleaseBranch.Name
            ],
            Label = string.Empty,
            PreventIncrement = new PreventIncrementConfiguration()
            {
                OfMergedBranch = true
            },
            TrackMergeTarget = false,
            TracksReleaseBranches = false,
            IsMainBranch = true,
            IsReleaseBranch = false,
            PreReleaseWeight = 55000
        });

        WithBranch(ReleaseBranch.Name).WithConfiguration(new BranchConfiguration
        {
            Increment = IncrementStrategy.None,
            DeploymentMode = DeploymentMode.ManualDeployment,
            RegularExpression = ReleaseBranch.RegexPattern,
            SourceBranches =
            [
                this.DevelopBranch.Name,
                this.MainBranch.Name,
                this.SupportBranch.Name,
                this.ReleaseBranch.Name
            ],
            Label = "beta",
            PreventIncrement = new PreventIncrementConfiguration()
            {
                OfMergedBranch = true,
                WhenCurrentCommitTagged = false
            },
            TrackMergeTarget = false,
            TracksReleaseBranches = false,
            IsMainBranch = false,
            IsReleaseBranch = true,
            PreReleaseWeight = 30000
        });

        WithBranch(FeatureBranch.Name).WithConfiguration(new BranchConfiguration
        {
            Increment = IncrementStrategy.Inherit,
            RegularExpression = FeatureBranch.RegexPattern,
            DeploymentMode = DeploymentMode.ManualDeployment,
            SourceBranches =
            [
                this.DevelopBranch.Name,
                this.MainBranch.Name,
                this.ReleaseBranch.Name,
                this.FeatureBranch.Name,
                this.SupportBranch.Name,
                this.HotfixBranch.Name
            ],
            Label = ConfigurationConstants.BranchNamePlaceholder,
            PreReleaseWeight = 30000
        });

        WithBranch(PullRequestBranch.Name).WithConfiguration(new BranchConfiguration
        {
            Increment = IncrementStrategy.Inherit,
            RegularExpression = PullRequestBranch.RegexPattern,
            DeploymentMode = DeploymentMode.ContinuousDelivery,
            SourceBranches =
            [
                this.DevelopBranch.Name,
                this.MainBranch.Name,
                this.ReleaseBranch.Name,
                this.FeatureBranch.Name,
                this.SupportBranch.Name,
                this.HotfixBranch.Name
            ],
            Label = "PullRequest",
            LabelNumberPattern = ConfigurationConstants.DefaultLabelNumberPattern,
            PreReleaseWeight = 30000
        });

        WithBranch(HotfixBranch.Name).WithConfiguration(new BranchConfiguration
        {
            Increment = IncrementStrategy.Inherit,
            RegularExpression = HotfixBranch.RegexPattern,
            DeploymentMode = DeploymentMode.ManualDeployment,
            PreventIncrement = new PreventIncrementConfiguration()
            {
                WhenCurrentCommitTagged = false
            },
            SourceBranches =
            [
                this.ReleaseBranch.Name,
                this.MainBranch.Name,
                this.SupportBranch.Name,
                this.HotfixBranch.Name
            ],
            Label = "beta",
            IsReleaseBranch = true,
            PreReleaseWeight = 30000
        });

        WithBranch(SupportBranch.Name).WithConfiguration(new BranchConfiguration
        {
            Increment = IncrementStrategy.Patch,
            RegularExpression = SupportBranch.RegexPattern,
            SourceBranches = [this.MainBranch.Name],
            Label = string.Empty,
            PreventIncrement = new PreventIncrementConfiguration()
            {
                OfMergedBranch = true
            },
            TrackMergeTarget = false,
            TracksReleaseBranches = false,
            IsMainBranch = true,
            IsReleaseBranch = false,
            PreReleaseWeight = 55000
        });

        WithBranch(UnknownBranch.Name).WithConfiguration(new BranchConfiguration
        {
            RegularExpression = UnknownBranch.RegexPattern,
            Label = ConfigurationConstants.BranchNamePlaceholder,
            DeploymentMode = DeploymentMode.ManualDeployment,
            Increment = IncrementStrategy.Inherit,
            SourceBranches =
            [
                this.MainBranch.Name,
                this.DevelopBranch.Name,
                this.ReleaseBranch.Name,
                this.FeatureBranch.Name,
                this.PullRequestBranch.Name,
                this.HotfixBranch.Name,
                this.SupportBranch.Name
            ]
        });
    }
}
