using System;
using GitFlowVersion;

public class TeamCity
{
    public static string GenerateBuildVersion(SemanticVersion semanticVersion)
    {
        var prereleaseString = "";

        if (semanticVersion.Stage != Stage.Final)
        {
            prereleaseString = "-" + semanticVersion.Stage;

            if (!string.IsNullOrEmpty(semanticVersion.Suffix))
            {
                prereleaseString += semanticVersion.Suffix;
            }
            else
            {
                prereleaseString += semanticVersion.PreRelease;
            }
        }

        return string.Format("##teamcity[buildNumber '{0}.{1}.{2}{3}']", semanticVersion.Major, semanticVersion.Minor, semanticVersion.Patch, prereleaseString);
    }

    public static bool IsRunningInBuildAgent()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEAMCITY_VERSION"));
    }

}