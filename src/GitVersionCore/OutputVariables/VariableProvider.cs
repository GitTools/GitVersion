namespace GitVersion
{
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
                semanticVersion.BuildMetaData.CommitsSinceTag = null;
            }

            var variables = new VersionVariables(
                major: semanticVersion.Major.ToString(),
                minor: semanticVersion.Minor.ToString(),
                patch: semanticVersion.Patch.ToString(),
                preReleaseTag: semanticVersion.PreReleaseTag,
                preReleaseTagWithDash: semanticVersion.PreReleaseTag.HasTag() ? "-" + semanticVersion.PreReleaseTag : null,
                buildMetaData: semanticVersion.BuildMetaData,
                buildMetaDataPadded: semanticVersion.BuildMetaData.ToString("p" + config.BuildMetaDataPadding),
                fullBuildMetaData: semanticVersion.BuildMetaData.ToString("f"),
                majorMinorPatch: string.Format("{0}.{1}.{2}", semanticVersion.Major, semanticVersion.Minor, semanticVersion.Patch),
                semVer: semanticVersion.ToString(),
                legacySemVer: semanticVersion.ToString("l"),
                legacySemVerPadded: semanticVersion.ToString("lp" + config.LegacySemVerPadding),
                assemblySemVer: semanticVersion.GetAssemblyVersion(config.AssemblyVersioningScheme),
                fullSemVer: semanticVersion.ToString("f"),
                informationalVersion: semanticVersion.ToString("i"),
                branchName: semanticVersion.BuildMetaData.Branch,
                sha: semanticVersion.BuildMetaData.Sha,
                commitDate: semanticVersion.BuildMetaData.CommitDate.UtcDateTime.ToString("yyyy-MM-dd"));

            return variables;
        }
    }
}