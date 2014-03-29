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

        public static Dictionary<string, string> ToKeyValue(this VersionAndBranch versionAndBranch)
        {
            var variables = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                {Major, versionAndBranch.Version.Major.ToString()},
                {Minor, versionAndBranch.Version.Minor.ToString()},
                {Patch, versionAndBranch.Version.Patch.ToString()},
                {PreReleaseTag, versionAndBranch.Version.PreReleaseTag},
                {PreReleaseTagWithDash, versionAndBranch.Version.PreReleaseTag.HasTag() ? "-" + versionAndBranch.Version.PreReleaseTag : null},
                {BuildMetaData, versionAndBranch.Version.BuildMetaData},
                {FullBuildMetaData, versionAndBranch.Version.BuildMetaData.ToString("f")},
                {MajorMinorPatch, string.Format("{0}.{1}.{2}", versionAndBranch.Version.Major, versionAndBranch.Version.Minor, versionAndBranch.Version.Patch)},
                {SemVer, versionAndBranch.Version.ToString()},
                {SemVerPadded, versionAndBranch.Version.ToString("sp")},
                {AssemblySemVer, versionAndBranch.Version.ToString("j") + ".0"},
                {FullSemVer, versionAndBranch.Version.ToString("f")},
                {FullSemVerPadded, versionAndBranch.Version.ToString("fp")},
                {ClassicVersion, versionAndBranch.Version.ToString("j") + (versionAndBranch.Version.BuildMetaData.CommitsSinceTag ?? 0)},
                {BranchName, versionAndBranch.Version.BuildMetaData.Branch},
                {Sha, versionAndBranch.Sha},
            };

            return variables;
        }
    }
}