﻿namespace GitVersion
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
        public const string LegacySemVer = "LegacySemVer";
        public const string LegacySemVerPadded = "LegacySemVerPadded";
        public const string FullSemVer = "FullSemVer";
        public const string AssemblySemVer = "AssemblySemVer";
        public const string ClassicVersion = "ClassicVersion";
        public const string ClassicVersionWithTag = "ClassicVersionWithTag";
        public const string PreReleaseTag = "PreReleaseTag";
        public const string PreReleaseTagWithDash = "PreReleaseTagWithDash";
        public const string InformationalVersion = "InformationalVersion";
        public const string AssemblyVersion = "AssemblyVersion";
        public const string AssemblyFileVersion = "AssemblyFileVersion";
        public const string OriginalRelease = "OriginalRelease";
        public const string NugetPackageVersion = "NugetPackageVersion";

        public static Dictionary<string, string> GetVariablesFor(
            SemanticVersion semanticVersion,
            AssemblyVersioningScheme assemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatch,
            bool addNumberOfCommitsSinceTagOnMasterBranchToFileVersion = true)
        {
            var bmd = semanticVersion.BuildMetaData;
            var formatter = bmd.Branch == "develop" ? new CiFeedFormatter() : null;

            var assemblyMetaData = AssemblyVersionsGenerator.Process(
                semanticVersion, assemblyVersioningScheme, addNumberOfCommitsSinceTagOnMasterBranchToFileVersion);

            var variables = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                {Major, semanticVersion.Major.ToString()},
                {Minor, semanticVersion.Minor.ToString()},
                {Patch, semanticVersion.Patch.ToString()},
                {PreReleaseTag, semanticVersion.PreReleaseTag},
                {PreReleaseTagWithDash, semanticVersion.PreReleaseTag.HasTag() ? "-" + semanticVersion.PreReleaseTag : null},
                {BuildMetaData, bmd},
                {FullBuildMetaData, bmd.ToString("f")},
                {MajorMinorPatch, string.Format("{0}.{1}.{2}", semanticVersion.Major, semanticVersion.Minor, semanticVersion.Patch)},
                {SemVer, semanticVersion.ToString(null, formatter)},
                {LegacySemVer, semanticVersion.ToString("l", formatter)},
                {LegacySemVerPadded, semanticVersion.ToString("lp", formatter)},
                {AssemblySemVer, semanticVersion.ToString("j") + ".0"},
                {FullSemVer, semanticVersion.ToString("f", formatter)},
                {InformationalVersion, semanticVersion.ToString("i", formatter)},
                {ClassicVersion, string.Format("{0}.{1}", semanticVersion.ToString("j"), (bmd.CommitsSinceTag ?? 0))},
                {ClassicVersionWithTag, string.Format("{0}.{1}{2}", semanticVersion.ToString("j"),
                    bmd.CommitsSinceTag ?? 0,
                    semanticVersion.PreReleaseTag.HasTag() ? "-" + semanticVersion.PreReleaseTag : null)},
                {BranchName, bmd.Branch},
                {Sha, bmd.Sha},
                {AssemblyVersion, assemblyMetaData.Version},
                {AssemblyFileVersion, assemblyMetaData.FileVersion},
                {OriginalRelease, string.Format("{0}.{1}", 
                    bmd.ReleaseDate.OriginalCommitSha,
                    bmd.ReleaseDate.OriginalDate.UtcDateTime.ToString("u"))},
                {NugetPackageVersion, semanticVersion.ToString("n", formatter)}
            };

            return variables;
        }
    }
}