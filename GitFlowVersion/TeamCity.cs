using System;
using System.Collections;
using GitFlowVersion;

public class TeamCity
{
    public static string GenerateBuildVersion(VersionInformation versionInformation)
    {
        var prereleaseString = "";

        if (versionInformation.Stability != Stability.Final)
        {
            prereleaseString = "-" + versionInformation.Stability;

            if (!string.IsNullOrEmpty(versionInformation.Suffix))
            {
                prereleaseString += versionInformation.Suffix;
            }
            else
            {
                prereleaseString += versionInformation.PreReleaseNumber;
            }
        }

        return string.Format("##teamcity[buildNumber '{0}.{1}.{2}{3}']", versionInformation.Major, versionInformation.Minor, versionInformation.Patch, prereleaseString);
    }


    public static bool IsRunningInBuildAgent()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEAMCITY_VERSION"));
    }

    public static bool IsBuildingAPullRequest()
    {
        return !string.IsNullOrEmpty(GetBranchEnvironmentVariable());
    }


    static string GetBranchEnvironmentVariable()
    {
        foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
        {
            if (((string)de.Key).StartsWith("teamcity.build.vcs.branch."))
                return (string)de.Value;
        }

        return null;
    }

    public static int CurrentPullRequestNo()
    {
        return int.Parse(GetBranchEnvironmentVariable().Split('/')[2]);
    }
}