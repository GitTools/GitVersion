using GitVersion.Configuration;
using GitVersion.Core;
using GitVersion.Extensions;
using GitVersion.Formatting;
using GitVersion.OutputVariables;

namespace GitVersion.VersionCalculation;

internal sealed class VariableProvider(IEnvironment environment) : IVariableProvider
{
    private readonly IEnvironment environment = environment.NotNull();

    public GitVersionVariables GetVariablesFor(
        SemanticVersion semanticVersion, IGitVersionConfiguration configuration, int preReleaseWeight)
    {
        semanticVersion.NotNull();
        configuration.NotNull();

        var semverFormatValues = new SemanticVersionFormatValues(semanticVersion, configuration, preReleaseWeight);

        var informationalVersion = CheckAndFormatString(
            configuration.AssemblyInformationalFormat,
            semverFormatValues,
            semverFormatValues.InformationalVersion,
            "AssemblyInformationalVersion"
        );

        var assemblyFileSemVer = CheckAndFormatString(
            configuration.AssemblyFileVersioningFormat,
            semverFormatValues,
            semverFormatValues.AssemblyFileSemVer,
            "AssemblyFileVersioningFormat"
        );

        var assemblySemVer = CheckAndFormatString(
            configuration.AssemblyVersioningFormat,
            semverFormatValues,
            semverFormatValues.AssemblySemVer,
            "AssemblyVersioningFormat"
        );

        return new(
            AssemblySemFileVer: assemblyFileSemVer,
            AssemblySemVer: assemblySemVer,
            BranchName: semverFormatValues.BranchName,
            BuildMetaData: semverFormatValues.BuildMetaData,
            CommitDate: semverFormatValues.CommitDate,
            CommitsSinceVersionSource: semverFormatValues.VersionSourceDistance,
            EscapedBranchName: semverFormatValues.EscapedBranchName,
            FullBuildMetaData: semverFormatValues.FullBuildMetaData,
            FullSemVer: semverFormatValues.FullSemVer,
            InformationalVersion: informationalVersion,
            Major: semverFormatValues.Major,
            MajorMinorPatch: semverFormatValues.MajorMinorPatch,
            Minor: semverFormatValues.Minor,
            Patch: semverFormatValues.Patch,
            PreReleaseLabel: semverFormatValues.PreReleaseLabel,
            PreReleaseLabelWithDash: semverFormatValues.PreReleaseLabelWithDash,
            PreReleaseNumber: semverFormatValues.PreReleaseNumber,
            PreReleaseTag: semverFormatValues.PreReleaseTag,
            PreReleaseTagWithDash: semverFormatValues.PreReleaseTagWithDash,
            SemVer: semverFormatValues.SemVer,
            Sha: semverFormatValues.Sha,
            ShortSha: semverFormatValues.ShortSha,
            UncommittedChanges: semverFormatValues.UncommittedChanges,
            VersionSourceDistance: semverFormatValues.VersionSourceDistance,
            VersionSourceIncrement: semverFormatValues.VersionSourceIncrement,
            VersionSourceSemVer: semverFormatValues.VersionSourceSemVer,
            VersionSourceSha: semverFormatValues.VersionSourceSha,
            WeightedPreReleaseNumber: semverFormatValues.WeightedPreReleaseNumber);
    }

    private string? CheckAndFormatString<T>(string? formatString, T source, string? defaultValue, string formatVarName)
    {
        string? formattedString;

        if (formatString.IsNullOrEmpty())
        {
            formattedString = defaultValue;
        }
        else
        {
            try
            {
                formattedString = formatString.FormatWith(source, this.environment)
                    .RegexReplace(RegexPatterns.Output.SanitizeAssemblyInfoRegexPattern, "-");
            }
            catch (ArgumentException exception)
            {
                throw new WarningException($"Unable to format {formatVarName}.  Check your format string: {exception.Message}");
            }
        }

        return formattedString;
    }
}
