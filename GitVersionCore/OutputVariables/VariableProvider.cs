namespace GitVersion
{
    using System;

    public static class VariableProvider
    {
        public static VersionVariables GetVariablesFor(SemanticVersion semanticVersion, AssemblyVersioningScheme assemblyVersioningScheme, VersioningMode mode)
        {
            var bmd = semanticVersion.BuildMetaData;
            IFormatProvider formatProvider = null;
            if (mode == VersioningMode.ContinuousDeployment)
            {
                // For continuous deployment the commits since tag gets promoted to the pre-release number
                semanticVersion = new SemanticVersion(semanticVersion);
                if (semanticVersion.PreReleaseTag.HasTag())
                {
                    var oldPreReleaseNumber = semanticVersion.PreReleaseTag.Number;
                    semanticVersion.PreReleaseTag.Number = semanticVersion.BuildMetaData.CommitsSinceTag;
                    semanticVersion.BuildMetaData.CommitsSinceTag = oldPreReleaseNumber;
                }
                else
                {
                    // When there is no pre-release tag we will make the version a 4 part number
                    formatProvider = new CommitsAsFourthVersionPartFormatter();
                }
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
                semVer: semanticVersion.ToString(null, formatProvider),
                legacySemVer: semanticVersion.ToString("l", formatProvider),
                legacySemVerPadded: semanticVersion.ToString("lp", formatProvider),
                assemblySemVer: semanticVersion.GetAssemblyVersion(assemblyVersioningScheme),
                fullSemVer: semanticVersion.ToString("f", formatProvider),
                informationalVersion: semanticVersion.ToString("i", formatProvider),
                branchName: bmd.Branch,
                sha: bmd.Sha);

            return variables;
        }
    }
}