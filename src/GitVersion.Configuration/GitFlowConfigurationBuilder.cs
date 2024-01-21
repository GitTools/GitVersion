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
            TagPrefix = ConfigurationConstants.DefaultTagPrefix,
            VersionInBranchPattern = ConfigurationConstants.DefaultVersionInBranchPattern,
            TagPreReleaseWeight = ConfigurationConstants.DefaultTagPreReleaseWeight,
            UpdateBuildNumber = ConfigurationConstants.DefaultUpdateBuildNumber,
            VersioningMode = VersioningMode.ContinuousDeployment,
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

        WithBranch(DevelopBranch.Name).WithConfiguration(new BranchConfiguration
        {
            Increment = IncrementStrategy.Minor,
            RegularExpression = DevelopBranch.RegexPattern,
            SourceBranches = [],
            Label = "alpha",
            PreventIncrementOfMergedBranchVersion = false,
            TrackMergeTarget = true,
            TracksReleaseBranches = true,
            IsMainline = false,
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
            VersioningMode = VersioningMode.ContinuousDelivery,
            SourceBranches =
            [
                this.DevelopBranch.Name,
                this.MainBranch.Name,
                this.SupportBranch.Name,
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
            VersioningMode = VersioningMode.ContinuousDelivery,
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
            VersioningMode = VersioningMode.ContinuousDeployment,
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
            VersioningMode = VersioningMode.ContinuousDelivery,
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
            PreventIncrementOfMergedBranchVersion = true,
            TrackMergeTarget = false,
            TracksReleaseBranches = false,
            IsMainline = true,
            IsReleaseBranch = false,
            PreReleaseWeight = 55000
        });

        WithBranch(UnknownBranch.Name).WithConfiguration(new BranchConfiguration
        {
            RegularExpression = UnknownBranch.RegexPattern,
            Label = ConfigurationConstants.BranchNamePlaceholder,
            VersioningMode = VersioningMode.ContinuousDelivery,
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
