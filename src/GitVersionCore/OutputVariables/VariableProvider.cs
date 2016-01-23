namespace GitVersion
{
    using System;

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
                    semanticVersion.PreReleaseTag.Name = config.ContinuousDeploymentFallbackTag;
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