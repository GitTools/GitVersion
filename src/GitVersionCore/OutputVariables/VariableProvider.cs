using System;
using System.Text.RegularExpressions;
using GitVersion.Exceptions;
using GitVersion.Extensions;
using GitVersion.VersionCalculation;
using GitVersion.VersioningModes;
using GitVersion.Configuration;
using GitVersion.Helpers;
using GitVersion.Logging;

namespace GitVersion.OutputVariables
{
    public class VariableProvider : IVariableProvider
    {
        private readonly INextVersionCalculator nextVersionCalculator;
        private readonly IEnvironment environment;
        private readonly ILog log;

        public VariableProvider(INextVersionCalculator nextVersionCalculator, IEnvironment environment, ILog log = default)
        {
            this.nextVersionCalculator = nextVersionCalculator ?? throw new ArgumentNullException(nameof(nextVersionCalculator));
            this.environment = environment;
            this.log = log ?? new NullLog();
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

            var informationalVersion = CheckAndFormatString(config.AssemblyInformationalFormat, semverFormatValues,
                environment, semverFormatValues.InformationalVersion, "AssemblyInformationalVersion");

            var assemblyFileSemVer = CheckAndFormatString(config.AssemblyFileVersioningFormat, semverFormatValues,
                environment, semverFormatValues.AssemblyFileSemVer, "AssemblyFileVersioningFormat");

            var assemblySemVer = CheckAndFormatString(config.AssemblyVersioningFormat, semverFormatValues,
                environment, semverFormatValues.AssemblySemVer, "AssemblyVersioningFormat");

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
            if (!semanticVersion.BuildMetaData.CommitsSinceTag.HasValue)
            {
                semanticVersion.PreReleaseTag.Number = null;
                semanticVersion.BuildMetaData.CommitsSinceVersionSource = 0;
            }
            else
            {
                // Number of commits since last tag should be added to PreRelease number if given. Remember to deduct automatic version bump.
                if (semanticVersion.PreReleaseTag.Number.HasValue)
                {
                    semanticVersion.PreReleaseTag.Number += semanticVersion.BuildMetaData.CommitsSinceTag - 1;
                }
                else
                {
                    semanticVersion.PreReleaseTag.Number = semanticVersion.BuildMetaData.CommitsSinceTag;
                }
                semanticVersion.BuildMetaData.CommitsSinceVersionSource = semanticVersion.BuildMetaData.CommitsSinceTag.Value;
                semanticVersion.BuildMetaData.CommitsSinceTag = null; // why is this set to null ?
            }
        }

        private string CheckAndFormatString<T>(string formatString, T source, IEnvironment environment, string defaultValue, string formatVarName)
        {
            string formattedString;

            if (string.IsNullOrEmpty(formatString))
            {
                formattedString = defaultValue;
            }
            else
            {
                WarnIfUsingObsoleteFormatValues(formatString);

                try
                {
                    formattedString = formatString.FormatWith(source, environment).RegexReplace("[^0-9A-Za-z-.+]", "-");
                }
                catch (ArgumentException formex)
                {
                    throw new WarningException($"Unable to format {formatVarName}.  Check your format string: {formex.Message}");
                }
            }

            return formattedString;
        }

        private void WarnIfUsingObsoleteFormatValues(string formatString)
        {
            var obsoletePropertyName = nameof(SemanticVersionFormatValues.DefaultInformationalVersion);
            if (formatString.Contains($"{{{obsoletePropertyName}}}"))
            {
                log.Write(LogLevel.Warn, $"Use format variable '{nameof(SemanticVersionFormatValues.InformationalVersion)}' instead of '{obsoletePropertyName}' which is obsolete and will be removed in a future release.");
            }
        }
    }
}
