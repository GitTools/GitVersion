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
            if (config.VersioningMode == VersioningMode.ContinuousDeployment && !isCurrentCommitTagged)
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

                // Evaluate tag number pattern and append to prerelease tag, preserving build metadata
                if (!string.IsNullOrEmpty(config.TagNumberPattern))
                {
                    var match = Regex.Match(semanticVersion.BuildMetaData.Branch, config.TagNumberPattern);
                    var numberGroup = match.Groups["number"];
                    if (numberGroup.Success)
                    {
                        semanticVersion.PreReleaseTag.Name += numberGroup.Value.PadLeft(config.BuildMetaDataPadding, '0');
                    }
                }

                // For continuous deployment the commits since tag gets promoted to the pre-release number
                semanticVersion.PreReleaseTag.Number = semanticVersion.BuildMetaData.CommitsSinceTag;
                semanticVersion.BuildMetaData.CommitsSinceVersionSource = semanticVersion.BuildMetaData.CommitsSinceTag ?? 0;
                semanticVersion.BuildMetaData.CommitsSinceTag = null;
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
                    informationalVersion = config.AssemblyInformationalFormat.FormatWith(semverFormatValues);
                }
                catch (FormatException formex)
                {
                    throw new WarningException(string.Format("Unable to format AssemblyInformationalVersion.  Check your format string: {0}", formex.Message));
                }
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
                semverFormatValues.PreReleaseTag,
                semverFormatValues.PreReleaseTagWithDash,
                semverFormatValues.PreReleaseLabel,
                semverFormatValues.PreReleaseNumber,
                informationalVersion,
                semverFormatValues.CommitDate,
                semverFormatValues.NuGetVersion,
                semverFormatValues.NuGetVersionV2,
                semverFormatValues.CommitsSinceVersionSource,
                semverFormatValues.CommitsSinceVersionSourcePadded);

            return variables;
        }
    }
}