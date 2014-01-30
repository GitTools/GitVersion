namespace GitFlowVersion
{
    using System;
    using System.Collections.Generic;

    public class GitFlowVariableProvider
    {
        public static string SemVer = "SemVer";
        public static string LongVersion = "LongVersion";
        public static string NugetVersion = "NugetVersion";
        public static string Major = "Major";
        public static string Minor = "Minor";
        public static string Patch = "Patch";

        public Dictionary<string, string> GetVariables(VersionAndBranch versionAndBranch)
        {
            var variables = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                {Major, versionAndBranch.Version.Major.ToString()},
                {Minor, versionAndBranch.Version.Minor.ToString()},
                {Patch, versionAndBranch.Version.Patch.ToString()},
                {"Suffix", versionAndBranch.Version.Suffix.JsonEncode()},
                {LongVersion, versionAndBranch.ToLongString().JsonEncode()},
                {NugetVersion, versionAndBranch.GenerateNugetVersion().JsonEncode()},
                {"ShortVersion", versionAndBranch.ToShortString().JsonEncode()},
                {"BranchName", versionAndBranch.BranchName.JsonEncode()},
                {"BranchType", versionAndBranch.BranchType == null ? null : versionAndBranch.BranchType.ToString()},
                {"Sha", versionAndBranch.Sha},
                {SemVer, versionAndBranch.GenerateSemVer()}
            };

            var releaseInformation = ReleaseInformationCalculator.Calculate(versionAndBranch.BranchType, versionAndBranch.Version.Tag);
            if (releaseInformation.ReleaseNumber.HasValue)
            {
                variables.Add("PreReleasePartOne", releaseInformation.ReleaseNumber.ToString());
            }
            if (versionAndBranch.Version.PreReleasePartTwo != null)
            {
                variables.Add("PreReleasePartTwo", versionAndBranch.Version.PreReleasePartTwo.ToString());
            }
            if (releaseInformation.Stability.HasValue)
            {
                variables.Add("Stability", releaseInformation.Stability.ToString());
            }
            return variables;
        }
    }
}