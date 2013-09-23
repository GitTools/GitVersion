using System;
using System.Collections;
using System.Collections.Generic;
using GitFlowVersion;

public class TeamCity
{
    public static string GenerateBuildVersion(VersionAndBranch versionAndBranch)
    {
        var prereleaseString = "";

        if (versionAndBranch.Version.Stability != Stability.Final)
        {
            switch (versionAndBranch.BranchType )
            {
             case BranchType.Develop:
                    prereleaseString = "-" + versionAndBranch.Version.Stability + versionAndBranch.Version.PreReleaseNumber;
                    break;

             case BranchType.Release:
                    prereleaseString = "-" + versionAndBranch.Version.Stability + versionAndBranch.Version.PreReleaseNumber;
                    break;

             case BranchType.Hotfix:
                    prereleaseString = "-" + versionAndBranch.Version.Stability + versionAndBranch.Version.PreReleaseNumber;
                    break;
             case BranchType.PullRequest:
                    prereleaseString = "-PullRequest-" + versionAndBranch.Version.Suffix;
                    break;
             case BranchType.Feature:
                    prereleaseString = "-Feature-" + versionAndBranch.BranchName + "-" + versionAndBranch.Sha;
                    break;
            }

        }

        return string.Format("##teamcity[buildNumber '{0}.{1}.{2}{3}']", versionAndBranch.Version.Major, versionAndBranch.Version.Minor, versionAndBranch.Version.Patch, prereleaseString);
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
            if (((string) de.Key).StartsWith("teamcity.build.vcs.branch."))
            {
                return (string)de.Value;
            }
        }

        return null;
    }

    public static int CurrentPullRequestNo()
    {
        return int.Parse(GetBranchEnvironmentVariable().Split('/')[2]);
    }

    public static IEnumerable<string> GenerateBuildLogOutput(VersionAndBranch versionAndBranch)
    {
        yield return GenerateBuildVersion(versionAndBranch);
        var semanticVersion = versionAndBranch.Version;
        yield return GenerateBuildParameter("Major", semanticVersion.Major.ToString());
        yield return GenerateBuildParameter("Minor", semanticVersion.Minor.ToString());
        yield return GenerateBuildParameter("Patch", semanticVersion.Patch.ToString());
        yield return GenerateBuildParameter("Stability", semanticVersion.Stability.ToString());
        yield return GenerateBuildParameter("PreReleaseNumber", semanticVersion.PreReleaseNumber.ToString());

    }

    static string GenerateBuildParameter(string name, string value)
    {
        return string.Format("##teamcity[setParameter name='GitFlowVersion.{0}' value='{1}']",name,value);
    }
}
