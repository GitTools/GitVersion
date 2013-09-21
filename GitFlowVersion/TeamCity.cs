using System;
using System.Collections;
using System.Collections.Generic;
using GitFlowVersion;

public class TeamCity
{
    public static string GenerateBuildVersion(VersionAndBranch versionInformation)
    {
        var prereleaseString = "";

        if (versionInformation.Version.Stability != Stability.Final)
        {
            switch (versionInformation.BranchType )
            {
             case BranchType.Develop:
                    prereleaseString = "-" + versionInformation.Version.Stability + versionInformation.Version.PreReleaseNumber;
                    break;

             case BranchType.Release:
                    prereleaseString = "-" + versionInformation.Version.Stability + versionInformation.Version.PreReleaseNumber;
                    break;

             case BranchType.Hotfix:
                    prereleaseString = "-" + versionInformation.Version.Stability + versionInformation.Version.PreReleaseNumber;
                    break;
             case BranchType.PullRequest:
                    prereleaseString = "-PullRequest-" + versionInformation.Version.Suffix;
                    break;
             case BranchType.Feature:
                    prereleaseString = "-Feature-" + versionInformation.BranchName + "-" + versionInformation.Sha;
                    break;
            }

        }

        return string.Format("##teamcity[buildNumber '{0}.{1}.{2}{3}']", versionInformation.Version.Major, versionInformation.Version.Minor, versionInformation.Version.Patch, prereleaseString);
    }


    public static bool IsRunningInBuildAgent()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEAMCITY_VERSION"));
    }

    public static bool IsBuildingAPullRequest()
    {
        var branchInfo = GetBranchEnvironmentVariable();

        return !string.IsNullOrEmpty(branchInfo) && branchInfo.Contains("/Pull/");
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

    public static IEnumerable<string> GenerateBuildLogOutput(VersionInformation versionInformation)
    {
        yield return GenerateBuildVersion(versionInformation);
        yield return GenerateBuildParameter("Major", versionInformation.Major.ToString());
        yield return GenerateBuildParameter("Minor", versionInformation.Minor.ToString());
        yield return GenerateBuildParameter("Patch", versionInformation.Patch.ToString());
        yield return GenerateBuildParameter("Stability", versionInformation.Stability.ToString());
        yield return GenerateBuildParameter("PreReleaseNumber", versionInformation.PreReleaseNumber.ToString());

    }

    static string GenerateBuildParameter(string name, string value)
    {
        return string.Format("#teamcity[setParameter name='GitFlowVersion.{0}' value='{1}']",name,value);
    }
}