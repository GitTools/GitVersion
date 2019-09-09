using System;
using System.Collections.Generic;
using GitVersion.Helpers;

namespace GitVersion.BuildServers
{
    public static class BuildServerList
    {
        static List<IBuildServer> BuildServers = new List<IBuildServer>
        {
            new ContinuaCi(),
            new TeamCity(),
            new AppVeyor(),
            new MyGet(),
            new Jenkins(),
            new GitLabCi(),
            new VsoAgent(),
            new TravisCI(),
            new EnvRun(),
            new Drone()
        };

        public static IEnumerable<IBuildServer> GetApplicableBuildServers()
        {
            var buildServices = new List<IBuildServer>();

            foreach (var buildServer in BuildServers)
            {
                try
                {
                    if (buildServer.CanApplyToCurrentContext())
                    {
                        Logger.WriteInfo($"Applicable build agent found: '{buildServer.GetType().Name}'.");
                        buildServices.Add(buildServer);
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteWarning($"Failed to check build server '{buildServer.GetType().Name}': {ex.Message}");
                }
            }

            return buildServices;
        }
    }
}