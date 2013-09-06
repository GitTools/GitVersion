using System;
using GitFlowVersion;

public class TeamCity
{
    public static void OutputVersionToBuildServer(SemanticVersion semanticVersion)
    {
        if (IsRunningInBuildAgent()) return;

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

        Console.Out.WriteLine("##teamcity[buildNumber '{0}.{1}.{2}{3}']", semanticVersion.Major, semanticVersion.Minor, semanticVersion.Patch, prereleaseString);
    }

    public static bool IsRunningInBuildAgent()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEAMCITY_VERSION"));
    }

}