using System;
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

}