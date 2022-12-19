using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests.Helpers;

public class TestEffectiveConfiguration : EffectiveConfiguration
{
    public TestEffectiveConfiguration(
        AssemblyVersioningScheme assemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatch,
        AssemblyFileVersioningScheme assemblyFileVersioningScheme = AssemblyFileVersioningScheme.MajorMinorPatch,
        string? assemblyVersioningFormat = null,
        string? assemblyFileVersioningFormat = null,
        string? assemblyInformationalFormat = null,
        VersioningMode versioningMode = VersioningMode.ContinuousDelivery,
        string labelPrefix = GitVersionConfiguration.DefaultLabelPrefix,
        string label = "",
        string? nextVersion = null,
        string branchPrefixToTrim = "",
        bool preventIncrementOfMergedBranchVersion = false,
        string? labelNumberPattern = null,
        string continuousDeploymentFallbackLabel = "ci",
        bool trackMergeTarget = false,
        string? majorMessage = null,
        string? minorMessage = null,
        string? patchMessage = null,
        string? noBumpMessage = null,
        CommitMessageIncrementMode commitMessageMode = CommitMessageIncrementMode.Enabled,
        IEnumerable<IVersionFilter>? versionFilters = null,
        bool tracksReleaseBranches = false,
        bool isRelease = false,
        bool isMainline = false,
        string commitDateFormat = "yyyy-MM-dd",
        bool updateBuildNumber = false) :
        base(assemblyVersioningScheme,
            assemblyFileVersioningScheme,
            assemblyInformationalFormat,
            assemblyVersioningFormat,
            assemblyFileVersioningFormat,
            versioningMode,
            labelPrefix,
            label,
            nextVersion,
            IncrementStrategy.Patch,
            branchPrefixToTrim,
            preventIncrementOfMergedBranchVersion,
            labelNumberPattern,
            continuousDeploymentFallbackLabel,
            trackMergeTarget,
            majorMessage,
            minorMessage,
            patchMessage,
            noBumpMessage,
            commitMessageMode,
            versionFilters ?? Enumerable.Empty<IVersionFilter>(),
            tracksReleaseBranches,
            isRelease,
            isMainline,
            commitDateFormat,
            updateBuildNumber,
            SemanticVersionFormat.Strict,
            0,
            0)
    {
    }
}
