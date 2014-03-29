namespace GitVersion
{
    using System;
    using System.Collections.Generic;

    public static class VariableProvider
    {
        public const string Major = "Major";
        public const string Minor = "Minor";
        public const string Patch = "Patch";
        public const string Suffix = "Suffix";
        public const string InformationalVersion = "InformationalVersion";
        public const string BranchName = "BranchName";
        public const string BranchType = "BranchType";
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
                {PreReleaseTagWithDash, "-" + versionAndBranch.Version.PreReleaseTag},
                {Suffix, versionAndBranch.Version.Suffix},
                {InformationalVersion, versionAndBranch.ToLongString()},
                {MajorMinorPatch, string.Format("{0}.{1}.{2}", versionAndBranch.Version.Major, versionAndBranch.Version.Minor, versionAndBranch.Version.Patch)},
                {SemVer, versionAndBranch.GenerateSemVer()},
                {SemVerPadded, versionAndBranch.GeneratePaddedSemVer()},
                {AssemblySemVer, versionAndBranch.GenerateAssemblySemVer()},
                {FullSemVer, versionAndBranch.GenerateFullSemVer()},
                {FullSemVerPadded, versionAndBranch.GeneratePaddedFullSemVer()},
                {ClassicVersion, versionAndBranch.GenerateClassicVersion()},
                {BranchName, versionAndBranch.BranchName},
                {BranchType, versionAndBranch.BranchType == null ? null : versionAndBranch.BranchType.ToString()},
                {Sha, versionAndBranch.Sha},
            };

            return variables;
        }
    }
}