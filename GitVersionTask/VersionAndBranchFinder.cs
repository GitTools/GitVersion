using System;
using System.Collections.Generic;
using GitVersion;

public static class VersionAndBranchFinder
{
    static List<string> processedDirectories = new List<string>(); 
    public static bool TryGetVersion(string directory, out Tuple<CachedVersion, GitVersionContext> versionAndBranch, Config configuration, bool noFetch)
    {
        var gitDirectory = GitDirFinder.TreeWalkForDotGitDir(directory);

        if (string.IsNullOrEmpty(gitDirectory))
        {
            var message =
                "No .git directory found in provided solution path. This means the assembly may not be versioned correctly. " +
                "To fix this warning either clone the repository using git or remove the `GitVersionTask` nuget package. " +
                "To temporarily work around this issue add a AssemblyInfo.cs with an appropriate `AssemblyVersionAttribute`. " +
                "If it is detected that this build is occurring on a CI server an error may be thrown.";
            Logger.WriteWarning(message);
            versionAndBranch = null;
            return false;
        }

        if (!processedDirectories.Contains(directory))
        {
            processedDirectories.Add(directory);
            var authentication = new Authentication();
            foreach (var buildServer in BuildServerList.GetApplicableBuildServers(authentication))
            {
                Logger.WriteInfo(string.Format("Executing PerformPreProcessingSteps for '{0}'.", buildServer.GetType().Name));
                buildServer.PerformPreProcessingSteps(gitDirectory, noFetch);
            }
        }
        versionAndBranch = VersionCache.GetVersion(gitDirectory, configuration);
        return true;
    }
}
