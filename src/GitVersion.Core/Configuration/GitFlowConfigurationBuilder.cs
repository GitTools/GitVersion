using GitVersion.Extensions;
using GitVersion.VersionCalculation;

namespace GitVersion.Configuration;

internal sealed class GitFlowConfigurationBuilder : ConfigurationBuilderBase<GitFlowConfigurationBuilder>
{
    public static GitFlowConfigurationBuilder New => new();

    private GitFlowConfigurationBuilder()
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
            TagPrefix = ConfigurationConstants.DefaultTagPrefix,
            VersionInBranchPattern = ConfigurationConstants.DefaultVersionInBranchPattern,
            TagPreReleaseWeight = 60000,
            UpdateBuildNumber = true,
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

        WithBranch(DevelopBranch.Name).WithConfiguration(new BranchConfiguration()
        {
            Increment = IncrementStrategy.Minor,
            RegularExpression = DevelopBranch.RegexPattern,
            SourceBranches = new HashSet<string>(),
            Label = "alpha",
            PreventIncrementOfMergedBranchVersion = false,
            TrackMergeTarget = true,
            TracksReleaseBranches = true,
            IsMainline = false,
            IsReleaseBranch = false,
            PreReleaseWeight = 0
        });

        WithBranch(MainBranch.Name).WithConfiguration(new BranchConfiguration()
        {
            Increment = IncrementStrategy.Patch,
            RegularExpression = MainBranch.RegexPattern,
            SourceBranches = new HashSet<string> {
                DevelopBranch.Name,
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
            VersioningMode = VersioningMode.ContinuousDelivery,
            SourceBranches = new HashSet<string> {
                DevelopBranch.Name,
                MainBranch.Name,
                SupportBranch.Name,
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
                DevelopBranch.Name,
                MainBranch.Name,
                ReleaseBranch.Name,
                FeatureBranch.Name,
                SupportBranch.Name,
                HotfixBranch.Name
            },
            Label = ConfigurationConstants.BranchNamePlaceholder,
            PreReleaseWeight = 30000
        });

        WithBranch(PullRequestBranch.Name).WithConfiguration(new BranchConfiguration()
        {
            Increment = IncrementStrategy.Inherit,
            RegularExpression = PullRequestBranch.RegexPattern,
            VersioningMode = VersioningMode.ContinuousDeployment,
            SourceBranches = new HashSet<string> {
                DevelopBranch.Name,
                MainBranch.Name,
                ReleaseBranch.Name,
                FeatureBranch.Name,
                SupportBranch.Name,
                HotfixBranch.Name
            },
            Label = "PullRequest",
            LabelNumberPattern = @"[/-](?<number>\d+)",
            PreReleaseWeight = 30000
        });

        WithBranch(HotfixBranch.Name).WithConfiguration(new BranchConfiguration()
        {
            Increment = IncrementStrategy.Inherit,
            RegularExpression = HotfixBranch.RegexPattern,
            VersioningMode = VersioningMode.ContinuousDelivery,
            SourceBranches = new HashSet<string> {
                ReleaseBranch.Name,
                MainBranch.Name,
                SupportBranch.Name,
                HotfixBranch.Name
            },
            Label = "beta",
            IsReleaseBranch = true,
            PreReleaseWeight = 30000
        });

        WithBranch(SupportBranch.Name).WithConfiguration(new BranchConfiguration()
        {
            Increment = IncrementStrategy.Patch,
            RegularExpression = SupportBranch.RegexPattern,
            SourceBranches = new HashSet<string> { MainBranch.Name },
            Label = string.Empty,
            PreventIncrementOfMergedBranchVersion = true,
            TrackMergeTarget = false,
            TracksReleaseBranches = false,
            IsMainline = true,
            IsReleaseBranch = false,
            PreReleaseWeight = 55000
        });

        WithBranch(UnknownBranch.Name).WithConfiguration(new BranchConfiguration()
        {
            RegularExpression = UnknownBranch.RegexPattern,
            Label = ConfigurationConstants.BranchNamePlaceholder,
            VersioningMode = VersioningMode.ContinuousDelivery,
            Increment = IncrementStrategy.Inherit,
            SourceBranches = new HashSet<string> {
                MainBranch.Name,
                DevelopBranch.Name,
                ReleaseBranch.Name,
                FeatureBranch.Name,
                PullRequestBranch.Name,
                HotfixBranch.Name,
                SupportBranch.Name
            }
        });
    }
}
