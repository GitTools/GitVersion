using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests.Helpers;

internal sealed class GitFlowConfigurationBuilder : TestConfigurationBuilderBase<GitFlowConfigurationBuilder>
{
    public static GitFlowConfigurationBuilder New => new();

    private GitFlowConfigurationBuilder()
    {
        WithConfiguration(new()
        {
            AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatch,
            AssemblyFileVersioningScheme = AssemblyFileVersioningScheme.MajorMinorPatch,
            LabelPrefix = GitVersionConfiguration.DefaultLabelPrefix,
            VersioningMode = VersioningMode.ContinuousDelivery,
            ContinuousDeploymentFallbackLabel = "ci",
            MajorVersionBumpMessage = IncrementStrategyFinder.DefaultMajorPattern,
            MinorVersionBumpMessage = IncrementStrategyFinder.DefaultMinorPattern,
            PatchVersionBumpMessage = IncrementStrategyFinder.DefaultPatchPattern,
            NoBumpMessage = IncrementStrategyFinder.DefaultNoBumpPattern,
            CommitMessageIncrementing = CommitMessageIncrementMode.Enabled,
            CommitDateFormat = "yyyy-MM-dd",
            UpdateBuildNumber = true,
            SemanticVersionFormat = SemanticVersionFormat.Strict,
            LabelPreReleaseWeight = 60000,
            Increment = IncrementStrategy.Inherit
        });

        WithBranch(DevelopBranch.Name).WithConfiguration(new()
        {
            VersioningMode = VersioningMode.ContinuousDeployment,
            Increment = IncrementStrategy.Minor,
            Regex = DevelopBranch.RegexPattern,
            SourceBranches = new HashSet<string>(),
            Label = "alpha",
            PreventIncrementOfMergedBranchVersion = false,
            TrackMergeTarget = true,
            TracksReleaseBranches = true,
            IsMainline = false,
            IsReleaseBranch = false,
            PreReleaseWeight = 0
        });

        WithBranch(MainBranch.Name).WithConfiguration(new()
        {
            VersioningMode = VersioningMode.ContinuousDelivery,
            Increment = IncrementStrategy.Patch,
            Regex = MainBranch.RegexPattern,
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

        WithBranch(ReleaseBranch.Name).WithConfiguration(new()
        {
            VersioningMode = VersioningMode.ContinuousDelivery,
            Increment = IncrementStrategy.None,
            Regex = ReleaseBranch.RegexPattern,
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

        WithBranch(FeatureBranch.Name).WithConfiguration(new()
        {
            VersioningMode = VersioningMode.ContinuousDelivery,
            Increment = IncrementStrategy.Inherit,
            Regex = FeatureBranch.RegexPattern,
            SourceBranches = new HashSet<string> {
                DevelopBranch.Name,
                MainBranch.Name,
                ReleaseBranch.Name,
                FeatureBranch.Name,
                SupportBranch.Name,
                HotfixBranch.Name
            },
            Label = "{BranchName}",
            PreReleaseWeight = 30000
        });

        WithBranch(PullRequestBranch.Name).WithConfiguration(new()
        {
            VersioningMode = VersioningMode.ContinuousDelivery,
            Increment = IncrementStrategy.Inherit,
            Regex = PullRequestBranch.RegexPattern,
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

        WithBranch(HotfixBranch.Name).WithConfiguration(new()
        {
            VersioningMode = VersioningMode.ContinuousDelivery,
            Increment = IncrementStrategy.Inherit,
            Regex = HotfixBranch.RegexPattern,
            SourceBranches = new HashSet<string> {
                ReleaseBranch.Name,
                MainBranch.Name,
                SupportBranch.Name,
                HotfixBranch.Name
            },
            Label = "beta",
            PreReleaseWeight = 30000
        });

        WithBranch(SupportBranch.Name).WithConfiguration(new()
        {
            VersioningMode = VersioningMode.ContinuousDelivery,
            Increment = IncrementStrategy.Patch,
            Regex = SupportBranch.RegexPattern,
            SourceBranches = new HashSet<string> { MainBranch.Name },
            Label = string.Empty,
            PreventIncrementOfMergedBranchVersion = true,
            TrackMergeTarget = false,
            TracksReleaseBranches = false,
            IsMainline = true,
            IsReleaseBranch = false,
            PreReleaseWeight = 55000
        });
    }

    public static BranchMetaData MainBranch = new()
    {
        Name = "main",
        RegexPattern = "^master$|^main$"
    };

    public static BranchMetaData DevelopBranch = new()
    {
        Name = "develop",
        RegexPattern = "^dev(elop)?(ment)?$"
    };

    public static BranchMetaData ReleaseBranch = new()
    {
        Name = "release",
        RegexPattern = "^releases?[/-]"
    };

    public static BranchMetaData FeatureBranch = new()
    {
        Name = "feature",
        RegexPattern = "^features?[/-]"
    };

    public static BranchMetaData PullRequestBranch = new()
    {
        Name = "pull-request",
        RegexPattern = @"^(pull|pull\-requests|pr)[/-]"
    };

    public static BranchMetaData HotfixBranch = new()
    {
        Name = "hotfix",
        RegexPattern = "^hotfix(es)?[/-]"
    };

    public static BranchMetaData SupportBranch = new()
    {
        Name = "support",
        RegexPattern = "^support[/-]"
    };
}
