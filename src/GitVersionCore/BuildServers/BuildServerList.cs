using System;
using System.Collections.Generic;
using GitVersion.Helpers;
using GitVersion.Common;

namespace GitVersion.BuildServers
{
    public static class BuildServerList
    {
        private static List<IBuildServer> supportedBuildServers;

        public static IEnumerable<IBuildServer> GetApplicableBuildServers()
        {
            var buildServices = new List<IBuildServer>();

            foreach (var buildServer in supportedBuildServers)
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

        public static void Init(IEnvironment environment)
        {
            supportedBuildServers = new List<IBuildServer>
            {
                new ContinuaCi(environment),
                new TeamCity(environment),
                new AppVeyor(environment),
                new MyGet(environment),
                new Jenkins(environment),
                new GitLabCi(environment),
                new VsoAgent(environment),
                new TravisCI(environment),
                new EnvRun(environment),
                new Drone(environment)
            };
        }
    }
}
