namespace GitVersion
{
    public static class VariableProvider
    {
        public static VersionVariables GetVariablesFor(SemanticVersion semanticVersion, AssemblyVersioningScheme assemblyVersioningScheme, VersioningMode mode)
        {
            var bmd = semanticVersion.BuildMetaData;
            var formatter = bmd.Branch == "develop" ? new CiFeedFormatter() : null;

            var classicVersionWithTag = string.Format("{0}.{1}{2}", semanticVersion.ToString("j"), bmd.CommitsSinceTag ?? 0, semanticVersion.PreReleaseTag.HasTag() ? "-" + semanticVersion.PreReleaseTag : null);
            var variables = new VersionVariables(
                major: semanticVersion.Major.ToString(),
                minor: semanticVersion.Minor.ToString(),
                patch: semanticVersion.Patch.ToString(),
                preReleaseTag: semanticVersion.PreReleaseTag,
                preReleaseTagWithDash: semanticVersion.PreReleaseTag.HasTag() ? "-" + semanticVersion.PreReleaseTag : null,
                buildMetaData: bmd,
                fullBuildMetaData: bmd.ToString("f"),
                majorMinorPatch: string.Format("{0}.{1}.{2}", semanticVersion.Major, semanticVersion.Minor, semanticVersion.Patch),
                semVer: semanticVersion.ToString(null, formatter),
                legacySemVer: semanticVersion.ToString("l", formatter),
                legacySemVerPadded: semanticVersion.ToString("lp", formatter),
                assemblySemVer: semanticVersion.GetAssemblyVersion(assemblyVersioningScheme),
                assemblyFileSemVer: semanticVersion.GetAssemblyFileVersion(assemblyVersioningScheme),
                fullSemVer: semanticVersion.ToString("f", formatter),
                informationalVersion: semanticVersion.ToString("i", formatter),
                classicVersion: string.Format("{0}.{1}", semanticVersion.ToString("j"), (bmd.CommitsSinceTag ?? 0)),
                classicVersionWithTag: classicVersionWithTag,
                branchName: bmd.Branch,
                sha: bmd.Sha);

            return variables;
        }
    }
}