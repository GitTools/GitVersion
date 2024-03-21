using GitVersion.VersionCalculation;

namespace GitVersion.Configuration;

internal sealed class TrunkBasedConfigurationBuilder : ConfigurationBuilderBase<TrunkBasedConfigurationBuilder>
{
    public static TrunkBasedConfigurationBuilder New => new();

    private TrunkBasedConfigurationBuilder()
    {
        WithConfiguration(new GitVersionConfiguration()
        {
            AssemblyFileVersioningScheme = ConfigurationConstants.DefaultAssemblyFileVersioningScheme,
            AssemblyVersioningScheme = ConfigurationConstants.DefaultAssemblyVersioningScheme,
            CommitDateFormat = ConfigurationConstants.DefaultCommitDateFormat,
            MajorVersionBumpMessage = IncrementStrategyFinder.DefaultMajorPattern,
            MinorVersionBumpMessage = IncrementStrategyFinder.DefaultMinorPattern,
            NoBumpMessage = IncrementStrategyFinder.DefaultNoBumpPattern,
            PatchVersionBumpMessage = IncrementStrategyFinder.DefaultPatchPattern,
            SemanticVersionFormat = ConfigurationConstants.DefaultSemanticVersionFormat,
            VersionStrategies = [
                VersionStrategies.ConfiguredNextVersion,
                VersionStrategies.TrunkBased
            ],
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

        WithBranch(MainBranch.Name).WithConfiguration(new BranchConfiguration()
        {
            Increment = IncrementStrategy.Patch,
            RegularExpression = MainBranch.RegexPattern,
            DeploymentMode = DeploymentMode.ContinuousDeployment,
            SourceBranches = [],
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

        WithBranch(FeatureBranch.Name).WithConfiguration(new BranchConfiguration()
        {
            Increment = IncrementStrategy.Minor,
            RegularExpression = FeatureBranch.RegexPattern,
            SourceBranches =
            [
                this.MainBranch.Name
            ],
            PreventIncrement = new PreventIncrementConfiguration()
            {
                WhenCurrentCommitTagged = false
            },
            PreReleaseWeight = 30000
        });

        WithBranch(HotfixBranch.Name).WithConfiguration(new BranchConfiguration()
        {
            Increment = IncrementStrategy.Patch,
            RegularExpression = HotfixBranch.RegexPattern,
            SourceBranches =
            [
                this.MainBranch.Name
            ],
            PreventIncrement = new PreventIncrementConfiguration()
            {
                WhenCurrentCommitTagged = false
            },
            PreReleaseWeight = 30000
        });

        WithBranch(PullRequestBranch.Name).WithConfiguration(new BranchConfiguration
        {
            Increment = IncrementStrategy.Inherit,
            RegularExpression = PullRequestBranch.RegexPattern,
            DeploymentMode = DeploymentMode.ContinuousDelivery,
            SourceBranches =
            [
                this.MainBranch.Name
            ],
            Label = "PullRequest",
            LabelNumberPattern = ConfigurationConstants.DefaultLabelNumberPattern,
            PreReleaseWeight = 30000
        });

        WithBranch(UnknownBranch.Name).WithConfiguration(new BranchConfiguration
        {
            Increment = IncrementStrategy.Patch,
            RegularExpression = UnknownBranch.RegexPattern,
            SourceBranches =
            [
                this.MainBranch.Name
            ],
            PreventIncrement = new PreventIncrementConfiguration()
            {
                WhenCurrentCommitTagged = false
            },
            PreReleaseWeight = 30000
        });
    }
}
