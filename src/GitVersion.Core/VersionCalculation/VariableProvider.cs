using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Formatting;
using GitVersion.OutputVariables;

namespace GitVersion.VersionCalculation;

internal sealed class VariableProvider(IEnvironment environment) : IVariableProvider
{
    private readonly IEnvironment environment = environment.NotNull();

    public GitVersionVariables GetVariablesFor(
        SemanticVersion semanticVersion,
        IGitVersionConfiguration configuration,
        EffectiveConfiguration effectiveConfiguration)
    {
        semanticVersion.NotNull();
        configuration.NotNull();
        effectiveConfiguration.NotNull();

        var semverFormatValues = new SemanticVersionFormatValues(
            semanticVersion, configuration, effectiveConfiguration.PreReleaseWeight);

        var informationalVersion = CheckAndFormatString(
            configuration.AssemblyInformationalFormat,
            semverFormatValues,
            semverFormatValues.InformationalVersion,
            "AssemblyInformationalVersion"
        );

        var customVersion = CheckAndFormatString(
            effectiveConfiguration.CustomVersionFormat,
            semverFormatValues,
            string.Empty,
            "CustomVersionFormat"
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
            CustomVersion: customVersion,
            EscapedBranchName: semverFormatValues.EscapedBranchName,
            FullBuildMetaData: semverFormatValues.FullBuildMetaData,
            FullSemVer: semverFormatValues.FullSemVer,
            InformationalVersion: informationalVersion,
            Major: semverFormatValues.Major,
            MajorMinorPatch: semverFormatValues.MajorMinorPatch,
            Minor: semverFormatValues.Minor,
            Patch: semverFormatValues.Patch,
            PreReleaseLabelName: semverFormatValues.PreReleaseLabelName,
            PreReleaseLabelNameWithDash: semverFormatValues.PreReleaseLabelNameWithDash,
            PreReleaseNumber: semverFormatValues.PreReleaseNumber,
            PreReleaseLabel: semverFormatValues.PreReleaseLabel,
            PreReleaseLabelWithDash: semverFormatValues.PreReleaseLabelWithDash,
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

    private string? CheckAndFormatString<T>(string? formatString, T source, string? defaultValue, string formatVarName) where T : notnull
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
            catch (Exception exception) when (exception is ArgumentException or FormatException)
            {
                throw new WarningException($"Unable to format {formatVarName}.  Check your format string: {exception.Message}");
            }
        }

        return formattedString;
    }
}
