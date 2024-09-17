using GitVersion.Core;
using GitVersion.VersionCalculation;

namespace GitVersion.Configuration;

internal sealed class TrunkBasedConfigurationBuilder : ConfigurationBuilderBase<TrunkBasedConfigurationBuilder>
{
    public static TrunkBasedConfigurationBuilder New => new();

    private TrunkBasedConfigurationBuilder()
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
            VersionStrategies = [
                VersionStrategies.ConfiguredNextVersion,
                VersionStrategies.Mainline
            ],
            TagPrefix = RegexPatterns.Configuration.DefaultTagPrefixPattern,
            VersionInBranchPattern = RegexPatterns.Configuration.DefaultVersionInBranchPattern,
            TagPreReleaseWeight = ConfigurationConstants.DefaultTagPreReleaseWeight,
            UpdateBuildNumber = ConfigurationConstants.DefaultUpdateBuildNumber,
            DeploymentMode = DeploymentMode.ContinuousDelivery,
            RegularExpression = string.Empty,
            Label = ConfigurationConstants.BranchNamePlaceholder,
            Increment = IncrementStrategy.Inherit,
            CommitMessageIncrementing = CommitMessageIncrementMode.Enabled,
            PreventIncrement = new PreventIncrementConfiguration
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

        WithBranch(MainBranch.Name).WithConfiguration(new BranchConfiguration
        {
            DeploymentMode = DeploymentMode.ContinuousDeployment,
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

        WithBranch(FeatureBranch.Name).WithConfiguration(new BranchConfiguration
        {
            DeploymentMode = DeploymentMode.ContinuousDelivery,
            Label = ConfigurationConstants.BranchNamePlaceholder,
            Increment = IncrementStrategy.Minor,
            PreventIncrement = new PreventIncrementConfiguration
            {
                WhenCurrentCommitTagged = false
            },
            RegularExpression = FeatureBranch.RegexPattern,
            SourceBranches =
            [
                this.MainBranch.Name
            ],
            TrackMergeMessage = true,
            IsMainBranch = false,
            PreReleaseWeight = 30000
        });

        WithBranch(HotfixBranch.Name).WithConfiguration(new BranchConfiguration
        {
            DeploymentMode = DeploymentMode.ContinuousDelivery,
            Label = ConfigurationConstants.BranchNamePlaceholder,
            Increment = IncrementStrategy.Patch,
            PreventIncrement = new PreventIncrementConfiguration
            {
                WhenCurrentCommitTagged = false
            },
            RegularExpression = HotfixBranch.RegexPattern,
            SourceBranches =
            [
                this.MainBranch.Name
            ],
            IsReleaseBranch = true,
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
                this.FeatureBranch.Name,
                this.HotfixBranch.Name,
            ],
            TrackMergeMessage = true,
            PreReleaseWeight = 30000
        });

        WithBranch(UnknownBranch.Name).WithConfiguration(new BranchConfiguration
        {
            Increment = IncrementStrategy.Patch,
            PreventIncrement = new PreventIncrementConfiguration
            {
                WhenCurrentCommitTagged = false
            },
            RegularExpression = UnknownBranch.RegexPattern,
            SourceBranches =
            [
                this.MainBranch.Name
            ],
            PreReleaseWeight = 30000
        });
    }
}
