using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests.Helpers;

public record TestEffectiveConfiguration : EffectiveConfiguration
{
    public TestEffectiveConfiguration(
        AssemblyVersioningScheme assemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatch,
        AssemblyFileVersioningScheme assemblyFileVersioningScheme = AssemblyFileVersioningScheme.MajorMinorPatch,
        string? assemblyVersioningFormat = null,
        string? assemblyFileVersioningFormat = null,
        string? assemblyInformationalFormat = null,
        DeploymentMode versioningMode = DeploymentMode.ContinuousDelivery,
        string tagPrefix = ConfigurationConstants.DefaultTagPrefix,
        string label = "ci",
        string? nextVersion = null,
        string regularExpression = "",
        bool preventIncrementOfMergedBranchVersion = false,
        string? labelNumberPattern = null,
        bool trackMergeTarget = false,
        string? majorMessage = null,
        string? minorMessage = null,
        string? patchMessage = null,
        string? noBumpMessage = null,
        CommitMessageIncrementMode commitMessageMode = CommitMessageIncrementMode.Enabled,
        IEnumerable<IVersionFilter>? versionFilters = null,
        bool tracksReleaseBranches = false,
        bool isRelease = false,
        bool isMainBranch = false,
        string commitDateFormat = "yyyy-MM-dd",
        bool updateBuildNumber = false) :
        base(assemblyVersioningScheme,
            assemblyFileVersioningScheme,
            assemblyInformationalFormat,
            assemblyVersioningFormat,
            assemblyFileVersioningFormat,
            versioningMode,
            tagPrefix,
            label,
            nextVersion,
            IncrementStrategy.Patch,
            regularExpression,
            preventIncrementOfMergedBranchVersion,
            labelNumberPattern,
            trackMergeTarget,
            majorMessage,
            minorMessage,
            patchMessage,
            noBumpMessage,
            commitMessageMode,
            versionFilters ?? [],
            tracksReleaseBranches,
            isRelease,
            isMainBranch,
            commitDateFormat,
            updateBuildNumber,
            SemanticVersionFormat.Strict,
            0,
            0)
    {
    }
}
