namespace GitVersion
{
    using System;
    using System.ComponentModel;
    using System.Text.RegularExpressions;
    using GitVersion.VersionCalculation;

    public static class VariableProvider
    {
        public static VersionVariables GetVariablesFor(SemanticVersion semanticVersion, EffectiveConfiguration config, bool isCurrentCommitTagged)
        {
            var isContinuousDeploymentMode = config.VersioningMode == VersioningMode.ContinuousDeployment && !isCurrentCommitTagged;
            if (isContinuousDeploymentMode)
            {
                semanticVersion = new SemanticVersion(semanticVersion);
                // Continuous Deployment always requires a pre-release tag unless the commit is tagged
                if (!semanticVersion.PreReleaseTag.HasTag())
                {
                    semanticVersion.PreReleaseTag.Name = NextVersionCalculator.GetBranchSpecificTag(config, semanticVersion.BuildMetaData.Branch, null);
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

            string informationalVersion;

            if (string.IsNullOrEmpty(config.AssemblyInformationalFormat))
            {
                informationalVersion = semverFormatValues.DefaultInformationalVersion;
            }
            else
            {
                try
                {
                    informationalVersion = config.AssemblyInformationalFormat.FormatWith<SemanticVersionFormatValues>(semverFormatValues);
                }
                catch (ArgumentException formex)
                {
                    throw new WarningException(string.Format("Unable to format AssemblyInformationalVersion.  Check your format string: {0}", formex.Message));
                }
            }

            string assemblyFileVersioningFormat;
            string assemblyFileSemVer;

            if (!(string.IsNullOrEmpty(config.AssemblyFileVersioningFormat)))
            {
                //assembly-file-versioning-format value if provided in the config, overwrites the exisiting AssemblyFileSemVer
                try
                {
                    assemblyFileVersioningFormat = config.AssemblyFileVersioningFormat.FormatWith<SemanticVersionFormatValues>(semverFormatValues);
                    assemblyFileSemVer = assemblyFileVersioningFormat;
                }
                catch (ArgumentException formex)
                {
                    throw new WarningException(string.Format("Unable to format AssemblyFileVersioningFormat.  Check your format string: {0}", formex.Message));
                }
            }
            else
            {
                assemblyFileSemVer = semverFormatValues.AssemblyFileSemVer;
            }

            var variables = new VersionVariables(
                semverFormatValues.Major,
                semverFormatValues.Minor,
                semverFormatValues.Patch,
                semverFormatValues.BuildMetaData,
                semverFormatValues.BuildMetaDataPadded,
                semverFormatValues.FullBuildMetaData,
                semverFormatValues.BranchName,
                semverFormatValues.Sha,
                semverFormatValues.MajorMinorPatch,
                semverFormatValues.SemVer,
                semverFormatValues.LegacySemVer,
                semverFormatValues.LegacySemVerPadded,
                semverFormatValues.FullSemVer,
                semverFormatValues.AssemblySemVer,
                assemblyFileSemVer,
                semverFormatValues.PreReleaseTag,
                semverFormatValues.PreReleaseTagWithDash,
                semverFormatValues.PreReleaseLabel,
                semverFormatValues.PreReleaseNumber,
                informationalVersion,
                semverFormatValues.CommitDate,
                semverFormatValues.NuGetVersion,
                semverFormatValues.NuGetVersionV2,
                semverFormatValues.NuGetPreReleaseTag,
                semverFormatValues.NuGetPreReleaseTagV2,
                semverFormatValues.CommitsSinceVersionSource,
                semverFormatValues.CommitsSinceVersionSourcePadded);

            return variables;
        }

        static void PromoteNumberOfCommitsToTagNumber(SemanticVersion semanticVersion)
        {
            // For continuous deployment the commits since tag gets promoted to the pre-release number
            semanticVersion.PreReleaseTag.Number = semanticVersion.BuildMetaData.CommitsSinceTag;
            semanticVersion.BuildMetaData.CommitsSinceVersionSource = semanticVersion.BuildMetaData.CommitsSinceTag ?? 0;
            semanticVersion.BuildMetaData.CommitsSinceTag = null;
        }
    }
}