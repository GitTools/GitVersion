namespace GitVersion
{
    using System;
    using System.Collections.Generic;

    public static class VariableProvider
    {
        public static Dictionary<string, string> ToKeyValue(this VersionAndBranch versionAndBranch)
        {
            var variables = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                {"Major", versionAndBranch.Version.Major.ToString()},
                {"Minor", versionAndBranch.Version.Minor.ToString()},
                {"Patch", versionAndBranch.Version.Patch.ToString()},
                {"Suffix", versionAndBranch.Version.Suffix},
                {"LongVersion", versionAndBranch.ToLongString()},
                {"NugetVersion", versionAndBranch.GenerateNugetVersion()},
                {"ShortVersion", versionAndBranch.ToShortString()},
                {"BranchName", versionAndBranch.BranchName},
                {"BranchType", versionAndBranch.BranchType == null ? null : versionAndBranch.BranchType.ToString()},
                {"Sha", versionAndBranch.Sha},
                {"MajorMinorPatch", string.Format("{0}.{1}.{2}", versionAndBranch.Version.Major, versionAndBranch.Version.Minor, versionAndBranch.Version.Patch)},
                {"SemVer", versionAndBranch.GenerateSemVer()},
                //TODO: legacy. people should move over to semver 
                {"Version", versionAndBranch.GenerateSemVer()}
            };

            var releaseInformation = ReleaseInformationCalculator.Calculate(versionAndBranch.BranchType, versionAndBranch.Version.Tag);
            if (releaseInformation.ReleaseNumber.HasValue)
            {
                variables.Add("PreReleasePartOne", releaseInformation.ReleaseNumber.ToString());
                //TODO: legacy. people should move over to PreReleasePartOne 
                variables.Add("PreReleaseNumber", releaseInformation.ReleaseNumber.ToString());
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