﻿namespace GitVersion
{
    public static class VariableProvider
    {
        public static VersionVariables GetVariablesFor(
            SemanticVersion semanticVersion, AssemblyVersioningScheme assemblyVersioningScheme, 
            VersioningMode mode, string continuousDeploymentFallbackTag, 
            bool currentCommitIsTagged)
        {
            var bmd = semanticVersion.BuildMetaData;
            if (mode == VersioningMode.ContinuousDeployment && !currentCommitIsTagged)
            {
                semanticVersion = new SemanticVersion(semanticVersion);
                // Continuous Delivery always requires a pre-release tag unless the commit is tagged
                if (!semanticVersion.PreReleaseTag.HasTag())
                {
                    semanticVersion.PreReleaseTag.Name = continuousDeploymentFallbackTag;
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
                buildMetaData: bmd,
                fullBuildMetaData: bmd.ToString("f"),
                majorMinorPatch: string.Format("{0}.{1}.{2}", semanticVersion.Major, semanticVersion.Minor, semanticVersion.Patch),
                semVer: semanticVersion.ToString(),
                legacySemVer: semanticVersion.ToString("l"),
                legacySemVerPadded: semanticVersion.ToString("lp"),
                assemblySemVer: semanticVersion.GetAssemblyVersion(assemblyVersioningScheme),
                fullSemVer: semanticVersion.ToString("f"),
                informationalVersion: semanticVersion.ToString("i"),
                branchName: bmd.Branch,
                sha: bmd.Sha);

            return variables;
        }
    }
}