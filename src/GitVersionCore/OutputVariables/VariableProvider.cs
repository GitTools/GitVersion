using System;
using System.Text.RegularExpressions;
using GitVersion.Exceptions;
using GitVersion.VersionCalculation;
using GitVersion.VersioningModes;
using GitVersion.Configuration;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.SemanticVersioning;

namespace GitVersion.OutputVariables
{
    public class VariableProvider : IVariableProvider
    {
        private readonly ILog log;

        public VariableProvider(ILog log)
        {
            this.log = log;
        }
        public VersionVariables GetVariablesFor(SemanticVersion semanticVersion, EffectiveConfiguration config, bool isCurrentCommitTagged)
        {
            var isContinuousDeploymentMode = config.VersioningMode == VersioningMode.ContinuousDeployment && !isCurrentCommitTagged;
            if (isContinuousDeploymentMode)
            {
                semanticVersion = new SemanticVersion(semanticVersion);
                // Continuous Deployment always requires a pre-release tag unless the commit is tagged
                if (!semanticVersion.PreReleaseTag.HasTag())
                {
                    var nextVersionCalculator = new NextVersionCalculator(log);
                    semanticVersion.PreReleaseTag.Name = nextVersionCalculator.GetBranchSpecificTag(config, semanticVersion.BuildMetaData.Branch, null);
                    if (string.IsNullOrEmpty(semanticVersion.PreReleaseTag.Name))
                    {
                        semanticVersion.PreReleaseTag.Name = config.ContinuousDeploymentFallbackTag;
                    }
                }
            }

            // Evaluate tag number pattern and append to prerelease tag, preserving build metadata
            var appendTagNumberPattern = !string.IsNullOrEmpty(config.TagNumberPattern) && semanticVersion.PreReleaseTag.HasTag();
            if (appendTagNumberPattern)
            {
                var match = Regex.Match(semanticVersion.BuildMetaData.Branch, config.TagNumberPattern);
                var numberGroup = match.Groups["number"];
                if (numberGroup.Success)
                {
                    semanticVersion.PreReleaseTag.Name += numberGroup.Value.PadLeft(config.BuildMetaDataPadding, '0');
                }
            }

            if (isContinuousDeploymentMode || appendTagNumberPattern || config.VersioningMode == VersioningMode.Mainline)
            {
                PromoteNumberOfCommitsToTagNumber(semanticVersion);
            }

            var semverFormatValues = new SemanticVersionFormatValues(semanticVersion, config);

            string informationalVersion = CheckAndFormatString(config.AssemblyInformationalFormat, semverFormatValues,
                semverFormatValues.DefaultInformationalVersion, "AssemblyInformationalVersion");

            string assemblyFileSemVer = CheckAndFormatString(config.AssemblyFileVersioningFormat, semverFormatValues,
                semverFormatValues.AssemblyFileSemVer, "AssemblyFileVersioningFormat");

            string assemblySemVer = CheckAndFormatString(config.AssemblyVersioningFormat, semverFormatValues,
                semverFormatValues.AssemblySemVer, "AssemblyVersioningFormat");

            var variables = new VersionVariables(
                semverFormatValues.Major,
                semverFormatValues.Minor,
                semverFormatValues.Patch,
                semverFormatValues.BuildMetaData,
                semverFormatValues.BuildMetaDataPadded,
                semverFormatValues.FullBuildMetaData,
                semverFormatValues.BranchName,
                semverFormatValues.Sha,
                semverFormatValues.ShortSha,
                semverFormatValues.MajorMinorPatch,
                semverFormatValues.SemVer,
                semverFormatValues.LegacySemVer,
                semverFormatValues.LegacySemVerPadded,
                semverFormatValues.FullSemVer,
                assemblySemVer,
                assemblyFileSemVer,
                semverFormatValues.PreReleaseTag,
                semverFormatValues.PreReleaseTagWithDash,
                semverFormatValues.PreReleaseLabel,
                semverFormatValues.PreReleaseNumber,
                semverFormatValues.WeightedPreReleaseNumber,
                informationalVersion,
                semverFormatValues.CommitDate,
                semverFormatValues.NuGetVersion,
                semverFormatValues.NuGetVersionV2,
                semverFormatValues.NuGetPreReleaseTag,
                semverFormatValues.NuGetPreReleaseTagV2,
                semverFormatValues.VersionSourceSha,
                semverFormatValues.CommitsSinceVersionSource,
                semverFormatValues.CommitsSinceVersionSourcePadded);

            return variables;
        }

        private static void PromoteNumberOfCommitsToTagNumber(SemanticVersion semanticVersion)
        {
            // For continuous deployment the commits since tag gets promoted to the pre-release number
            semanticVersion.PreReleaseTag.Number = semanticVersion.BuildMetaData.CommitsSinceTag;
            semanticVersion.BuildMetaData.CommitsSinceVersionSource = semanticVersion.BuildMetaData.CommitsSinceTag ?? 0;
            semanticVersion.BuildMetaData.CommitsSinceTag = null;
        }

        private static string CheckAndFormatString<T>(string formatString, T source,  string defaultValue, string formatVarName)
        {
            string formattedString;

            if (string.IsNullOrEmpty(formatString))
            {
                formattedString = defaultValue;
            }
            else
            {
                try
                {
                    formattedString = formatString.FormatWith(source);
                }
                catch (ArgumentException formex)
                {
                    throw new WarningException($"Unable to format {formatVarName}.  Check your format string: {formex.Message}");
                }
            }

            return formattedString;
        }
    }
}
