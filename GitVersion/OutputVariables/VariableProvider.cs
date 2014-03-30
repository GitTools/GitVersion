namespace GitVersion
{
    using System;
    using System.Collections.Generic;

    public static class VariableProvider
    {
        public const string Major = "Major";
        public const string Minor = "Minor";
        public const string Patch = "Patch";
        public const string BuildMetaData = "BuildMetaData";
        public const string FullBuildMetaData = "FullBuildMetaData";
        public const string BranchName = "BranchName";
        public const string Sha = "Sha";
        public const string MajorMinorPatch = "MajorMinorPatch";
        public const string SemVer = "SemVer";
        public const string SemVerPadded = "SemVerPadded";
        public const string FullSemVer = "FullSemVer";
        public const string FullSemVerPadded = "FullSemVerPadded";
        public const string AssemblySemVer = "AssemblySemVer";
        public const string ClassicVersion = "ClassicVersion";
        public const string PreReleaseTag = "PreReleaseTag";
        public const string PreReleaseTagWithDash = "PreReleaseTagWithDash";

        public static Dictionary<string, string> GetVariablesFor(SemanticVersion semanticVersion)
        {
            var variables = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                {Major, semanticVersion.Major.ToString()},
                {Minor, semanticVersion.Minor.ToString()},
                {Patch, semanticVersion.Patch.ToString()},
                {PreReleaseTag, semanticVersion.PreReleaseTag},
                {PreReleaseTagWithDash, semanticVersion.PreReleaseTag.HasTag() ? "-" + semanticVersion.PreReleaseTag : null},
                {BuildMetaData, semanticVersion.BuildMetaData},
                {FullBuildMetaData, semanticVersion.BuildMetaData.ToString("f")},
                {MajorMinorPatch, string.Format("{0}.{1}.{2}", semanticVersion.Major, semanticVersion.Minor, semanticVersion.Patch)},
                {SemVer, semanticVersion.ToString()},
                {SemVerPadded, semanticVersion.ToString("sp")},
                {AssemblySemVer, semanticVersion.ToString("j") + ".0"},
                {FullSemVer, semanticVersion.ToString("f")},
                {FullSemVerPadded, semanticVersion.ToString("fp")},
                {ClassicVersion, string.Format("{0}.{1}", semanticVersion.ToString("j"), (semanticVersion.BuildMetaData.CommitsSinceTag ?? 0))},
                {BranchName, semanticVersion.BuildMetaData.Branch},
                {Sha, semanticVersion.BuildMetaData.Sha},
            };

            return variables;
        }
    }
}